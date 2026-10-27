using System;
using System.Collections.Generic;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using BoomerangGuardian.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BoomerangGuardian.Spawning
{
    /// <summary>
    /// Randomly spawns targets, obstacles and collectibles inside the arena (PB-08, PB-09, ADR-0006).
    ///
    /// Overlap avoidance (no initial overlap):
    ///   1. Each prefab gets a rotation-independent footprint (XZ radius + height) measured once.
    ///   2. Random candidates are rejected if they are too close to the walls, the platform,
    ///      the player, or any object already placed this round (our own list, because new
    ///      colliders are not visible to physics queries until the next simulation step).
    ///   3. Physics.CheckBox rejects candidates that touch existing scene geometry.
    ///   4. After spawning, every pair of colliders is checked with Physics.ComputePenetration
    ///      and the number of overlapping pairs is logged as proof (should be 0).
    ///
    /// Platform support (PB-12): HideAll() hides everything; Respawn() regenerates or restores
    /// according to config.json respawnMode.
    ///
    /// Talks to: GuardianPlatform calls HideAll()/Respawn(); HudController and JourneyController
    /// read TargetsSpawned / TargetsRemaining / IsHidden. Counts come from ConfigLoader.Current
    /// (config.json), falling back to the Inspector values. Prefabs come from the SpawnConfig asset,
    /// so this script never references a specific art asset.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("Prefab lists for targets, obstacles and collectibles.")]
        [SerializeField] private SpawnConfig spawnConfig;
        [Tooltip("Parent for spawned objects (e.g. Gameplay/Spawned). Created if empty.")]
        [SerializeField, OptionalReference] private Transform spawnRoot;

        [Header("Fallback values (used when no config.json is loaded)")]
        [SerializeField, Min(0)] private int targetCount = 150;
        [SerializeField, Min(0)] private int obstacleCount = 150;
        [SerializeField, Min(0)] private int collectibleCount = 30;
        [SerializeField] private RespawnMode respawnMode = RespawnMode.Regenerate;

        [Header("Region")]
        [Tooltip("Center of the playable region; its Y is the ground height.")]
        [SerializeField] private Vector3 regionCenter = Vector3.zero;
        [Tooltip("Size (X, Z) of the playable region inside the walls, in meters.")]
        [SerializeField] private Vector2 regionSize = new Vector2(100f, 100f);
        [Tooltip("Minimum distance between spawned objects and the walls (m).")]
        [SerializeField, Min(0f)] private float wallMargin = 2f;

        [Header("Exclusion zones")]
        [Tooltip("The Guardian Platform; nothing spawns within the clear radius.")]
        [SerializeField] private Transform platform;
        [SerializeField, Min(0f)] private float platformClearRadius = 6f;
        [Tooltip("The player; nothing spawns within the clear radius of the current position.")]
        [SerializeField] private Transform player;
        [SerializeField, Min(0f)] private float playerClearRadius = 4f;

        [Header("Placement")]
        [Tooltip("Extra gap between spawned objects (m).")]
        [SerializeField, Min(0f)] private float spacing = 0.5f;
        [Tooltip("Random positions tried per object before giving up.")]
        [SerializeField, Min(1)] private int maxAttemptsPerObject = 60;
        [Tooltip("Layers treated as existing scene geometry (walls, props) during placement.")]
        [SerializeField] private LayerMask blockingMask = ~0;

        /// <summary>Raised after a fresh layout has been spawned.</summary>
        public event Action Spawned;
        /// <summary>Raised after HideAll() (player stepped on the platform).</summary>
        public event Action Hidden;
        /// <summary>Raised after Respawn() in Restore mode (Regenerate raises Spawned instead).</summary>
        public event Action Restored;

        /// <summary>True while the world is hidden because the player is on the platform.</summary>
        public bool IsHidden { get; private set; }
        /// <summary>Number of targets in the current layout (hit or not).</summary>
        public int TargetsSpawned => targets.Count;
        /// <summary>Targets not yet hit. Destroyed targets compare equal to null, so they are skipped.</summary>
        public int TargetsRemaining
        {
            get
            {
                int count = 0;
                foreach (Target t in targets)
                    if (t != null && !t.IsHit) count++;
                return count;
            }
        }

        // Size of a prefab seen from above: a circle (radius around the pivot) plus a height.
        private readonly struct Footprint
        {
            public readonly float Radius;
            public readonly float Height;
            public Footprint(float radius, float height) { Radius = radius; Height = height; }
        }

        // An object already placed this round, as a circle on the XZ ground plane.
        private readonly struct Placement
        {
            public readonly Vector2 Position;
            public readonly float Radius;
            public Placement(Vector2 position, float radius) { Position = position; Radius = radius; }
        }

        private const float GroundClearance = 0.05f;   // keeps CheckBox off the ground plane

        private readonly List<Target> targets = new List<Target>();
        private readonly List<Obstacle> obstacles = new List<Obstacle>();
        private readonly List<Collectible> collectibles = new List<Collectible>();
        private readonly List<Placement> placements = new List<Placement>();
        private readonly Dictionary<GameObject, Footprint> footprints = new Dictionary<GameObject, Footprint>();   // cache: measure each prefab once

        // Start (not Awake): ConfigLoader runs first (DefaultExecutionOrder -100) and loads config.json
        // in its Awake, so ConfigLoader.Current is ready here.
        private void Start() => SpawnAll();

        /// <summary>Destroys any previous objects and spawns a fresh random layout.</summary>
        public void SpawnAll()
        {
            if (spawnConfig == null)
            {
                Debug.LogError($"{nameof(SpawnManager)}: no SpawnConfig assigned.", this);
                return;
            }

            DestroyAll();
            // config is null when no ConfigLoader is in the scene; "?." and "??" then fall back
            // to the Inspector values below.
            GameConfig config = ConfigLoader.Current;
            int seed = config?.randomSeed ?? 0;
            // Seed 0 = new layout every run; any other seed makes the layout repeatable (useful for demos).
            if (seed != 0) Random.InitState(seed);

            // Stopwatch only measures how long the placement takes, for the log line.
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int failed = 0;
            // Targets first, then obstacles, then collectibles: each group must avoid all earlier ones.
            failed += SpawnGroup(spawnConfig.TargetPrefabs, config?.targetCount ?? targetCount, targets, "Targets");
            failed += SpawnGroup(spawnConfig.ObstaclePrefabs, config?.obstacleCount ?? obstacleCount, obstacles, "Obstacles");
            failed += SpawnGroup(spawnConfig.CollectiblePrefabs, config?.collectibleCount ?? collectibleCount, collectibles, "Collectibles");
            watch.Stop();

            // Proof for the "no initial overlap" requirement: should always log 0 overlapping pairs.
            int overlaps = CountOverlappingPairs();
            int total = targets.Count + obstacles.Count + collectibles.Count;
            string summary = $"SpawnManager: {targets.Count} targets + {obstacles.Count} obstacles + {collectibles.Count} collectibles " +
                             $"= {total} objects in {watch.ElapsedMilliseconds} ms (seed: {(seed == 0 ? "random" : seed.ToString())}). " +
                             $"Failed placements: {failed}. Overlap check: {overlaps} overlapping pairs.";
            if (failed > 0 || overlaps > 0) Debug.LogWarning(summary, this);
            else Debug.Log(summary, this);

            IsHidden = false;
            Spawned?.Invoke();
        }

        /// <summary>Hides every spawned object (player entered the platform).</summary>
        public void HideAll()
        {
            SetAllActive(false);
            IsHidden = true;
            Hidden?.Invoke();
        }

        /// <summary>Brings objects back (player left the platform), per config.json respawnMode.</summary>
        public void Respawn()
        {
            RespawnMode mode = ConfigLoader.Current?.RespawnMode ?? respawnMode;
            Debug.Log($"SpawnManager: respawning ({mode}).", this);

            // Regenerate: brand-new random layout at full count.
            // Restore: re-activate the hidden objects; targets already hit (destroyed) stay gone.
            if (mode == RespawnMode.Regenerate)
            {
                SpawnAll();
                return;
            }

            SetAllActive(true);
            IsHidden = false;
            Restored?.Invoke();
        }

        /// <summary>
        /// Spawns <paramref name="count"/> random picks from <paramref name="prefabs"/> into a named group.
        /// Generic over the component type (Target / Obstacle / Collectible), so one method serves all
        /// three lists. Returns how many could not be placed.
        /// </summary>
        private int SpawnGroup<T>(IReadOnlyList<T> prefabs, int count, List<T> spawned, string groupName) where T : Component
        {
            if (count <= 0) return 0;
            if (prefabs == null || prefabs.Count == 0)
            {
                Debug.LogError($"{nameof(SpawnManager)}: no {groupName} prefabs in {spawnConfig.name}.", this);
                return count;
            }

            Transform group = GetGroup(groupName);
            int failed = 0;
            for (int i = 0; i < count; i++)
            {
                T prefab = prefabs[Random.Range(0, prefabs.Count)];
                if (prefab == null) { failed++; continue; }

                Footprint footprint = GetFootprint(prefab.gameObject);
                if (!TryFindPosition(footprint, out Vector3 position)) { failed++; continue; }

                // Random yaw only (around Y) so objects stay upright. The footprint radius already
                // covers every yaw, so rotating cannot create an overlap.
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                // Instantiate(prefab component) returns the same component on the clone, typed as T.
                T instance = Instantiate(prefab, position, rotation, group);
                instance.name = $"{prefab.name}_{spawned.Count + 1:000}";
                spawned.Add(instance);
                // Remember the circle we just used, so later candidates keep their distance from it.
                placements.Add(new Placement(new Vector2(position.x, position.z), footprint.Radius));
            }
            return failed;
        }

        /// <summary>
        /// Rejection sampling: try random ground points until one passes every check,
        /// or give up after maxAttemptsPerObject tries.
        /// </summary>
        private bool TryFindPosition(Footprint footprint, out Vector3 position)
        {
            // Shrink the sampling rectangle by the wall margin and the object's own radius,
            // so even the object's edge stays inside the walls.
            float halfX = regionSize.x * 0.5f - wallMargin - footprint.Radius;
            float halfZ = regionSize.y * 0.5f - wallMargin - footprint.Radius;
            if (halfX <= 0f || halfZ <= 0f)
            {
                position = default;
                return false;
            }

            for (int attempt = 0; attempt < maxAttemptsPerObject; attempt++)
            {
                Vector3 candidate = regionCenter + new Vector3(Random.Range(-halfX, halfX), 0f, Random.Range(-halfZ, halfZ));
                var xz = new Vector2(candidate.x, candidate.z);

                // Cheap 2D distance checks first; the physics query (most expensive) runs last.
                if (IsInside(xz, platform, platformClearRadius + footprint.Radius)) continue;
                if (IsInside(xz, player, playerClearRadius + footprint.Radius)) continue;
                if (OverlapsPlaced(xz, footprint.Radius)) continue;
                if (OverlapsScene(candidate, footprint)) continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>True if the XZ point lies within <paramref name="radius"/> of the transform (exclusion zone).</summary>
        private static bool IsInside(Vector2 xz, Transform center, float radius)
        {
            if (center == null) return false;
            var c = new Vector2(center.position.x, center.position.z);
            // Compare squared lengths: same result as distance < radius, without a square root.
            return (xz - c).sqrMagnitude < radius * radius;
        }

        /// <summary>Circle-vs-circle test against everything placed earlier in this round.</summary>
        private bool OverlapsPlaced(Vector2 xz, float radius)
        {
            foreach (Placement p in placements)
            {
                // Two circles don't touch if their centers are further apart than the sum of radii (+ gap).
                float minDistance = radius + p.Radius + spacing;
                if ((xz - p.Position).sqrMagnitude < minDistance * minDistance) return true;
            }
            return false;
        }

        /// <summary>
        /// Physics test against existing scene colliders (walls, props) on blockingMask.
        /// Uses an axis-aligned box that encloses the footprint circle, lifted slightly off the
        /// ground so the floor collider itself does not count as a hit. Triggers are ignored.
        /// </summary>
        private bool OverlapsScene(Vector3 groundPoint, Footprint footprint)
        {
            float halfHeight = Mathf.Max(0.05f, (footprint.Height - GroundClearance) * 0.5f);
            Vector3 center = groundPoint + Vector3.up * (GroundClearance + halfHeight);
            var halfExtents = new Vector3(footprint.Radius, halfHeight, footprint.Radius);
            return Physics.CheckBox(center, halfExtents, Quaternion.identity, blockingMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Measures a prefab once: the XZ radius around its pivot that covers it at any Y rotation,
        /// plus its height. Uses a temporary instance, because colliders only report bounds in a scene.
        /// </summary>
        private Footprint GetFootprint(GameObject prefab)
        {
            if (footprints.TryGetValue(prefab, out Footprint cached)) return cached;

            // Spawn a throw-away copy far below the arena so it can't touch anything.
            Vector3 probePosition = new Vector3(0f, -10000f, 0f);
            GameObject probe = Instantiate(prefab, probePosition, Quaternion.identity);
            // Push the new transform to the physics engine now, so Collider.bounds is up to date.
            Physics.SyncTransforms();

            // Union of all collider and renderer bounds = the full space the object occupies.

            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Collider c in probe.GetComponentsInChildren<Collider>())
                Encapsulate(ref bounds, ref hasBounds, c.bounds);
            foreach (Renderer r in probe.GetComponentsInChildren<Renderer>())
                if (r.GetComponentInParent<MinimapIcon>() == null)   // icons float high above; not part of the footprint
                    Encapsulate(ref bounds, ref hasBounds, r.bounds);
            // Immediate, so the probe never exists for a physics step or a frame.
            DestroyImmediate(probe);

            Footprint footprint;
            if (!hasBounds)
            {
                footprint = new Footprint(0.5f, 1f);   // nothing measurable: small default
            }
            else
            {
                // Bounds relative to the pivot (the prefab's ground contact point).
                Vector3 min = bounds.min - probePosition;
                Vector3 max = bounds.max - probePosition;
                // Farthest X and Z extent from the pivot; the corner (x, z) is the farthest point of the box.
                float x = Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x));
                float z = Mathf.Max(Mathf.Abs(min.z), Mathf.Abs(max.z));
                // Pythagoras: a circle of radius sqrt(x² + z²) contains that box at ANY Y rotation.
                // Height = top of the bounds above the pivot (at least 0.1 m).
                footprint = new Footprint(Mathf.Sqrt(x * x + z * z), Mathf.Max(0.1f, max.y));
            }

            footprints[prefab] = footprint;
            return footprint;
        }

        /// <summary>Grows <paramref name="bounds"/> to include <paramref name="add"/>; the first non-empty box starts it.</summary>
        private static void Encapsulate(ref Bounds bounds, ref bool hasBounds, Bounds add)
        {
            // Skip empty bounds: encapsulating them would wrongly stretch the box toward their center.
            if (add.size == Vector3.zero) return;
            if (hasBounds) bounds.Encapsulate(add);
            else { bounds = add; hasBounds = true; }
        }

        /// <summary>Validation: counts pairs of spawned colliders that actually penetrate.</summary>
        private int CountOverlappingPairs()
        {
            // The objects were just instantiated this frame; sync so physics sees their positions.
            Physics.SyncTransforms();
            var colliders = new List<Collider>();
            var owners = new List<int>();   // index of the spawned object each collider belongs to
            int owner = 0;
            foreach (Component c in AllSpawned())
            {
                if (c == null) continue;
                foreach (Collider col in c.GetComponentsInChildren<Collider>())
                {
                    colliders.Add(col);
                    owners.Add(owner);
                }
                owner++;
            }

            // Check every unordered pair once (j starts at i + 1).
            int overlaps = 0;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider a = colliders[i];
                for (int j = i + 1; j < colliders.Count; j++)
                {
                    Collider b = colliders[j];
                    if (owners[i] == owners[j]) continue;   // parts of the same object
                    if (!a.bounds.Intersects(b.bounds)) continue;   // cheap AABB pre-filter

                    // Exact shape test: returns true and a depth if the two colliders really penetrate.
                    // Touching within 1 mm is not counted as an overlap.
                    if (Physics.ComputePenetration(a, a.transform.position, a.transform.rotation,
                            b, b.transform.position, b.transform.rotation, out _, out float distance) && distance > 0.001f)
                        overlaps++;
                }
            }
            return overlaps;
        }

        /// <summary>Iterator (yield return) over all three spawned lists, without building a new list.</summary>
        private IEnumerable<Component> AllSpawned()
        {
            foreach (Target t in targets) yield return t;
            foreach (Obstacle o in obstacles) yield return o;
            foreach (Collectible c in collectibles) yield return c;
        }

        /// <summary>Hide/show by SetActive: objects keep their state (and position) for Restore mode.</summary>
        private void SetAllActive(bool active)
        {
            // c != null skips targets that were already hit and destroyed.
            foreach (Component c in AllSpawned())
                if (c != null) c.gameObject.SetActive(active);
        }

        private void DestroyAll()
        {
            foreach (Component c in AllSpawned())
            {
                if (c == null) continue;
                // Deactivate first: Destroy() is deferred to the end of the frame, but inactive
                // colliders leave the physics scene at once, so they can't block the new layout.
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }
            targets.Clear();
            obstacles.Clear();
            collectibles.Clear();
            placements.Clear();
        }

        /// <summary>Finds or creates the child folder (e.g. Spawned/Targets) that keeps the Hierarchy tidy.</summary>
        private Transform GetGroup(string groupName)
        {
            if (spawnRoot == null) spawnRoot = new GameObject("Spawned").transform;
            Transform group = spawnRoot.Find(groupName);
            if (group == null)
            {
                group = new GameObject(groupName).transform;
                group.SetParent(spawnRoot, false);
            }
            return group;
        }

        // Editor-only drawing when selected: green = usable spawn area, cyan = platform / player clear zones.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(regionCenter, new Vector3(regionSize.x - 2f * wallMargin, 0.1f, regionSize.y - 2f * wallMargin));
            Gizmos.color = Color.cyan;
            if (platform != null) Gizmos.DrawWireSphere(platform.position, platformClearRadius);
            if (player != null) Gizmos.DrawWireSphere(player.position, playerClearRadius);
        }
    }
}
