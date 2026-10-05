using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using BoomerangGuardian.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Sprint 5 setup (ADR-0009): the transparent fade material for targets that float away (ghosts).
    /// Idempotent. Rerun after adding target variants.
    /// Menu: Boomerang Guardian → Setup → Sprint 5 (Art Polish)
    /// Batch: -executeMethod BoomerangGuardian.EditorTools.Sprint5Setup.RunBatch
    /// </summary>
    public static class Sprint5Setup
    {
        private const string FadeMaterialPath = SetupUtils.ProjectRoot + "/Materials/M_TargetFade.mat";
        private const string SpawnConfigPath = SetupUtils.ProjectRoot + "/Settings/SpawnConfig.asset";

        [MenuItem("Boomerang Guardian/Setup/Sprint 5 (Art Polish)")]
        public static void RunFromMenu()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sprint 5 setup", "Exit Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Debug.Log(Run());
            EditorUtility.DisplayDialog("Sprint 5 setup finished", "Done. See the Console for the report.", "OK");
        }

        public static void RunBatch() => Debug.Log(Run());

        private static string Run()
        {
            var u = new SetupUtils();
            Material fade = GetOrCreateFadeMaterial(u);

            var spawnConfig = AssetDatabase.LoadAssetAtPath<SpawnConfig>(SpawnConfigPath);
            if (spawnConfig == null)
            {
                u.Warn("SpawnConfig not found. Run the Sprint 2 setup first.");
                return Report(u);
            }

            foreach (Target target in spawnConfig.TargetPrefabs)
            {
                if (target == null) continue;
                string path = AssetDatabase.GetAssetPath(target);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var t = root.GetComponent<Target>();
                    var so = new SerializedObject(t);
                    SerializedProperty template = so.FindProperty("fadeMaterialTemplate");
                    if (template.objectReferenceValue != null)
                    {
                        u.Skipped($"fade template on {path}");
                        continue;
                    }

                    // First run for this prefab: link the template and switch to FloatAway (Halloween ghosts, PO 2026-10-05).
                    // Set Hit Reaction back to Tumble in the Inspector for targets that should fall instead.
                    template.objectReferenceValue = fade;
                    so.FindProperty("hitReaction").enumValueIndex = (int)HitReaction.FloatAway;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    u.Linked($"{path}: Fade Material Template → M_TargetFade, Hit Reaction → FloatAway");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            return Report(u);
        }

        private static Material GetOrCreateFadeMaterial(SetupUtils u)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FadeMaterialPath);
            if (material != null)
            {
                u.Skipped(FadeMaterialPath);
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_TargetFade" };
            MaterialUtility.MakeTransparent(material);
            // Keep the variants a textured, glowing model needs (normal map + emission) in builds.
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_EMISSION");
            material.SetColor(MaterialUtility.EmissionColor, Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(material, FadeMaterialPath);
            u.Created($"{FadeMaterialPath} (URP Lit, Transparent, normal map + emission)");
            return material;
        }

        private static string Report(SetupUtils u) =>
            "Boomerang Guardian: Sprint 5 setup report\n" + u.Log + WiringValidator.Report();
    }
}
