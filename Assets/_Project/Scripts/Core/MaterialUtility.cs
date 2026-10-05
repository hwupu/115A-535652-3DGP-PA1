using UnityEngine;
using UnityEngine.Rendering;

namespace BoomerangGuardian.Core
{
    /// <summary>Helpers for URP Lit materials (used by the target fade-out).</summary>
    public static class MaterialUtility
    {
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private static readonly string[] TextureProperties = { "_BaseMap", "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap" };
        private static readonly string[] ColorProperties = { "_BaseColor", "_EmissionColor" };
        private static readonly string[] FloatProperties = { "_Smoothness", "_Metallic", "_BumpScale", "_OcclusionStrength" };

        /// <summary>Switches a URP Lit material to Transparent / Alpha blending.</summary>
        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);   // 0 = Opaque, 1 = Transparent
            m.SetFloat("_Blend", 0f);     // Alpha
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>Copies the common URP Lit textures, colors and values that exist on both materials.</summary>
        public static void CopyLitProperties(Material from, Material to)
        {
            foreach (string p in TextureProperties)
                if (from.HasProperty(p) && to.HasProperty(p)) to.SetTexture(p, from.GetTexture(p));
            foreach (string p in ColorProperties)
                if (from.HasProperty(p) && to.HasProperty(p)) to.SetColor(p, from.GetColor(p));
            foreach (string p in FloatProperties)
                if (from.HasProperty(p) && to.HasProperty(p)) to.SetFloat(p, from.GetFloat(p));
        }
    }
}
