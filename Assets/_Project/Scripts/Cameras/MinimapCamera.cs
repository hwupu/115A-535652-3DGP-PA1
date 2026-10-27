using UnityEngine;
using UnityEngine.Rendering;

namespace BoomerangGuardian.Cameras
{
    /// <summary>
    /// Top-down orthographic camera for the minimap (PB-15, ADR-0007).
    /// It follows the player and renders only the Minimap layer (flat icons) into a
    /// RenderTexture, which the HUD shows in the upper-right corner.
    /// Scene fog is switched off just for this camera's render, so the map stays readable at night.
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
        private bool fogWasOn;

        private void Awake()
        {
            minimapCamera = GetComponent<Camera>();
            minimapCamera.orthographic = true;   // no perspective: equal scale everywhere, like a real map
        }

        // URP calls these static events for every camera each frame; we hook in to toggle fog
        // only around this camera's render. Static events must be unsubscribed in OnDisable.
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
        }

        // Fog is a global render setting; the map camera looks down from far away, so fog would grey it out.
        private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam != minimapCamera) return;   // the main camera keeps its fog
            fogWasOn = RenderSettings.fog;
            RenderSettings.fog = false;
        }

        private void OnEndCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam != minimapCamera) return;
            RenderSettings.fog = fogWasOn;   // restore, so the next camera renders with the scene's fog
        }

        // LateUpdate: follow the player after it has moved this frame.
        private void LateUpdate()
        {
            if (target == null) return;
            // orthographicSize is half the vertical view height in world units.
            minimapCamera.orthographicSize = viewRadius;
            Vector3 p = target.position;
            // Keep the player's x/z but a fixed height; pitch 90 degrees looks straight down.
            // With yaw 0, world +Z (north) is the top of the map.
            float yaw = rotateWithTarget ? target.eulerAngles.y : 0f;
            transform.SetPositionAndRotation(new Vector3(p.x, height, p.z), Quaternion.Euler(90f, yaw, 0f));
        }
    }
}
