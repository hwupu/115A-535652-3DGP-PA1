using System;
using BoomerangGuardian.Player;
using BoomerangGuardian.UI;
using UnityEngine;

namespace BoomerangGuardian.Interaction
{
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

        public static event Action<Collectible> AnyCollected;

        private static Transform player;   // shared by all collectibles; found once per scene

        private Vector3 visualStart;
        private Quaternion visualAuthoredRotation;   // the model's import rotation inside the prefab
        private float phase;
        private bool collected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AnyCollected = null;
            player = null;
        }

        private void Awake()
        {
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
            visualStart = visual.localPosition;
            visualAuthoredRotation = visual.localRotation;
        }

        private void Update()
        {
            if (visual == null) return;

            float offset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f + phase) * bobHeight;
            visual.localPosition = visualStart + Vector3.up * offset;

            if (motion == VisualMotion.Spin)
            {
                visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
                return;
            }

            if (player == null)
            {
                var motor = FindFirstObjectByType<PlayerMotor>();
                if (motor == null) return;
                player = motor.transform;
            }
            Vector3 toPlayer = player.position - visual.position;
            toPlayer.y = 0f;   // turn around the vertical axis only
            if (toPlayer.sqrMagnitude < 0.0001f) return;
            // World yaw that maps the prefab's +Z onto the player direction, applied on top of the
            // authored import rotation; independent of the spawner's random yaw on the root.
            visual.rotation = Quaternion.LookRotation(toPlayer) * visualAuthoredRotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.GetComponent<PlayerMotor>() == null) return;

            collected = true;
            AnyCollected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
