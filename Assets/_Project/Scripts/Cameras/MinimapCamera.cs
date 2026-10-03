using UnityEngine;

namespace BoomerangGuardian.Cameras
{
    /// <summary>
    /// Top-down orthographic camera for the minimap (PB-15, ADR-0007).
    /// It follows the player and renders only the Minimap layer (flat icons) into a
    /// RenderTexture, which the HUD shows in the upper-right corner.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCamera : MonoBehaviour
    {
        [Tooltip("Object to follow (the player).")]
        [SerializeField] private Transform target;
        [Tooltip("Camera height in world units (above all minimap icons).")]
        [SerializeField] private float height = 60f;
        [Tooltip("Half the visible width in meters: 30 shows a 60 × 60 m area around the player.")]
        [SerializeField, Min(5f)] private float viewRadius = 30f;
        [Tooltip("Off: north stays up and the player arrow turns. On: the map turns with the player.")]
        [SerializeField] private bool rotateWithTarget = false;

        private Camera minimapCamera;

        private void Awake()
        {
            minimapCamera = GetComponent<Camera>();
            minimapCamera.orthographic = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            minimapCamera.orthographicSize = viewRadius;
            Vector3 p = target.position;
            float yaw = rotateWithTarget ? target.eulerAngles.y : 0f;
            transform.SetPositionAndRotation(new Vector3(p.x, height, p.z), Quaternion.Euler(90f, yaw, 0f));
        }
    }
}
