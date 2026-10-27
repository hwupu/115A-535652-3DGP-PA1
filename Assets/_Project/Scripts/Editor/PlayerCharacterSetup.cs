using System.Linq;
using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Player character setup (PB-28, ADR-0011): the Little Witch model with Idle / Walk / Run / Jump / Throw.
    /// Simple by design. Idempotent: rerun after re-exporting the FBX.
    /// Steps: configure the FBX importer (Generic rig, named clips, looping), build the AC_Player
    /// Animator Controller (Speed blend tree + Jump / Throw triggers), create the M_Witch material,
    /// then put the model under the Player as "Model", move the boomerang "Hand" onto the hand bone,
    /// hide the body in first person (CameraRig) and add PlayerAnimator, which feeds the Animator
    /// from PlayerMotor (speed, Jumped event) and BoomerangThrower (Thrown event).
    /// Menu: Boomerang Guardian → Setup → Player Character (Witch)
    /// Batch: -executeMethod BoomerangGuardian.EditorTools.PlayerCharacterSetup.RunBatch
    /// </summary>
    public static class PlayerCharacterSetup
    {
        private const string MainScenePath = SetupUtils.ProjectRoot + "/Scenes/Main.unity";
        private const string ModelPath = SetupUtils.ProjectRoot + "/Models/Cute Witch/The little Witch.fbx";
        private const string TexturePath = SetupUtils.ProjectRoot + "/Models/Cute Witch/textures/Material_Base_Color.png";
        private const string MaterialPath = SetupUtils.ProjectRoot + "/Materials/M_Witch.mat";
        private const string ControllerFolder = SetupUtils.ProjectRoot + "/Animation";
        private const string ControllerPath = ControllerFolder + "/AC_Player.controller";

        // The witch is ~1.33 m tall (incl. hat); scale it to roughly fill the 2 m player capsule.
        private const float ModelScale = 1.5f;
        // The capsule's pivot is its center (height 2), so the feet are 1 m below.
        private static readonly Vector3 ModelOffset = new Vector3(0f, -1f, 0f);

        // Blend thresholds in m/s (game speeds are 5 / 10). Clips are sped up a bit; sliding is accepted.
        private const float WalkThreshold = 2.5f, RunThreshold = 6f, WalkTimeScale = 1.5f, RunTimeScale = 1.8f;

        private static readonly string[] LoopingClips = { "Idle", "Walk", "Run" };

        /// <summary>Menu entry: refuses to run in Play Mode and lets the user save open scene changes first.</summary>
        [MenuItem("Boomerang Guardian/Setup/Player Character (Witch)")]
        public static void RunFromMenu()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Player character setup", "Exit Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log(Run());
            EditorUtility.DisplayDialog("Player character setup finished", "Done. See the Console for the report.", "OK");
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

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            {
                u.Warn($"Model not found at {ModelPath}.");
                return Report(u, scene);
            }

            ConfigureImporter(u);
            AnimatorController controller = GetOrCreateController(u);
            Material material = GetOrCreateMaterial(u);
            SetUpPlayer(u, controller, material);
            return Report(u, scene);
        }

        // ---------- Import ----------

        /// <summary>
        /// Sets the FBX import options in code (same as the Model / Rig / Animation tabs in the Inspector)
        /// and reimports. A clip already named "Idle" means this was done before.
        /// </summary>
        private static void ConfigureImporter(SetupUtils u)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer.clipAnimations.Any(c => c.name == "Idle"))
            {
                u.Skipped("witch import settings");
                return;
            }

            // Generic rig (not Humanoid): the clips play on this one model only, so no retargeting is needed.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;   // keep bones such as hand.R accessible
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;   // dense per-frame keys (handoff §6)

            // Start from the takes found in the FBX, rename them, and mark only locomotion clips as looping
            // (Jump and Throw are one-shots). loopPose blends the last frame into the first for a seamless loop.
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = clip.takeName.Replace("metarig|", "");   // "metarig|Idle" → "Idle"
                clip.loopTime = LoopingClips.Contains(clip.name);
                clip.loopPose = clip.loopTime;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            u.Linked($"witch import: Generic rig, {clips.Length} clips ({string.Join(", ", clips.Select(c => c.name + (c.loopTime ? " loop" : "")))}), compression off");
        }

        /// <summary>Finds an imported clip by name among the sub-assets of the FBX.</summary>
        private static AnimationClip Clip(string name) =>
            AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);

        // ---------- Animator controller ----------

        /// <summary>
        /// Builds the Animator Controller: parameters Speed (float), Jump and Throw (triggers);
        /// a Locomotion blend tree as the default state; and one-shot Jump / Throw states.
        /// </summary>
        private static AnimatorController GetOrCreateController(SetupUtils u)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                u.Skipped(ControllerPath);
                return existing;
            }

            u.EnsureFolder(ControllerFolder);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // Locomotion: 1D blend on Speed.
            // Speed 0 = Idle, 2.5 = Walk, 6 = Run; in between, Unity mixes the two neighbouring clips
            // by linear weight, so speeding up smoothly turns a walk into a run.
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip("Idle"), 0f);
            tree.AddChild(Clip("Walk"), WalkThreshold);
            tree.AddChild(Clip("Run"), RunThreshold);
            // ChildMotion is a struct: copy the array, edit the play speeds, then assign it back.
            ChildMotion[] children = tree.children;
            children[1].timeScale = WalkTimeScale;
            children[2].timeScale = RunTimeScale;
            tree.children = children;
            sm.defaultState = locomotion;

            AddOneShot(sm, locomotion, "Jump", Clip("Jump"));
            AddOneShot(sm, locomotion, "Throw", Clip("ThrowBoomerang"));

            AssetDatabase.SaveAssets();
            u.Created($"{ControllerPath} (Locomotion blend Idle 0 / Walk {WalkThreshold} / Run {RunThreshold} m/s; Jump and Throw triggers)");
            return controller;
        }

        /// <summary>
        /// Adds a state that plays <paramref name="clip"/> once when <paramref name="trigger"/> fires,
        /// then returns to <paramref name="back"/> (Locomotion).
        /// </summary>
        private static void AddOneShot(AnimatorStateMachine sm, AnimatorState back, string trigger, AnimationClip clip)
        {
            AnimatorState state = sm.AddState(trigger);
            state.motion = clip;

            // Enter from Any State as soon as the trigger is set (no exit time), with a 0.1 s cross-fade.
            // canTransitionToSelf = false stops the clip from restarting while it is already playing.
            AnimatorStateTransition enter = sm.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.hasExitTime = false;
            enter.duration = 0.1f;
            enter.canTransitionToSelf = false;

            // Leave automatically at 90 % of the clip, blending back to Locomotion over 0.15 s.
            AnimatorStateTransition exit = state.AddTransition(back);
            exit.hasExitTime = true;
            exit.exitTime = 0.9f;
            exit.duration = 0.15f;
        }

        // ---------- Material ----------

        /// <summary>URP Lit material with the witch's base-color texture; low smoothness for a matte look.</summary>
        private static Material GetOrCreateMaterial(SetupUtils u)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                u.Skipped(MaterialPath);
                return material;
            }
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Witch" };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture != null) material.SetTexture("_BaseMap", texture);
            else u.Warn($"Texture not found: {TexturePath}");
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            u.Created($"{MaterialPath} (base map: {(texture != null ? texture.name : "none")})");
            return material;
        }

        // ---------- Scene ----------

        /// <summary>
        /// Wires the witch into the existing Player. The Player root keeps its Rigidbody, capsule collider
        /// and gameplay scripts; the model is only a visual child (prefab convention).
        /// </summary>
        private static void SetUpPlayer(SetupUtils u, AnimatorController controller, Material material)
        {
            PlayerMotor motor = Object.FindFirstObjectByType<PlayerMotor>();
            CameraRig rig = Object.FindFirstObjectByType<CameraRig>();
            if (motor == null)
            {
                u.Warn("Player (PlayerMotor) not found.");
                return;
            }
            Transform player = motor.transform;

            // Character model as child "Model".
            Transform model = player.Find("Model");
            if (model == null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                // InstantiatePrefab (not Instantiate) keeps the link to the FBX, so re-exports update the scene.
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, player);
                Undo.RegisterCreatedObjectUndo(instance, "Setup");
                instance.name = "Model";
                model = instance.transform;
                model.localPosition = ModelOffset;
                model.localRotation = Quaternion.identity;
                model.localScale = Vector3.one * ModelScale;

                // Replace every material slot on every renderer with M_Witch.
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                {
                    var materials = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    r.sharedMaterials = materials;
                }
                u.Created($"Player/Model (witch, scale {ModelScale}, feet at the capsule bottom, M_Witch)");
            }
            else
            {
                u.Skipped("Player/Model");
            }

            // Animator on the model.
            Animator animator = u.GetOrAdd<Animator>(model.gameObject);
            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = controller;
                u.Linked("Model Animator → AC_Player");
            }
            animator.applyRootMotion = false;   // clips are in place; physics moves the player
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // keep animating (and moving the hand bone) even when the body is not drawn, e.g. in first person

            // Hide the old placeholder visuals (keep the capsule collider).
            if (player.TryGetComponent(out MeshRenderer capsuleRenderer) && capsuleRenderer.enabled)
            {
                Undo.RecordObject(capsuleRenderer, "Setup");
                capsuleRenderer.enabled = false;
                u.Linked("Player capsule Mesh Renderer disabled (collider kept)");
            }
            Transform nose = player.Find("Nose");
            if (nose != null && nose.gameObject.activeSelf)
            {
                Undo.RecordObject(nose.gameObject, "Setup");
                nose.gameObject.SetActive(false);
                u.Linked("Player/Nose deactivated");
            }

            // Spawn the boomerang from the character's right hand.
            // Parenting "Hand" to the animated bone makes the spawn point follow the throw animation.
            Transform handBone = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "hand.R");
            Transform hand = player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Hand");
            if (hand != null && handBone != null && hand.parent != handBone)
            {
                Undo.SetTransformParent(hand, handBone, "Setup");
                hand.localPosition = Vector3.zero;
                u.Linked("Player/Hand moved under the hand.R bone (boomerang spawns from the witch's hand)");
            }

            // First person: hide the character body.
            // In first person CameraRig switches these renderers to ShadowsOnly: the body is not drawn
            // (it would block the view) but still casts its shadow.
            // Only rewrite the list if it differs, to keep reruns idempotent.
            if (rig != null)
            {
                Renderer[] body = model.GetComponentsInChildren<Renderer>(true);
                var so = new SerializedObject(rig);
                SerializedProperty list = so.FindProperty("hideInFirstPerson");
                bool alreadySet = list.arraySize == body.Length &&
                                  Enumerable.Range(0, body.Length).All(i => list.GetArrayElementAtIndex(i).objectReferenceValue == body[i]);
                if (!alreadySet)
                {
                    list.arraySize = body.Length;
                    for (int i = 0; i < body.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = body[i];
                    so.ApplyModifiedProperties();
                    u.Linked($"CameraRig.hideInFirstPerson → {body.Length} witch renderers");
                }
            }

            // Animation driver: PlayerAnimator sets Speed from PlayerMotor and fires the Jump / Throw
            // triggers when PlayerMotor.Jumped / BoomerangThrower.Thrown are raised.
            var driver = u.GetOrAdd<PlayerAnimator>(player.gameObject);
            u.LinkIfEmpty(driver, "animator", animator);
            u.LinkIfEmpty(driver, "motor", motor);
            u.LinkIfEmpty(driver, "thrower", player.GetComponent<BoomerangThrower>());
        }

        /// <summary>Saves the scene and assets, then returns the log plus the WiringValidator report.</summary>
        private static string Report(SetupUtils u, Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Boomerang Guardian: Player character setup report\n" + u.Log + WiringValidator.Report();
        }
    }
}
