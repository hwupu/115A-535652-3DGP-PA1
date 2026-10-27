using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BoomerangGuardian.Interaction
{
    /// <summary>
    /// Hover preview of the boomerang's path (PB-29). Every frame it ray casts from the cursor with the
    /// same logic as a click (<see cref="TargetSelector.Evaluate"/>) and draws the same Bézier curve the
    /// boomerang flies, for targets and invalid objects alike. One neutral color on purpose (PO): the
    /// preview doesn't reveal whether a click is valid. Hidden over the HUD, while turning the camera,
    /// in menus, or when the ray hits nothing.
    /// Reads the path shape (Curve, ArcHeight) from the thrower's projectile prefab and uses the shared
    /// static <see cref="BoomerangProjectile.ControlPoint"/> / <see cref="BoomerangProjectile.Bezier"/>,
    /// so the drawn line and the real flight cannot drift apart (ADR-0005 amendment).
    /// </summary>
    public class TrajectoryPreview : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private TargetSelector selector;
        [SerializeField] private BoomerangThrower thrower;
        [SerializeField] private CameraRig cameraRig;
        [Tooltip("Line drawing the curve (world space).")]
        [SerializeField] private LineRenderer line;

        [Header("Look")]
        [SerializeField, Min(2)] private int segments = 32;
        [Tooltip("Line color; alpha = opacity. The same for valid and invalid objects.")]
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.25f);

        private Vector3[] points;

        private void Awake()
        {
            // N segments need N + 1 points. Allocated once here, reused every frame (no per-frame garbage).
            points = new Vector3[segments + 1];
            if (line != null)
            {
                line.useWorldSpace = true;
                line.positionCount = points.Length;
                line.enabled = false;
            }
        }

        // LateUpdate runs after all Update calls, so the player and camera have already moved this
        // frame and the curve starts at the hand's final position (no one-frame lag).
        private void LateUpdate()
        {
            if (line == null) return;
            line.enabled = TryBuildCurve();
            if (!line.enabled) return;

            line.startColor = color;
            line.endColor = color;
            line.SetPositions(points);
        }

        /// <summary>
        /// Fills <c>points</c> with the predicted path. Returns false when the preview should be hidden.
        /// </summary>
        private bool TryBuildCurve()
        {
            if (input == null || selector == null || thrower == null || cameraRig == null) return false;
            // Hidden when paused/in menus (gameplay input off), while RMB-looking or MMB-orbiting, or over UI.
            if (!input.GameplayEnabled || input.IsLookHeld || cameraRig.IsOrbiting) return false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

            // Same decision a click would make, but without throwing; `end` is where the curve ends.
            SelectionResult result = selector.Evaluate(input.PointerPosition, out _, out Vector3 end, out _, out string clickedName);
            if (result == SelectionResult.AlreadyHit) return false;
            if (result == SelectionResult.NotATarget && clickedName == "(nothing)") return false;   // sky

            // Fallback values match the projectile's defaults if no prefab is assigned.
            BoomerangProjectile prefab = thrower.Prefab;
            float curve = prefab != null ? prefab.Curve : 0.3f;
            float arcHeight = prefab != null ? prefab.ArcHeight : 0.5f;

            Vector3 start = thrower.ThrowOrigin;
            Vector3 control = BoomerangProjectile.ControlPoint(start, end, curve, arcHeight);
            // Sample the Bézier at evenly spaced t = 0, 1/N, ..., 1; the LineRenderer connects the samples.
            for (int i = 0; i < points.Length; i++)
                points[i] = BoomerangProjectile.Bezier(start, control, end, (float)i / segments);
            return true;
        }
    }
}
