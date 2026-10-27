using UnityEngine;
using UnityEngine.Rendering;

namespace BoomerangGuardian.Core
{
    /// <summary>
    /// Helpers for URP Lit materials (used by the target fade-out, ADR-0010).
    /// Target builds transparent per-instance material copies so it can fade alpha: it copies the
    /// M_TargetFade template and applies CopyLitProperties, or falls back to MakeTransparent at runtime.
    /// The Sprint 5 Editor setup uses MakeTransparent to author the M_TargetFade template itself.
    /// A static class: pure helper functions, no state, not a component.
    /// </summary>
    public static class MaterialUtility
    {
        // Shader.PropertyToID turns the name into an int once; SetColor(int) is faster than SetColor(string).
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private static readonly string[] TextureProperties = { "_BaseMap", "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap" };
        private static readonly string[] ColorProperties = { "_BaseColor", "_EmissionColor" };
        private static readonly string[] FloatProperties = { "_Smoothness", "_Metallic", "_BumpScale", "_OcclusionStrength" };

        /// <summary>Switches a URP Lit material to Transparent / Alpha blending.</summary>
        public static void MakeTransparent(Material m)
        {
            // Changing _Surface in code does not update the rest of the material like the Inspector does,
            // so we also set the blend factors, ZWrite, keyword and render queue by hand.
            m.SetFloat("_Surface", 1f);   // 0 = Opaque, 1 = Transparent
            m.SetFloat("_Blend", 0f);     // Alpha
            m.SetOverrideTag("RenderType", "Transparent");
            // Classic alpha blending: result = src * srcAlpha + dst * (1 - srcAlpha).
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);   // transparent surfaces don't write depth, so objects behind stay visible
            // The keyword selects the transparent shader variant. Builds only include variants some
            // material uses, which is why the M_TargetFade template exists (ADR-0010).
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;   // draw after opaque geometry (back-to-front)
        }

        /// <summary>Copies the common URP Lit textures, colors and values that exist on both materials.</summary>
        public static void CopyLitProperties(Material from, Material to)
        {
            // HasProperty on both sides: source materials may use other shaders that lack some slots.
            foreach (string p in TextureProperties)
                if (from.HasProperty(p) && to.HasProperty(p)) to.SetTexture(p, from.GetTexture(p));
            foreach (string p in ColorProperties)
                if (from.HasProperty(p) && to.HasProperty(p)) to.SetColor(p, from.GetColor(p));
            foreach (string p in FloatProperties)
                if (from.HasProperty(p) && to.HasProperty(p)) to.SetFloat(p, from.GetFloat(p));
        }
    }
}
