using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using BoomerangGuardian.Player;
using BoomerangGuardian.Spawning;
using BoomerangGuardian.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Sprint 5 setup (ADR-0009): fade material for ghosts that float away (ADR-0010), ghosts wandering
    /// around (IdleWander, PB-20) and the hover trajectory preview (PB-29).
    /// Why a fade material asset: Unity strips shader variants that no asset uses from builds. A target
    /// that switches its material to transparent only at runtime could render opaque in a build, so each
    /// target prefab references M_TargetFade (already transparent) to keep those variants included.
    /// Idempotent. Rerun after adding target variants.
    /// Menu: Boomerang Guardian → Setup → Sprint 5 (Art Polish)
    /// Batch: -executeMethod BoomerangGuardian.EditorTools.Sprint5Setup.RunBatch
    /// </summary>
    public static class Sprint5Setup
    {
        private const string FadeMaterialPath = SetupUtils.ProjectRoot + "/Materials/M_TargetFade.mat";
        private const string SpawnConfigPath = SetupUtils.ProjectRoot + "/Settings/SpawnConfig.asset";
        private const string MainScenePath = SetupUtils.ProjectRoot + "/Scenes/Main.unity";
        private const string PreviewMaterialPath = SetupUtils.ProjectRoot + "/Materials/M_TrajectoryPreview.mat";

        /// <summary>Menu entry: refuses to run in Play Mode and lets the user save open scene changes first.</summary>
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

        /// <summary>Entry point for -executeMethod (no dialogs, so it works in -batchmode).</summary>
        public static void RunBatch() => Debug.Log(Run());

        /// <summary>Runs all steps and returns the text report.</summary>
        private static string Run()
        {
            var u = new SetupUtils();
            // Open the scene first: OpenScene unloads in-memory assets created earlier in a run.
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            Material fade = GetOrCreateFadeMaterial(u);

            var spawnConfig = AssetDatabase.LoadAssetAtPath<SpawnConfig>(SpawnConfigPath);
            if (spawnConfig == null)
            {
                u.Warn("SpawnConfig not found. Run the Sprint 2 setup first.");
                return Report(u, scene);
            }

            foreach (Target target in spawnConfig.TargetPrefabs) AddWander(u, target);
            SetUpTrajectoryPreview(u);

            // Link the fade template on every target prefab. Prefabs are edited in an isolated preview
            // scene (LoadPrefabContents) and always unloaded in finally, even after `continue`.
            foreach (Target target in spawnConfig.TargetPrefabs)
            {
                if (target == null) continue;
                string path = AssetDatabase.GetAssetPath(target);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var t = root.GetComponent<Target>();
                    // Private [SerializeField] fields are set through SerializedObject, like the Inspector does.
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

            return Report(u, scene);
        }

        /// <summary>Creates (once) M_TargetFade: a transparent URP Lit material used as the template for fading targets (ADR-0010).</summary>
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
            // Black emission = no glow by default. At runtime Target copies this template and
            // MaterialUtility.CopyLitProperties brings over each model's own maps and colors (incl. emission).
            material.SetColor(MaterialUtility.EmissionColor, Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(material, FadeMaterialPath);
            u.Created($"{FadeMaterialPath} (URP Lit, Transparent, normal map + emission)");
            return material;
        }

        /// <summary>
        /// Ghosts drift and spin (PB-20): IdleWander on the target's visual child.
        /// Only the visual moves, so the root's collider and Rigidbody (spawn checks, picking) stay put.
        /// </summary>
        private static void AddWander(SetupUtils u, Target target)
        {
            if (target == null) return;
            string path = AssetDatabase.GetAssetPath(target);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<IdleWander>() != null)
                {
                    u.Skipped($"IdleWander on {path}");
                    return;
                }
                // The visual = the top-level child holding the first non-icon renderer.
                // Find a renderer (skipping minimap icons and the root), then walk up the parents
                // until we reach the direct child of the root.
                Transform visual = null;
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.GetComponentInParent<MinimapIcon>() != null || r.transform == root.transform) continue;
                    visual = r.transform;
                    while (visual.parent != root.transform) visual = visual.parent;
                    break;
                }
                if (visual == null)
                {
                    u.Warn($"{path}: no visual child found for IdleWander");
                    return;
                }
                var wander = root.AddComponent<IdleWander>();
                SetupUtils.SetValue(wander, "visual", p => p.objectReferenceValue = visual);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                u.Created($"IdleWander on {path} (visual: {visual.name})");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Hover trajectory preview (PB-29): a LineRenderer child of the Player.
        /// TrajectoryPreview reads the hovered target via TargetSelector and draws the boomerang's
        /// Bézier path; it gets its references here and starts with the line hidden.
        /// </summary>
        private static void SetUpTrajectoryPreview(SetupUtils u)
        {
            PlayerMotor motor = Object.FindFirstObjectByType<PlayerMotor>();
            if (motor == null)
            {
                u.Warn("Player not found; trajectory preview skipped.");
                return;
            }
            if (Object.FindFirstObjectByType<TrajectoryPreview>() != null)
            {
                u.Skipped("TrajectoryPreview");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(PreviewMaterialPath);
            if (material == null)
            {
                // Sprites/Default: unlit, alpha-blended, uses the line's vertex colors (50 % alpha).
                material = new Material(Shader.Find("Sprites/Default")) { name = "M_TrajectoryPreview" };
                AssetDatabase.CreateAsset(material, PreviewMaterialPath);
                u.Created(PreviewMaterialPath);
            }

            var go = new GameObject("TrajectoryPreview");
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.transform.SetParent(motor.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            // World-space points, so the path does not move when the player turns; View alignment makes
            // the line always face the camera; hidden until the mouse hovers a target.
            line.useWorldSpace = true;
            line.widthMultiplier = 0.06f;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            var preview = go.AddComponent<TrajectoryPreview>();
            GameObject player = motor.gameObject;
            SetupUtils.SetValue(preview, "input", p => p.objectReferenceValue = player.GetComponent<PlayerInputReader>());
            SetupUtils.SetValue(preview, "selector", p => p.objectReferenceValue = player.GetComponent<TargetSelector>());
            SetupUtils.SetValue(preview, "thrower", p => p.objectReferenceValue = player.GetComponent<BoomerangThrower>());
            SetupUtils.SetValue(preview, "cameraRig", p => p.objectReferenceValue = Object.FindFirstObjectByType<CameraRig>());
            SetupUtils.SetValue(preview, "line", p => p.objectReferenceValue = line);
            u.Created("Player/TrajectoryPreview (LineRenderer, 50 % green = valid / red = invalid)");
        }

        /// <summary>Saves the scene and assets, then returns the log plus the WiringValidator report.</summary>
        private static string Report(SetupUtils u, Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Boomerang Guardian: Sprint 5 setup report\n" + u.Log + WiringValidator.Report();
        }
    }
}
