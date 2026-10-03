using System;
using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using BoomerangGuardian.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BoomerangGuardian.Interaction
{
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

        public float MaxThrowRange => ConfigLoader.Current?.maxThrowRange ?? maxThrowRange;

        private bool selectRequested;

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

        public SelectionResult Select(Vector2 screenPosition)
        {
            Ray ray = cameraRig.ViewCamera.ScreenPointToRay(screenPosition);
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, pickMask, QueryTriggerInteraction.Ignore);
            Debug.DrawRay(ray.origin, ray.direction * (hitSomething ? hit.distance : maxRayDistance),
                hitSomething ? Color.yellow : Color.gray, 1f);

            Target target = hitSomething ? hit.collider.GetComponentInParent<Target>() : null;
            if (target == null)
            {
                string message = !hitSomething ? "Nothing there to throw at."
                    : hit.collider.GetComponentInParent<Obstacle>() != null ? "That's an obstacle, not a target!"
                    : "That's not a target!";
                return Reject(SelectionResult.NotATarget, message, hitSomething ? hit.collider.name : "(nothing)");
            }

            if (target.IsHit) return SelectionResult.AlreadyHit;   // already flying away; ignore quietly

            float distance = Vector3.Distance(thrower.ThrowOrigin, target.AimPoint);
            if (distance > MaxThrowRange)
                return Reject(SelectionResult.TooFar, $"Too far! {distance:0} m (max {MaxThrowRange:0} m)", target.name);

            if (!thrower.IsReady)
                return Reject(SelectionResult.OnCooldown, $"Boomerang not ready ({thrower.CooldownRemaining:0.0} s)", target.name);

            thrower.Throw(target);
            Debug.Log($"Selected {target.name} at {distance:0.0} m → boomerang thrown.");
            TargetSelected?.Invoke(target);
            return SelectionResult.Thrown;
        }

        private SelectionResult Reject(SelectionResult reason, string message, string clickedName)
        {
            Debug.Log($"Invalid selection ({reason}): clicked {clickedName}.");
            SelectionRejected?.Invoke(reason, message);
            return reason;
        }
    }
}
