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
            points = new Vector3[segments + 1];
            if (line != null)
            {
                line.useWorldSpace = true;
                line.positionCount = points.Length;
                line.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (line == null) return;
            line.enabled = TryBuildCurve();
            if (!line.enabled) return;

            line.startColor = color;
            line.endColor = color;
            line.SetPositions(points);
        }

        private bool TryBuildCurve()
        {
            if (input == null || selector == null || thrower == null || cameraRig == null) return false;
            if (!input.GameplayEnabled || input.IsLookHeld || cameraRig.IsOrbiting) return false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

            SelectionResult result = selector.Evaluate(input.PointerPosition, out _, out Vector3 end, out _, out string clickedName);
            if (result == SelectionResult.AlreadyHit) return false;
            if (result == SelectionResult.NotATarget && clickedName == "(nothing)") return false;   // sky

            BoomerangProjectile prefab = thrower.Prefab;
            float curve = prefab != null ? prefab.Curve : 0.3f;
            float arcHeight = prefab != null ? prefab.ArcHeight : 0.5f;

            Vector3 start = thrower.ThrowOrigin;
            Vector3 control = BoomerangProjectile.ControlPoint(start, end, curve, arcHeight);
            for (int i = 0; i < points.Length; i++)
                points[i] = BoomerangProjectile.Bezier(start, control, end, (float)i / segments);
            return true;
        }
    }
}
