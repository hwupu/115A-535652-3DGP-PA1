using System;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using UnityEngine;

namespace BoomerangGuardian.Player
{
    /// <summary>
    /// Physics-based player movement (ADR-0004).
    /// - W/S/A/D move relative to the camera's yaw, with acceleration smoothing.
    /// - SPACE toggles normal / fast speed.
    /// - F jumps when grounded. In the air, horizontal velocity is kept, so the
    ///   player follows a projectile (parabolic) arc under Unity gravity.
    /// - As a dynamic Rigidbody, the player pushes other Rigidbodies (obstacles).
    /// Talks to: PlayerInputReader (polls Move, listens to ToggleSpeed/Jump), CameraRig (reads Yaw),
    /// ConfigLoader (speeds from config.json). Its events feed PlayerAnimator and JourneyController; the HUD reads IsFast.
    /// All physics work happens in FixedUpdate, the fixed physics step, not in Update.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Input source (usually on this GameObject).")]
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Camera rig whose yaw defines 'forward' for movement.")]
        [SerializeField] private CameraRig cameraRig;

        [Header("Speed (SPACE toggles; overridden by config.json when loaded)")]
        [Tooltip("Normal movement speed in m/s.")]
        [SerializeField, Min(0f)] private float normalSpeed = 5f;
        [Tooltip("Fast movement speed in m/s.")]
        [SerializeField, Min(0f)] private float fastSpeed = 10f;
        [Tooltip("How quickly ground velocity reaches the target speed (m/s²). Higher = snappier.")]
        [SerializeField, Min(0f)] private float groundAcceleration = 40f;
        [Tooltip("Steering while airborne (m/s²). 0 = pure projectile motion.")]
        [SerializeField, Min(0f)] private float airAcceleration = 0f;

        [Header("Jump (F)")]
        [Tooltip("Jump apex height in meters.")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
        [Tooltip("A jump pressed slightly before landing is still accepted within this time (s).")]
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.15f;
        [Tooltip("Extra distance below the capsule that still counts as grounded (m).")]
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.15f;
        [Tooltip("Layers that count as ground.")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Tooltip("Surfaces steeper than this angle (degrees) do not count as ground.")]
        [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 50f;

        [Header("Safety")]
        [Tooltip("Below this height the player is reset to the start position.")]
        [SerializeField] private float fallResetY = -20f;

        /// <summary>Raised when SPACE toggles the speed mode. Argument: true = fast.</summary>
        public event Action<bool> SpeedModeChanged;
        /// <summary>Raised when a jump actually starts (used by the animator).</summary>
        public event Action Jumped;

        /// <summary>Current speed over the ground in m/s (used by the animator).</summary>
        public float HorizontalSpeed
        {
            get
            {
                if (body == null) return 0f;
                Vector3 v = body.linearVelocity;
                return new Vector2(v.x, v.z).magnitude;   // ignore y, so jumping/falling does not count as running
            }
        }

        /// <summary>True in fast mode (SPACE). Shown on the HUD.</summary>
        public bool IsFast { get; private set; }
        /// <summary>Result of the ground sphere-cast, refreshed every physics step.</summary>
        public bool IsGrounded { get; private set; }
        /// <summary>Target ground speed for the current mode, in m/s.</summary>
        public float CurrentSpeed => IsFast ? fastSpeed : normalSpeed;

        // Right after a jump the capsule still touches the ground; ignore ground hits this long.
        private const float JumpGroundLockTime = 0.2f;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private Collider[] ownColliders;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];   // reused buffer: NonAlloc casts create no garbage
        private float jumpRequestTime = float.NegativeInfinity;         // -inf = "no jump pending"
        private float groundLockUntil;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            ownColliders = GetComponentsInChildren<Collider>();

            body.freezeRotation = true;                      // we steer rotation ourselves
            body.interpolation = RigidbodyInterpolation.Interpolate;           // smooth rendering between fixed steps (camera follows in LateUpdate)
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;   // no tunnelling through thin walls in fast mode

            // Zero friction so the player does not stick to walls while jumping.
            // Ground deceleration is handled by groundAcceleration instead.
            capsule.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

            startPosition = body.position;
            startRotation = body.rotation;

            // Without the rig, "forward" would be stuck on world +Z and the player would never turn.
            if (cameraRig == null)
            {
                cameraRig = FindFirstObjectByType<CameraRig>();
                if (cameraRig != null)
                    Debug.LogWarning($"{nameof(PlayerMotor)}: Camera Rig was not assigned; using '{cameraRig.name}'. " +
                                     "Assign it in the Inspector (or run Boomerang Guardian → Validate Wiring).", this);
                else
                    Debug.LogError($"{nameof(PlayerMotor)}: no CameraRig in the scene; movement will follow world axes.", this);
            }
        }

        // Subscribe in OnEnable and unsubscribe in OnDisable, so events never call a disabled/destroyed motor.
        // ConfigLoader.Changed is static, so forgetting to unsubscribe would leak across scene reloads.
        private void OnEnable()
        {
            ConfigLoader.Changed += ApplyConfig;
            if (input == null) return;
            input.ToggleSpeedPressed += ToggleSpeed;
            input.JumpPressed += RequestJump;
        }

        private void OnDisable()
        {
            ConfigLoader.Changed -= ApplyConfig;
            if (input == null) return;
            input.ToggleSpeedPressed -= ToggleSpeed;
            input.JumpPressed -= RequestJump;
        }

        // ConfigLoader loads config.json in its Awake (execution order -100), so Current is ready by Start.
        // If there is no loader (Current == null), the Inspector values stay as the fallback.
        private void Start()
        {
            if (ConfigLoader.Current != null) ApplyConfig(ConfigLoader.Current);
        }

        /// <summary>Copies speeds from config.json; also called again when F5 reloads the file.</summary>
        private void ApplyConfig(GameConfig config)
        {
            normalSpeed = config.normalSpeed;
            fastSpeed = config.fastSpeed;
        }

        // FixedUpdate runs at the physics rate, so velocity changes are frame-rate independent.
        // It also stops when Time.timeScale = 0, which is how menus freeze the player.
        private void FixedUpdate()
        {
            IsGrounded = Time.time >= groundLockUntil && CheckGrounded();

            // Only the camera's horizontal angle (yaw) matters; pitch must not tilt the movement.
            Quaternion yaw = Quaternion.Euler(0f, cameraRig != null ? cameraRig.Yaw : body.rotation.eulerAngles.y, 0f);
            Move(yaw);
            TryJump();
            body.MoveRotation(yaw);   // face the camera direction (also makes FP look correct)

            if (body.position.y < fallResetY) ResetToStart();
        }

        /// <summary>
        /// Steers horizontal velocity toward the WASD direction. Vertical velocity (gravity, jump)
        /// is left to the physics engine.
        /// </summary>
        private void Move(Quaternion yaw)
        {
            Vector2 moveInput = input != null ? input.Move : Vector2.zero;
            // Input (x, y) becomes a local (x, 0, z) vector; rotating it by the yaw quaternion makes it camera-relative.
            Vector3 wish = yaw * new Vector3(moveInput.x, 0f, moveInput.y);
            if (wish.sqrMagnitude > 1f) wish.Normalize();   // no faster diagonals

            Vector3 velocity = body.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            // MoveTowards changes velocity by at most a * dt per step: constant acceleration, no overshoot.
            // In the air airAcceleration defaults to 0, so the horizontal velocity stays fixed (projectile motion).
            horizontal = Vector3.MoveTowards(horizontal, wish * CurrentSpeed, acceleration * Time.fixedDeltaTime);

            // Setting linearVelocity directly (instead of AddForce) gives exact, tunable speeds;
            // the body is still dynamic, so collisions and pushing obstacles still work.
            body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        /// <summary>
        /// Starts a jump if F was pressed recently (jump buffer) and the player is grounded.
        /// The jump is only a vertical launch speed; gravity then makes the parabola.
        /// </summary>
        private void TryJump()
        {
            bool buffered = Time.time - jumpRequestTime <= jumpBufferTime;
            if (!buffered || !IsGrounded) return;

            // v = sqrt(2 g h): the launch speed that reaches jumpHeight under gravity.
            float launchSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(velocity.x, launchSpeed, velocity.z);

            jumpRequestTime = float.NegativeInfinity;
            groundLockUntil = Time.time + JumpGroundLockTime;   // avoid re-jumping while still touching the ground
            IsGrounded = false;
            Jumped?.Invoke();
        }

        /// <summary>
        /// Sphere-casts down from the capsule center and returns true if it hits a walkable surface
        /// (not our own collider, not steeper than maxGroundAngle).
        /// </summary>
        private bool CheckGrounded()
        {
            // Convert the capsule's local size to world size (the prefab may be scaled).
            Vector3 scale = transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * 0.5f, radius);
            Vector3 origin = transform.TransformPoint(capsule.center);
            // Slightly smaller than the capsule, so the cast does not start already touching side walls.
            float castRadius = radius * 0.95f;
            // Travel from the center down to the bottom hemisphere, plus a small tolerance below the feet;
            // (radius - castRadius) adds back the shrink so the sphere's bottom still reaches the same depth.
            float castDistance = halfHeight - radius + groundCheckDistance + (radius - castRadius);

            int count = Physics.SphereCastNonAlloc(origin, castRadius, Vector3.down, groundHits,
                castDistance, groundMask, QueryTriggerInteraction.Ignore);

            // Slope test with a dot product: normal · up = normal.y = cos(slope angle).
            // A surface is walkable if its normal is within maxGroundAngle of straight up.
            float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                // distance 0 means the sphere started inside that collider; its normal is not reliable.
                if (IsOwnCollider(hit.collider) || hit.distance <= 0f) continue;
                if (hit.normal.y >= minNormalY) return true;
            }
            return false;
        }

        private bool IsOwnCollider(Collider other)
        {
            foreach (Collider own in ownColliders)
                if (own == other) return true;
            return false;
        }

        private void ToggleSpeed()
        {
            IsFast = !IsFast;
            Debug.Log($"Speed mode: {(IsFast ? "FAST" : "NORMAL")} ({CurrentSpeed} m/s)");
            SpeedModeChanged?.Invoke(IsFast);
        }

        // The input event only records the time; FixedUpdate (TryJump) does the physics.
        // This keeps all Rigidbody changes inside the physics step.
        private void RequestJump() => jumpRequestTime = Time.time;

        /// <summary>Safety net: teleports the player back to the spawn point after falling off the world.</summary>
        private void ResetToStart()
        {
            Debug.LogWarning("Player fell out of the world; resetting to the start position.");
            body.linearVelocity = Vector3.zero;
            body.position = startPosition;
            body.rotation = startRotation;
        }
    }
}
