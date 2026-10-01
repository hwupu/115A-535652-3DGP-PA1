using System;
using BoomerangGuardian.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace BoomerangGuardian.Cameras
{
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson,
    }

    /// <summary>
    /// First-/third-person camera on the Main Camera (PB-05).
    /// - Hold RMB to look (yaw + clamped pitch); the cursor stays free otherwise for picking.
    /// - V switches between first and third person.
    /// - Scroll wheel changes third-person distance.
    /// - The third-person camera is pulled in when static geometry (walls, platform) is
    ///   in the way. Pushable Rigidbodies are ignored, to avoid jitter among many obstacles.
    /// The rig owns yaw; PlayerMotor reads <see cref="Yaw"/> to move and turn the player.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player transform to follow.")]
        [SerializeField] private Transform target;
        [Tooltip("Input source (on the Player).")]
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Renderers switched to shadows-only in first person, e.g. the player body.")]
        [SerializeField] private Renderer[] hideInFirstPerson = Array.Empty<Renderer>();

        [Header("Look (hold RMB)")]
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [SerializeField, Min(0f)] private float lookSensitivity = 0.15f;
        [Tooltip("Lock and hide the cursor while RMB is held, so it can't leave the window.")]
        [SerializeField] private bool lockCursorWhileLooking = true;
        [SerializeField] private CameraMode startMode = CameraMode.ThirdPerson;

        [Header("First person")]
        [Tooltip("Eye position relative to the target's pivot (world axes).")]
        [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 0.6f, 0f);
        [Tooltip("Min / max pitch in degrees (negative = look up).")]
        [SerializeField] private Vector2 firstPersonPitchLimits = new Vector2(-80f, 80f);

        [Header("Third person")]
        [Tooltip("Point the camera orbits around, relative to the target's pivot.")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 0.8f, 0f);
        [Tooltip("Min / max pitch in degrees (negative = look up).")]
        [SerializeField] private Vector2 thirdPersonPitchLimits = new Vector2(-20f, 70f);
        [Tooltip("Initial orbit distance in meters.")]
        [SerializeField, Min(0f)] private float startDistance = 6f;
        [Tooltip("Min / max orbit distance in meters (scroll wheel).")]
        [SerializeField] private Vector2 distanceLimits = new Vector2(2f, 15f);
        [Tooltip("Meters per scroll notch.")]
        [SerializeField, Min(0f)] private float zoomStep = 1f;
        [Tooltip("Smoothing time for distance changes (s).")]
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.1f;

        [Header("Third-person collision")]
        [Tooltip("Layers that block the camera.")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [Tooltip("Radius of the sphere used to keep the camera out of geometry.")]
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;

        /// <summary>Raised when V switches the camera mode.</summary>
        public event Action<CameraMode> ModeChanged;

        public CameraMode Mode { get; private set; }
        public float Yaw => yaw;
        public Camera ViewCamera { get; private set; }

        private const float MinCollisionDistance = 0.3f;

        private readonly RaycastHit[] collisionHits = new RaycastHit[16];
        private float yaw;
        private float pitch;
        private float targetDistance;
        private float currentDistance;
        private float zoomVelocity;
        private bool cursorLocked;
        private bool skipNextLookDelta;

        private void Awake()
        {
            ViewCamera = GetComponent<Camera>();
            targetDistance = currentDistance = Mathf.Clamp(startDistance, distanceLimits.x, distanceLimits.y);
        }

        private void Start()
        {
            if (target == null)
            {
                Debug.LogError($"{nameof(CameraRig)}: no target assigned.", this);
                enabled = false;
                return;
            }

            yaw = target.eulerAngles.y;
            pitch = 15f;
            SetMode(startMode);
        }

        private void OnEnable()
        {
            if (input != null) input.SwitchViewPressed += ToggleMode;
        }

        private void OnDisable()
        {
            if (input != null) input.SwitchViewPressed -= ToggleMode;
            SetCursorLocked(false);
        }

        private void LateUpdate()
        {
            UpdateLook();
            UpdateZoom();

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            if (Mode == CameraMode.FirstPerson)
            {
                transform.SetPositionAndRotation(target.position + eyeOffset, rotation);
            }
            else
            {
                Vector3 pivot = target.position + pivotOffset;
                Vector3 back = rotation * Vector3.back;
                float distance = ResolveCollision(pivot, back, currentDistance);
                transform.SetPositionAndRotation(pivot + back * distance, rotation);
            }
        }

        private void UpdateLook()
        {
            bool looking = input != null && input.IsLookHeld;
            if (lockCursorWhileLooking && looking != cursorLocked)
            {
                SetCursorLocked(looking);
                skipNextLookDelta = looking;   // locking the cursor can produce one large delta spike
            }
            if (!looking) return;

            Vector2 delta = input.LookDelta;
            if (skipNextLookDelta)
            {
                skipNextLookDelta = false;
                return;
            }

            // Mouse delta is already per-frame (pixels), so no Time.deltaTime here.
            yaw += delta.x * lookSensitivity;
            pitch -= delta.y * lookSensitivity;
            pitch = ClampPitch(pitch);
        }

        private void UpdateZoom()
        {
            if (Mode == CameraMode.ThirdPerson && input != null)
            {
                // Clamp per frame: some platforms report about ±120 per notch, others about ±1.
                float scroll = Mathf.Clamp(input.Zoom, -1f, 1f);
                targetDistance = Mathf.Clamp(targetDistance - scroll * zoomStep, distanceLimits.x, distanceLimits.y);
            }
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
        }

        private float ResolveCollision(Vector3 pivot, Vector3 direction, float desiredDistance)
        {
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, direction, collisionHits,
                desiredDistance, collisionMask, QueryTriggerInteraction.Ignore);

            float distance = desiredDistance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = collisionHits[i];
                if (hit.distance <= 0f || hit.transform.IsChildOf(target)) continue;

                Rigidbody hitBody = hit.collider.attachedRigidbody;
                if (hitBody != null && !hitBody.isKinematic) continue;   // ignore pushable props

                distance = Mathf.Min(distance, hit.distance);
            }
            return Mathf.Max(distance, MinCollisionDistance);
        }

        private void ToggleMode() =>
            SetMode(Mode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson);

        private void SetMode(CameraMode mode)
        {
            Mode = mode;
            pitch = ClampPitch(pitch);

            ShadowCastingMode shadowMode = mode == CameraMode.FirstPerson ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            foreach (Renderer r in hideInFirstPerson)
                if (r != null) r.shadowCastingMode = shadowMode;

            Debug.Log($"Camera mode: {mode}");
            ModeChanged?.Invoke(mode);
        }

        private float ClampPitch(float value)
        {
            Vector2 limits = Mode == CameraMode.FirstPerson ? firstPersonPitchLimits : thirdPersonPitchLimits;
            return Mathf.Clamp(value, limits.x, limits.y);
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
