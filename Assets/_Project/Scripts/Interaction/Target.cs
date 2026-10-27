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
    /// How it reacts in those 2 seconds is chosen per prefab (<see cref="HitReaction"/>, ADR-0010).
    /// Hit by BoomerangProjectile; picked by <see cref="TargetSelector"/> via its AimPoint.
    /// Raises the static <see cref="AnyHit"/> event, which ScoreManager (score) and JourneyController
    /// (story stage) listen to, so they don't need a reference to every spawned target.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Target : MonoBehaviour
    {
        // Shader property IDs are cached once: setting colors by int ID is faster than by string name.
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

        /// <summary>
        /// Raised whenever any target is hit (used for scoring and the story stage; the hit sound
        /// comes from BoomerangThrower.TargetHit). Static, so listeners subscribe once for all targets.
        /// </summary>
        public static event Action<Target> AnyHit;

        /// <summary>True once hit; makes the target unselectable and prevents double scoring.</summary>
        public bool IsHit { get; private set; }

        /// <summary>World-space point to aim at: the center of the target's colliders.</summary>
        public Vector3 AimPoint
        {
            get
            {
                // The root pivot sits on the ground, so aim at the colliders' combined AABB center instead.
                // Computed live, so it follows the target while it moves or is pushed.
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

        // Static events survive between Play sessions when Domain Reload is off; clear stale subscribers on start.
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

            body.WakeUp();   // a resting Rigidbody may be asleep and would ignore forces
            hitTime = Time.time;
            if (hitReaction == HitReaction.FloatAway) StartFloatAway(impulse);
            else
            {
                // ForceMode.Impulse = instant change of momentum (N·s), mass-aware. Applying it at the
                // contact point (off the center of mass) also adds torque, so the target tumbles.
                body.AddForceAtPosition(impulse, point, ForceMode.Impulse);   // push + a little spin
                ApplyTint(iconsOnly: false);
            }

            AnyHit?.Invoke(this);
            // Destroy with a delay: the target stays visible for disappearDelay (spec: 2 s), no coroutine needed.
            Destroy(gameObject, disappearDelay);
            return true;
        }

        /// <summary>
        /// FloatAway setup: no gravity, drag so the push eases out, upright spin, then a steady rise
        /// (FixedUpdate) and a fade (Update) until the delayed Destroy.
        /// </summary>
        private void StartFloatAway(Vector3 impulse)
        {
            floating = true;
            body.useGravity = false;
            body.linearDamping = floatDrag;   // Unity 6 name for the old Rigidbody.drag
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;   // stay upright

            Vector3 push = impulse;
            push.y = Mathf.Max(push.y, 0f);   // never push a ghost downward
            body.AddForce(push, ForceMode.Impulse);   // still "pushed away" (spec), then it rises
            body.angularVelocity = Vector3.up * (floatSpin * Mathf.Deg2Rad);   // angularVelocity is in rad/s

            ApplyTint(iconsOnly: true);   // the minimap dot turns grey
            PrepareFade();
        }

        // Continuous forces belong in FixedUpdate (once per physics step). ForceMode.Acceleration
        // ignores mass, so every ghost rises at the same rate regardless of its Rigidbody mass.
        private void FixedUpdate()
        {
            if (floating) body.AddForce(Vector3.up * riseAcceleration, ForceMode.Acceleration);
        }

        // The fade is visual only, so it runs per rendered frame in Update.
        private void Update()
        {
            if (!floating || fadeMaterials.Count == 0) return;
            // t goes 0 → 1 over disappearDelay; SmoothStep eases in and out, so alpha fades 1 → 0 smoothly
            // and reaches 0 right when the delayed Destroy fires.
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

        /// <summary>
        /// Gives every visual renderer its own transparent copy of its material.
        /// Imported materials are opaque and can't fade. Preferred path: copy the transparent template
        /// (M_TargetFade) and copy the original textures/colors onto it; referencing the template keeps
        /// its transparent shader variant in builds (ADR-0010). Fallback: convert a copy at runtime.
        /// Per-target copies are needed so fading one ghost doesn't fade every ghost sharing the material.
        /// </summary>
        private void PrepareFade()
        {
            if (fadeMaterialTemplate == null)
                Debug.LogWarning($"{name}: no Fade Material Template; converting materials at runtime " +
                                 "(may render opaque in a build). Run Boomerang Guardian → Setup → Sprint 5.", this);

            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (IsIcon(r)) continue;   // the minimap icon keeps its own (grey-tinted) look
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

                    // Remember the starting colors; Update scales them by alpha each frame.
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

        /// <summary>
        /// Tints renderers with hitTint via a MaterialPropertyBlock: a per-renderer override that leaves
        /// the shared material asset untouched and creates no material copies.
        /// </summary>
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

        /// <summary>True for the minimap icon renderer (a child with <see cref="MinimapIcon"/>).</summary>
        private static bool IsIcon(Renderer r) => r.GetComponentInParent<MinimapIcon>() != null;
    }
}
