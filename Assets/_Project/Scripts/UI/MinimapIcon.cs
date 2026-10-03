using UnityEngine;

namespace BoomerangGuardian.UI
{
    /// <summary>
    /// A flat icon (on the Minimap layer) that only the minimap camera sees (ADR-0007).
    /// It stays level at a fixed world height above its parent, even when the parent is
    /// pushed or tumbles, and optionally turns with the parent's yaw (e.g. the player arrow).
    /// The SpawnManager ignores icons when measuring prefab footprints.
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        [Tooltip("World height of the icon. Higher icons draw on top of lower ones.")]
        [SerializeField] private float worldHeight = 20f;
        [Tooltip("Turn with the parent's yaw (use for the player arrow).")]
        [SerializeField] private bool followYaw = false;

        private void LateUpdate()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            Vector3 p = parent.position;
            float yaw = followYaw ? parent.eulerAngles.y : 0f;
            transform.SetPositionAndRotation(new Vector3(p.x, worldHeight, p.z), Quaternion.Euler(0f, yaw, 0f));
        }
    }
}
