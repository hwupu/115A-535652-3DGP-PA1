using System;
using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    /// <summary>
    /// A target object (PB-10). It can be selected by ray casting and hit by a boomerang.
    /// When hit: it is pushed away by an impulse, tinted, stays visible for 2 seconds, then disappears.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Target : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Tooltip("Seconds the target stays visible after being hit (spec: 2 s).")]
        [SerializeField, Min(0f)] private float disappearDelay = 2f;
        [Tooltip("Color applied to the target once it has been hit.")]
        [SerializeField] private Color hitTint = new Color(0.3f, 0.3f, 0.3f, 1f);

        /// <summary>Raised whenever any target is hit (used for scoring and audio).</summary>
        public static event Action<Target> AnyHit;

        public bool IsHit { get; private set; }

        /// <summary>World-space point to aim at: the center of the target's colliders.</summary>
        public Vector3 AimPoint
        {
            get
            {
                if (colliders.Length == 0) return transform.position + Vector3.up * 0.5f;
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
                return bounds.center;
            }
        }

        private Rigidbody body;
        private Collider[] colliders;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => AnyHit = null;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
        }

        /// <summary>
        /// Applies a hit. Returns false if the target was already hit (no double scoring).
        /// </summary>
        public bool Hit(Vector3 impulse, Vector3 point)
        {
            if (IsHit) return false;
            IsHit = true;

            body.WakeUp();
            body.AddForceAtPosition(impulse, point, ForceMode.Impulse);   // push + a little spin
            ApplyTint();

            AnyHit?.Invoke(this);
            Destroy(gameObject, disappearDelay);
            return true;
        }

        private void ApplyTint()
        {
            var block = new MaterialPropertyBlock();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, hitTint);   // URP Lit
                block.SetColor(ColorId, hitTint);       // Built-in / legacy shaders
                r.SetPropertyBlock(block);
            }
        }
    }
}
