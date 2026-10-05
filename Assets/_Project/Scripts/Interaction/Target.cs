using System;
using System.Collections.Generic;
using BoomerangGuardian.Core;
using BoomerangGuardian.UI;
using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    public enum HitReaction
    {
        /// <summary>Knocked away under gravity, tinted grey (e.g. barrels).</summary>
        Tumble,
        /// <summary>Pushed away, then floats upward, spins and fades out (e.g. ghosts).</summary>
        FloatAway,
    }

    /// <summary>
    /// A target object (PB-10). It can be selected by ray casting and hit by a boomerang.
    /// When hit: it is pushed away by an impulse, stays visible for 2 seconds, then disappears.
    /// How it reacts in those 2 seconds is chosen per prefab (<see cref="HitReaction"/>).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Target : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Tooltip("Seconds the target stays visible after being hit (spec: 2 s).")]
        [SerializeField, Min(0f)] private float disappearDelay = 2f;
        [Tooltip("Color applied once hit (Tumble: whole target; FloatAway: minimap icon only).")]
        [SerializeField] private Color hitTint = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Header("Hit reaction")]
        [SerializeField] private HitReaction hitReaction = HitReaction.Tumble;
        [Tooltip("FloatAway: upward acceleration while floating (m/s²).")]
        [SerializeField, Min(0f)] private float riseAcceleration = 3f;
        [Tooltip("FloatAway: drag while floating, so the push eases out smoothly.")]
        [SerializeField, Min(0f)] private float floatDrag = 1.5f;
        [Tooltip("FloatAway: spin around the vertical axis (degrees per second).")]
        [SerializeField] private float floatSpin = 240f;
        [Tooltip("FloatAway: transparent URP Lit material used for the fade (created by the Sprint 5 setup). " +
                 "Referencing it keeps the transparent shader variant in builds.")]
        [SerializeField, OptionalReference] private Material fadeMaterialTemplate;

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
        private bool floating;
        private float hitTime;
        private readonly List<Material> fadeMaterials = new List<Material>();
        private readonly List<Color> fadeBaseColors = new List<Color>();
        private readonly List<Color> fadeEmissionColors = new List<Color>();

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
            hitTime = Time.time;
            if (hitReaction == HitReaction.FloatAway) StartFloatAway(impulse);
            else
            {
                body.AddForceAtPosition(impulse, point, ForceMode.Impulse);   // push + a little spin
                ApplyTint(iconsOnly: false);
            }

            AnyHit?.Invoke(this);
            Destroy(gameObject, disappearDelay);
            return true;
        }

        private void StartFloatAway(Vector3 impulse)
        {
            floating = true;
            body.useGravity = false;
            body.linearDamping = floatDrag;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;   // stay upright

            Vector3 push = impulse;
            push.y = Mathf.Max(push.y, 0f);
            body.AddForce(push, ForceMode.Impulse);   // still "pushed away" (spec), then it rises
            body.angularVelocity = Vector3.up * (floatSpin * Mathf.Deg2Rad);

            ApplyTint(iconsOnly: true);   // the minimap dot turns grey
            PrepareFade();
        }

        private void FixedUpdate()
        {
            if (floating) body.AddForce(Vector3.up * riseAcceleration, ForceMode.Acceleration);
        }

        private void Update()
        {
            if (!floating || fadeMaterials.Count == 0) return;
            float t = disappearDelay > 0f ? Mathf.Clamp01((Time.time - hitTime) / disappearDelay) : 1f;
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < fadeMaterials.Count; i++)
            {
                Color c = fadeBaseColors[i];
                c.a *= alpha;
                fadeMaterials[i].SetColor(MaterialUtility.BaseColor, c);
                fadeMaterials[i].SetColor(MaterialUtility.EmissionColor, fadeEmissionColors[i] * alpha);   // fade the glow too
            }
        }

        /// <summary>Gives every visual renderer its own transparent copy of its material.</summary>
        private void PrepareFade()
        {
            if (fadeMaterialTemplate == null)
                Debug.LogWarning($"{name}: no Fade Material Template; converting materials at runtime " +
                                 "(may render opaque in a build). Run Boomerang Guardian → Setup → Sprint 5.", this);

            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (IsIcon(r)) continue;
                Material[] materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material original = materials[i];
                    if (original == null) continue;

                    Material fade;
                    if (fadeMaterialTemplate != null)
                    {
                        fade = new Material(fadeMaterialTemplate);
                        MaterialUtility.CopyLitProperties(original, fade);
                    }
                    else
                    {
                        fade = new Material(original);
                        MaterialUtility.MakeTransparent(fade);
                    }

                    materials[i] = fade;
                    fadeMaterials.Add(fade);
                    fadeBaseColors.Add(fade.HasProperty(MaterialUtility.BaseColor) ? fade.GetColor(MaterialUtility.BaseColor) : Color.white);
                    fadeEmissionColors.Add(fade.HasProperty(MaterialUtility.EmissionColor) ? fade.GetColor(MaterialUtility.EmissionColor) : Color.black);
                }
                r.sharedMaterials = materials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // a fading ghost shouldn't keep a solid shadow
            }
        }

        private void OnDestroy()
        {
            foreach (Material m in fadeMaterials) Destroy(m);   // runtime copies: avoid leaking materials
        }

        private void ApplyTint(bool iconsOnly)
        {
            var block = new MaterialPropertyBlock();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (iconsOnly && !IsIcon(r)) continue;
                r.GetPropertyBlock(block);
                block.SetColor(MaterialUtility.BaseColor, hitTint);   // URP Lit / Unlit
                block.SetColor(ColorId, hitTint);                     // Built-in / legacy shaders
                r.SetPropertyBlock(block);
            }
        }

        private static bool IsIcon(Renderer r) => r.GetComponentInParent<MinimapIcon>() != null;
    }
}
