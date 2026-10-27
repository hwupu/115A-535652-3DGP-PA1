using System;
using BoomerangGuardian.Player;
using BoomerangGuardian.UI;
using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    /// <summary>How a Collectible's visual moves, chosen per prefab in the Inspector.</summary>
    public enum VisualMotion
    {
        /// <summary>Spins around the vertical axis (e.g. gems).</summary>
        Spin,
        /// <summary>Always turns its front (+Z of the prefab) toward the player (e.g. pumpkins).</summary>
        FacePlayer,
    }

    /// <summary>
    /// A collectible item (PB-13). When the player touches its trigger, it disappears
    /// and raises <see cref="AnyCollected"/>; the ScoreManager adds bonus points.
    /// The visual bobs up and down, and either spins or faces the player.
    /// Listeners of AnyCollected: ScoreManager (points) and AudioManager (pickup sound).
    /// Only the visual child moves; the root and its trigger collider stay where SpawnManager put them.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [Tooltip("Visual child that bobs and spins / faces the player. If empty, the first visual child is used (with a warning).")]
        [SerializeField] private Transform visual;
        [Tooltip("Spin: rotate continuously. FacePlayer: the prefab's front (+Z) always points at the player.")]
        [SerializeField] private VisualMotion motion = VisualMotion.Spin;
        [Tooltip("Spin speed in degrees per second.")]
        [SerializeField] private float spinSpeed = 90f;
        [Tooltip("Bob amplitude in meters.")]
        [SerializeField, Min(0f)] private float bobHeight = 0.15f;
        [Tooltip("Bob cycles per second.")]
        [SerializeField, Min(0f)] private float bobFrequency = 0.6f;

        // Static event: listeners subscribe once for ALL collectibles instead of to each spawned instance.
        public static event Action<Collectible> AnyCollected;

        private static Transform player;   // shared by all collectibles; found once per scene

        private Vector3 visualStart;
        private Quaternion visualAuthoredRotation;   // the model's import rotation inside the prefab
        private float phase;
        private bool collected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Statics survive Play Mode restarts when domain reload is disabled; clear them here.
            AnyCollected = null;
            player = null;
        }

        private void Awake()
        {
            // Trigger: the player walks through it (OnTriggerEnter) instead of bumping into it.
            GetComponent<Collider>().isTrigger = true;
            phase = UnityEngine.Random.value * Mathf.PI * 2f;   // desynchronize the bobbing

            if (visual == null)
            {
                // Fail loudly (ADR-0009): e.g. the link is lost when a model swap replaces the Model object.
                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                {
                    if (r.GetComponentInParent<MinimapIcon>() != null || r.transform == transform) continue;
                    visual = r.transform;
                    while (visual.parent != transform) visual = visual.parent;   // move the whole child, not a sub-mesh
                    break;
                }
                Debug.LogWarning($"{nameof(Collectible)}: 'Visual' is not assigned on {name}; " +
                                 (visual != null ? $"animating '{visual.name}' instead. " : "nothing will move. ") +
                                 "Assign it in the prefab (Boomerang Guardian → Validate Wiring).", this);
            }

            if (visual == null) return;
            // Remember the authored pose; Update animates relative to it.
            visualStart = visual.localPosition;
            visualAuthoredRotation = visual.localRotation;
        }

        // Purely visual animation, so it runs in Update (per frame), not FixedUpdate.
        private void Update()
        {
            if (visual == null) return;

            // Bob: sine wave. frequency * 2π converts cycles/second to radians/second;
            // phase offsets each instance; the result swings between -bobHeight and +bobHeight.
            float offset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f + phase) * bobHeight;
            visual.localPosition = visualStart + Vector3.up * offset;

            if (motion == VisualMotion.Spin)
            {
                // Degrees/second * deltaTime = frame-rate independent spin around the world up axis.
                visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
                return;
            }

            // Lazy lookup: the search runs only until the player is found, then the static cache is reused.
            if (player == null)
            {
                var motor = FindFirstObjectByType<PlayerMotor>();
                if (motor == null) return;
                player = motor.transform;
            }
            Vector3 toPlayer = player.position - visual.position;
            toPlayer.y = 0f;   // turn around the vertical axis only
            if (toPlayer.sqrMagnitude < 0.0001f) return;   // LookRotation of a zero vector is undefined
            // World yaw that maps the prefab's +Z onto the player direction, applied on top of the
            // authored import rotation; independent of the spawner's random yaw on the root.
            // Quaternion order matters: A * B applies B first, then A.
            visual.rotation = Quaternion.LookRotation(toPlayer) * visualAuthoredRotation;
        }

        // Physics callback. Needs a Rigidbody on one side; the player has one, so this fires.
        private void OnTriggerEnter(Collider other)
        {
            // Guard: Destroy is deferred to the end of the frame, so another player collider could still
            // trigger before then; count the pickup only once.
            if (collected) return;
            // Identify the player by component (PlayerMotor on the Rigidbody), not by tag.
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.GetComponent<PlayerMotor>() == null) return;

            collected = true;
            AnyCollected?.Invoke(this);   // raise before Destroy, while the object is still valid
            Destroy(gameObject);
        }
    }
}
