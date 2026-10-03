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

        public bool IsFast { get; private set; }
        public bool IsGrounded { get; private set; }
        public float CurrentSpeed => IsFast ? fastSpeed : normalSpeed;

        private const float JumpGroundLockTime = 0.2f;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private Collider[] ownColliders;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private float jumpRequestTime = float.NegativeInfinity;
        private float groundLockUntil;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            ownColliders = GetComponentsInChildren<Collider>();

            body.freezeRotation = true;                      // we steer rotation ourselves
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

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
        }

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

        private void Start()
        {
            if (ConfigLoader.Current != null) ApplyConfig(ConfigLoader.Current);
        }

        private void ApplyConfig(GameConfig config)
        {
            normalSpeed = config.normalSpeed;
            fastSpeed = config.fastSpeed;
        }

        private void FixedUpdate()
        {
            IsGrounded = Time.time >= groundLockUntil && CheckGrounded();

            Quaternion yaw = Quaternion.Euler(0f, cameraRig != null ? cameraRig.Yaw : body.rotation.eulerAngles.y, 0f);
            Move(yaw);
            TryJump();
            body.MoveRotation(yaw);   // face the camera direction (also makes FP look correct)

            if (body.position.y < fallResetY) ResetToStart();
        }

        private void Move(Quaternion yaw)
        {
            Vector2 moveInput = input != null ? input.Move : Vector2.zero;
            Vector3 wish = yaw * new Vector3(moveInput.x, 0f, moveInput.y);
            if (wish.sqrMagnitude > 1f) wish.Normalize();   // no faster diagonals

            Vector3 velocity = body.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            horizontal = Vector3.MoveTowards(horizontal, wish * CurrentSpeed, acceleration * Time.fixedDeltaTime);

            body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

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
        }

        private bool CheckGrounded()
        {
            Vector3 scale = transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * 0.5f, radius);
            Vector3 origin = transform.TransformPoint(capsule.center);
            float castRadius = radius * 0.95f;
            float castDistance = halfHeight - radius + groundCheckDistance + (radius - castRadius);

            int count = Physics.SphereCastNonAlloc(origin, castRadius, Vector3.down, groundHits,
                castDistance, groundMask, QueryTriggerInteraction.Ignore);

            float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
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

        private void RequestJump() => jumpRequestTime = Time.time;

        private void ResetToStart()
        {
            Debug.LogWarning("Player fell out of the world; resetting to the start position.");
            body.linearVelocity = Vector3.zero;
            body.position = startPosition;
            body.rotation = startRotation;
        }
    }
}
