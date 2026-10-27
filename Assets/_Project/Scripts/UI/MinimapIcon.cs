using UnityEngine;

namespace BoomerangGuardian.UI
{
    /// <summary>
    /// A flat icon (on the Minimap layer) that only the minimap camera sees (ADR-0007).
    /// It stays level at a fixed world height above its parent, even when the parent is
    /// pushed or tumbles, and optionally turns with the parent's yaw (e.g. the player arrow).
    /// The SpawnManager ignores icons when measuring prefab footprints.
    /// Used for PB-15: the top-down orthographic MinimapCamera renders only this layer, so the
    /// minimap shows clean symbols instead of the 3D models (theme-agnostic, ADR-0007).
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        [Tooltip("World height of the icon. Higher icons draw on top of lower ones.")]
        [SerializeField] private float worldHeight = 20f;
        [Tooltip("Turn with the parent's yaw (use for the player arrow).")]
        [SerializeField] private bool followYaw = false;

        // LateUpdate runs after all Update calls and after physics has moved the parent this frame,
        // so the icon snaps to the parent's final position before the minimap camera renders.
        private void LateUpdate()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            Vector3 p = parent.position;
            // Keep only the parent's yaw (rotation around world Y). Dropping pitch and roll keeps the
            // icon flat for the top-down camera even when the parent tumbles after a hit.
            float yaw = followYaw ? parent.eulerAngles.y : 0f;
            // Overwrite the world pose each frame instead of inheriting it: x/z follow the parent,
            // y is a fixed height. SetPositionAndRotation sets both in one call.
            transform.SetPositionAndRotation(new Vector3(p.x, worldHeight, p.z), Quaternion.Euler(0f, yaw, 0f));
        }
    }
}
