using System;
using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using BoomerangGuardian.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BoomerangGuardian.Interaction
{
    /// <summary>What a click at the cursor would do; shared by clicks and the hover preview.</summary>
    public enum SelectionResult
    {
        Thrown,
        NotATarget,
        TooFar,
        OnCooldown,
        AlreadyHit,
    }

    /// <summary>
    /// Object picking by ray casting (PB-06).
    /// Left click → ray from the camera through the cursor → the first non-trigger collider.
    /// A valid target in range with the boomerang ready → throw. Otherwise the selection is
    /// rejected (error sound + HUD message). Clicking an already-hit target is silently ignored.
    /// Talks to: <see cref="PlayerInputReader"/> (SelectPressed event, pointer position),
    /// <see cref="CameraRig"/> (the camera to cast from), <see cref="BoomerangThrower"/> (range origin,
    /// cooldown, Throw). Raises SelectionRejected, which HudController (message) and AudioManager
    /// (error sound) listen to. <see cref="TrajectoryPreview"/> reuses <see cref="Evaluate"/>.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private BoomerangThrower thrower;

        [Header("Ray casting")]
        [Tooltip("Layers the pick ray can hit. Exclude the Player layer.")]
        [SerializeField] private LayerMask pickMask = ~0;
        [Tooltip("Maximum ray length in meters.")]
        [SerializeField, Min(1f)] private float maxRayDistance = 500f;
        [Tooltip("Maximum throw distance (used when no config.json is loaded).")]
        [SerializeField, Min(1f)] private float maxThrowRange = 40f;

        /// <summary>Raised when a throw starts at the selected target.</summary>
        public event Action<Target> TargetSelected;
        /// <summary>Raised with the reason and a player-facing message for invalid selections.</summary>
        public event Action<SelectionResult, string> SelectionRejected;

        /// <summary>Maximum ray length in meters.</summary>
        public float MaxRayDistance => maxRayDistance;
        /// <summary>Maximum throw distance: config.json value if loaded (ADR-0008), else the Inspector value.</summary>
        public float MaxThrowRange => ConfigLoader.Current?.maxThrowRange ?? maxThrowRange;

        private bool selectRequested;

        // Subscribe in OnEnable and unsubscribe in OnDisable (always paired), so a disabled or
        // destroyed selector never receives clicks and the event holds no dangling reference.
        private void OnEnable()
        {
            if (input != null) input.SelectPressed += RequestSelect;
        }

        private void OnDisable()
        {
            if (input != null) input.SelectPressed -= RequestSelect;
        }

        // Input callbacks run during input processing; the EventSystem UI check is only
        // reliable in Update, so the click is handled there.
        private void RequestSelect() => selectRequested = true;

        private void Update()
        {
            if (!selectRequested) return;
            selectRequested = false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;   // clicked the HUD
            Select(input.PointerPosition);
        }

        /// <summary>
        /// Handles a click at a screen position: evaluates it, then throws, ignores, or rejects.
        /// </summary>
        public SelectionResult Select(Vector2 screenPosition)
        {
            SelectionResult result = Evaluate(screenPosition, out Target target, out _, out string message, out string clickedName, drawDebugRay: true);
            switch (result)
            {
                case SelectionResult.AlreadyHit:
                    return result;   // already flying away; ignore quietly
                case SelectionResult.Thrown:
                    thrower.Throw(target);
                    Debug.Log($"Selected {target.name} → boomerang thrown.");
                    TargetSelected?.Invoke(target);
                    return result;
                default:
                    return Reject(result, message, clickedName);
            }
        }

        /// <summary>
        /// Ray casts from the cursor and decides what a click would do, without doing it.
        /// Used by clicks (<see cref="Select"/>) and by the hover preview (PB-29).
        /// <paramref name="aimPoint"/> is the target's aim point, or the hit point on an invalid object.
        /// Returns <see cref="SelectionResult.Thrown"/> when a click would throw.
        /// </summary>
        public SelectionResult Evaluate(Vector2 screenPosition, out Target target, out Vector3 aimPoint,
            out string message, out string clickedName, bool drawDebugRay = false)
        {
            // ScreenPointToRay: a ray from the camera through the cursor pixel into the 3D world.
            // Raycast returns the first collider along it; pickMask excludes layers (e.g. Player), and
            // QueryTriggerInteraction.Ignore skips trigger volumes so they don't block the pick.
            Ray ray = cameraRig.ViewCamera.ScreenPointToRay(screenPosition);
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, pickMask, QueryTriggerInteraction.Ignore);
            if (drawDebugRay)   // visible in the Scene view for 1 s (yellow = hit, grey = miss)
                Debug.DrawRay(ray.origin, ray.direction * (hitSomething ? hit.distance : maxRayDistance),
                    hitSomething ? Color.yellow : Color.gray, 1f);

            // Identify by component, not tag: the collider may be on a child of the Target prefab root.
            target = hitSomething ? hit.collider.GetComponentInParent<Target>() : null;
            aimPoint = hitSomething ? hit.point : ray.GetPoint(maxRayDistance);
            clickedName = hitSomething ? hit.collider.name : "(nothing)";
            message = null;

            if (target == null)
            {
                message = !hitSomething ? "Nothing there to throw at."
                    : hit.collider.GetComponentInParent<Obstacle>() != null ? "That's an obstacle, not a target!"
                    : "That's not a target!";
                return SelectionResult.NotATarget;
            }

            aimPoint = target.AimPoint;
            // Checks in priority order: already hit → too far → cooldown → OK.
            if (target.IsHit) return SelectionResult.AlreadyHit;

            // Range is measured from the hand to the target's center, the same points the boomerang flies between.
            float distance = Vector3.Distance(thrower.ThrowOrigin, aimPoint);
            if (distance > MaxThrowRange)
            {
                message = $"Too far! {distance:0} m (max {MaxThrowRange:0} m)";
                return SelectionResult.TooFar;
            }

            if (!thrower.IsReady)
            {
                message = $"Boomerang not ready ({thrower.CooldownRemaining:0.0} s)";
                return SelectionResult.OnCooldown;
            }
            return SelectionResult.Thrown;
        }

        /// <summary>Logs the invalid pick and raises SelectionRejected (HUD message + error sound).</summary>
        private SelectionResult Reject(SelectionResult reason, string message, string clickedName)
        {
            Debug.Log($"Invalid selection ({reason}): clicked {clickedName}.");
            SelectionRejected?.Invoke(reason, message);
            return reason;
        }
    }
}
