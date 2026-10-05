using System.IO;
using BoomerangGuardian.Audio;
using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using BoomerangGuardian.Player;
using BoomerangGuardian.Spawning;
using BoomerangGuardian.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Sprint 2 setup (ADR-0009). Idempotent: safe to run again; it only creates what is missing
    /// and never overwrites references you already set.
    /// Menu: Boomerang Guardian → Setup → Sprint 2 (Core Loop)
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod BoomerangGuardian.EditorTools.Sprint2Setup.RunBatch
    /// </summary>
    public static class Sprint2Setup
    {
        private const string MainScenePath = SetupUtils.ProjectRoot + "/Scenes/Main.unity";
        private const string PrefabFolder = SetupUtils.ProjectRoot + "/Prefabs/Greybox";
        private const string SpawnConfigPath = SetupUtils.ProjectRoot + "/Settings/SpawnConfig.asset";
        private const string StreamingConfigPath = "Assets/StreamingAssets/" + ConfigLoader.FileName;

        [MenuItem("Boomerang Guardian/Setup/Sprint 2 (Core Loop)")]
        public static void RunFromMenu()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sprint 2 setup", "Exit Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string log = Run();
            EditorUtility.DisplayDialog("Sprint 2 setup finished",
                "Done. See the Console for the full list of created / linked items.\n\nNext: press Play and follow the checklist in docs/guides/sprint-02-setup.md.", "OK");
            Debug.Log(log);
        }

        public static void RunBatch() => Debug.Log(Run());

        private static string Run()
        {
            var u = new SetupUtils();

            // Open the scene first: OpenScene unloads unused assets, which would invalidate
            // references to prefabs created earlier in this run.
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // ---------- Assets ----------
            u.EnsureFolder(PrefabFolder);
            Material mTarget = u.GetOrCreateMaterial("M_Target", new Color(0.85f, 0.2f, 0.2f));
            Material mTargetBand = u.GetOrCreateMaterial("M_TargetBand", new Color(0.95f, 0.95f, 0.95f));
            Material mCrate = u.GetOrCreateMaterial("M_Crate", new Color(0.55f, 0.36f, 0.2f));
            Material mRock = u.GetOrCreateMaterial("M_Rock", new Color(0.5f, 0.5f, 0.52f));
            Material mCollectible = u.GetOrCreateMaterial("M_Collectible", new Color(1f, 0.82f, 0.2f));
            Material mBoomerang = u.GetOrCreateMaterial("M_Boomerang", new Color(0.95f, 0.55f, 0.15f));

            // Named after the Halloween theme (PO 2026-10-05); a fresh project gets a greybox barrel shape under this name.
            Target targetPrefab = GetOrCreatePrefab<Target>(u, "Target_Ghost", root =>
            {
                AddRigidbody(root, 2f);
                var col = root.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.6f, 0f);
                col.radius = 0.4f;
                col.height = 1.2f;
                root.AddComponent<Target>();
                SetupUtils.Visual(PrimitiveType.Cylinder, root.transform, "Model", new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), mTarget);
                SetupUtils.Visual(PrimitiveType.Cylinder, root.transform, "Band", new Vector3(0f, 0.6f, 0f), new Vector3(0.82f, 0.1f, 0.82f), mTargetBand);
            });

            Obstacle cratePrefab = GetOrCreatePrefab<Obstacle>(u, "Obstacle_Crate", root =>
            {
                AddRigidbody(root, 15f);
                var col = root.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.6f, 0f);
                col.size = Vector3.one * 1.2f;
                root.AddComponent<Obstacle>();
                SetupUtils.Visual(PrimitiveType.Cube, root.transform, "Model", new Vector3(0f, 0.6f, 0f), Vector3.one * 1.2f, mCrate);
            });

            Obstacle rockPrefab = GetOrCreatePrefab<Obstacle>(u, "Obstacle_Pillar", root =>
            {
                AddRigidbody(root, 40f);
                var col = root.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 1f, 0f);
                col.size = new Vector3(1f, 2f, 1f);
                root.AddComponent<Obstacle>();
                SetupUtils.Visual(PrimitiveType.Cube, root.transform, "Model", new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 1f), mRock);
            });

            Collectible collectiblePrefab = GetOrCreatePrefab<Collectible>(u, "Collectible_Gem", root =>
            {
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0f, 0.9f, 0f);
                col.radius = 0.45f;
                var collectible = root.AddComponent<Collectible>();
                GameObject model = SetupUtils.Visual(PrimitiveType.Cube, root.transform, "Model", new Vector3(0f, 0.9f, 0f), Vector3.one * 0.4f, mCollectible, new Vector3(45f, 0f, 45f));
                SetupUtils.SetValue(collectible, "visual", p => p.objectReferenceValue = model.transform);
            });

            BoomerangProjectile boomerangPrefab = GetOrCreatePrefab<BoomerangProjectile>(u, "Boomerang", root =>
            {
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.35f;
                var projectile = root.AddComponent<BoomerangProjectile>();
                var spinner = new GameObject("Spinner").transform;
                spinner.SetParent(root.transform, false);
                SetupUtils.Visual(PrimitiveType.Cube, spinner, "Arm_A", new Vector3(0.12f, 0f, 0.1f), new Vector3(0.5f, 0.05f, 0.12f), mBoomerang, new Vector3(0f, 35f, 0f));
                SetupUtils.Visual(PrimitiveType.Cube, spinner, "Arm_B", new Vector3(-0.12f, 0f, 0.1f), new Vector3(0.5f, 0.05f, 0.12f), mBoomerang, new Vector3(0f, -35f, 0f));
                SetupUtils.SetValue(projectile, "spinner", p => p.objectReferenceValue = spinner);
            });

            SpawnConfig spawnConfig = GetOrCreateSpawnConfig(u, targetPrefab, new[] { cratePrefab, rockPrefab }, collectiblePrefab);
            EnsureStreamingConfig(u);

            // ---------- Scene ----------
            PlayerMotor motor = Object.FindFirstObjectByType<PlayerMotor>();
            CameraRig rig = Object.FindFirstObjectByType<CameraRig>();
            if (motor == null || rig == null)
            {
                u.Warn("Player (PlayerMotor) or Main Camera (CameraRig) not found. Run the Sprint 1 setup guide first.");
                return Finish(u, scene);
            }
            GameObject player = motor.gameObject;
            var input = player.GetComponent<PlayerInputReader>();

            // Systems
            ApplicationController app = Object.FindFirstObjectByType<ApplicationController>();
            GameObject gameManager = app != null ? app.gameObject : u.FindOrCreate(scene, "Systems/GameManager");
            var configLoader = u.GetOrAdd<ConfigLoader>(gameManager);
            u.LinkIfEmpty(configLoader, "input", input);
            var scoreManager = u.GetOrAdd<ScoreManager>(gameManager);

            // Spawn manager
            GameObject spawnerGo = u.FindOrCreate(scene, "Systems/SpawnManager");
            bool newSpawner = !spawnerGo.TryGetComponent(out SpawnManager _);
            var spawner = u.GetOrAdd<SpawnManager>(spawnerGo);
            Transform platformVisual = FindByName(scene, "GuardianPlatform");
            Transform ground = FindByName(scene, "Ground");
            u.LinkIfEmpty(spawner, "spawnConfig", spawnConfig);
            u.LinkIfEmpty(spawner, "spawnRoot", u.FindOrCreate(scene, "Gameplay/Spawned").transform);
            u.LinkIfEmpty(spawner, "platform", platformVisual);
            u.LinkIfEmpty(spawner, "player", player.transform);
            if (newSpawner && ground != null && ground.TryGetComponent(out Renderer groundRenderer))
            {
                Bounds b = groundRenderer.bounds;
                SetupUtils.SetValue(spawner, "regionCenter", p => p.vector3Value = new Vector3(b.center.x, b.max.y, b.center.z));
                SetupUtils.SetValue(spawner, "regionSize", p => p.vector2Value = new Vector2(b.size.x, b.size.z));
                u.Linked($"SpawnManager region from Ground: {b.size.x:0} × {b.size.z:0} m");
            }

            // Platform trigger
            GuardianPlatform platformTrigger = GetOrCreatePlatformTrigger(u, scene, platformVisual);
            if (platformTrigger != null) u.LinkIfEmpty(platformTrigger, "spawnManager", spawner);

            // Player: hand, thrower, selector
            Transform hand = player.transform.Find("Hand");
            if (hand == null)
            {
                hand = new GameObject("Hand").transform;
                Undo.RegisterCreatedObjectUndo(hand.gameObject, "Setup");
                hand.SetParent(player.transform, false);
                hand.localPosition = new Vector3(0.45f, 0.35f, 0.35f);
                u.Created("Player/Hand");
            }
            var thrower = u.GetOrAdd<BoomerangThrower>(player);
            u.LinkIfEmpty(thrower, "boomerangPrefab", boomerangPrefab);
            u.LinkIfEmpty(thrower, "hand", hand);

            bool newSelector = !player.TryGetComponent(out TargetSelector _);
            var selector = u.GetOrAdd<TargetSelector>(player);
            u.LinkIfEmpty(selector, "input", input);
            u.LinkIfEmpty(selector, "cameraRig", rig);
            u.LinkIfEmpty(selector, "thrower", thrower);
            if (newSelector)
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                int mask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
                SetupUtils.SetValue(selector, "pickMask", p => p.intValue = mask);
                if (playerLayer < 0) u.Warn("Layer 'Player' not found: the pick ray may hit the player.");
            }

            // Audio
            GameObject audioGo = u.FindOrCreate(scene, "Systems/AudioManager");
            var audio = u.GetOrAdd<AudioManager>(audioGo);   // RequireComponent adds the AudioSource
            u.LinkIfEmpty(audio, "thrower", thrower);
            u.LinkIfEmpty(audio, "selector", selector);
            u.LinkIfEmpty(audio, "platform", platformTrigger);

            // UI
            EnsureEventSystem(u, scene);
            BuildHud(u, scene, scoreManager, spawner, motor, rig, thrower, selector, platformTrigger);

            return Finish(u, scene);
        }

        // ---------- Prefabs ----------

        private static T GetOrCreatePrefab<T>(SetupUtils u, string name, System.Action<GameObject> build) where T : Component
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                u.Skipped(path);
                return existing.GetComponent<T>();
            }

            var root = new GameObject(name);
            build(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            u.Created(path);
            return prefab.GetComponent<T>();
        }

        private static void AddRigidbody(GameObject go, float mass)
        {
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.None;   // hundreds of bodies; keep it cheap
        }

        private static SpawnConfig GetOrCreateSpawnConfig(SetupUtils u, Target target, Obstacle[] obstacles, Collectible collectible)
        {
            var config = AssetDatabase.LoadAssetAtPath<SpawnConfig>(SpawnConfigPath);
            if (config != null)
            {
                u.Skipped(SpawnConfigPath);
                return config;
            }

            config = ScriptableObject.CreateInstance<SpawnConfig>();
            AssetDatabase.CreateAsset(config, SpawnConfigPath);
            var so = new SerializedObject(config);
            SetArray(so.FindProperty("targetPrefabs"), target);
            SetArray(so.FindProperty("obstaclePrefabs"), obstacles);
            SetArray(so.FindProperty("collectiblePrefabs"), collectible);
            so.ApplyModifiedPropertiesWithoutUndo();
            u.Created(SpawnConfigPath);
            return config;
        }

        private static void SetArray(SerializedProperty array, params Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static void EnsureStreamingConfig(SetupUtils u)
        {
            if (File.Exists(StreamingConfigPath))
            {
                u.Skipped(StreamingConfigPath);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(StreamingConfigPath));
            File.WriteAllText(StreamingConfigPath, JsonUtility.ToJson(new GameConfig(), true));
            AssetDatabase.ImportAsset(StreamingConfigPath);
            u.Created(StreamingConfigPath);
        }

        // ---------- Scene helpers ----------

        private static Transform FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root.transform;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            }
            return null;
        }

        private static GuardianPlatform GetOrCreatePlatformTrigger(SetupUtils u, Scene scene, Transform platformVisual)
        {
            GuardianPlatform existing = Object.FindFirstObjectByType<GuardianPlatform>();
            if (existing != null)
            {
                u.Skipped($"GuardianPlatform trigger ({existing.name})");
                return existing;
            }
            if (platformVisual == null)
            {
                u.Warn("GuardianPlatform object not found; platform trigger not created.");
                return null;
            }

            // A separate object, so the trigger isn't affected by the disc's flat scale.
            var go = new GameObject("GuardianPlatformTrigger");
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.transform.SetParent(platformVisual.parent, false);
            go.transform.position = platformVisual.position + Vector3.up * 1.5f;
            Vector3 s = platformVisual.lossyScale;
            go.transform.localScale = new Vector3(s.x * 0.95f, 1.5f, s.z * 0.95f);   // 3 m tall, just inside the disc edge

            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Mesh cylinder = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = cylinder;
            meshCollider.convex = true;
            meshCollider.isTrigger = true;

            var platform = go.AddComponent<GuardianPlatform>();
            u.Created($"{go.name} (convex cylinder trigger, 3 m tall)");
            return platform;
        }

        private static void EnsureEventSystem(SetupUtils u, Scene scene)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                u.Skipped("EventSystem");
                return;
            }
            GameObject go = u.FindOrCreate(scene, "UI/EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static void BuildHud(SetupUtils u, Scene scene, ScoreManager score, SpawnManager spawner, PlayerMotor motor,
            CameraRig rig, BoomerangThrower thrower, TargetSelector selector, GuardianPlatform platform)
        {
            HudController hud = Object.FindFirstObjectByType<HudController>();
            if (hud == null)
            {
                // TMP_Settings.defaultFontAsset throws if the TMP Settings asset was never imported.
                TMP_FontAsset font = Resources.Load<TMP_Settings>("TMP Settings") != null ? TMP_Settings.defaultFontAsset : null;
                if (font == null)
                {
                    u.Warn("TextMeshPro resources missing: Window → TextMeshPro → Import TMP Essential Resources, then run this setup again. HUD skipped.");
                    return;
                }

                GameObject canvasGo = u.FindOrCreate(scene, "UI/HUD");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
                hud = canvasGo.AddComponent<HudController>();

                // Stats panel (top-left)
                RectTransform stats = Panel(canvasGo.transform, "Panel_Stats", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(520f, 170f));
                TMP_Text scoreText = Label(stats, "Text_Score", "Score: 0", 48, FontStyles.Bold, new Vector2(20f, -14f), new Vector2(480f, 60f));
                TMP_Text targetsText = Label(stats, "Text_Targets", "Targets hit: 0", 26, FontStyles.Normal, new Vector2(20f, -80f), new Vector2(480f, 36f));
                TMP_Text statusText = Label(stats, "Text_Status", "Speed: Normal", 22, FontStyles.Normal, new Vector2(20f, -122f), new Vector2(480f, 32f));

                // Cooldown bar (bottom-center)
                RectTransform bar = Panel(canvasGo.transform, "Panel_Cooldown", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(380f, 48f));
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                fill.SetParent(bar, false);
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = Vector2.one;
                fill.offsetMin = new Vector2(4f, 4f);
                fill.offsetMax = new Vector2(-4f, -4f);
                fill.GetComponent<Image>().color = new Color(0.3f, 0.8f, 1f, 0.9f);
                fill.GetComponent<Image>().raycastTarget = false;
                TMP_Text cooldownText = Label(bar, "Text_Cooldown", "Boomerang ready", 24, FontStyles.Bold, Vector2.zero, Vector2.zero);
                Stretch(cooldownText.rectTransform);
                cooldownText.alignment = TextAlignmentOptions.Center;

                // Message line (center, below the middle)
                TMP_Text message = Label(canvasGo.transform, "Text_Message", "", 34, FontStyles.Bold, new Vector2(0f, -200f), new Vector2(1200f, 60f));
                RectTransform mr = message.rectTransform;
                mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(0.5f, 0.5f);
                mr.anchoredPosition = new Vector2(0f, -200f);
                message.alignment = TextAlignmentOptions.Center;

                SetupUtils.SetValue(hud, "scoreText", p => p.objectReferenceValue = scoreText);
                SetupUtils.SetValue(hud, "targetsText", p => p.objectReferenceValue = targetsText);
                SetupUtils.SetValue(hud, "statusText", p => p.objectReferenceValue = statusText);
                SetupUtils.SetValue(hud, "cooldownText", p => p.objectReferenceValue = cooldownText);
                SetupUtils.SetValue(hud, "cooldownFill", p => p.objectReferenceValue = fill);
                SetupUtils.SetValue(hud, "messageText", p => p.objectReferenceValue = message);
                u.Created("HUD canvas (stats top-left, cooldown bar bottom-center, message line; top-right kept free for the minimap)");
            }
            else
            {
                u.Skipped("HUD");
            }

            u.LinkIfEmpty(hud, "score", score);
            u.LinkIfEmpty(hud, "spawner", spawner);
            u.LinkIfEmpty(hud, "motor", motor);
            u.LinkIfEmpty(hud, "cameraRig", rig);
            u.LinkIfEmpty(hud, "thrower", thrower);
            u.LinkIfEmpty(hud, "selector", selector);
            u.LinkIfEmpty(hud, "platform", platform);
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            rt.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            return rt;
        }

        private static TMP_Text Label(Transform parent, string name, string text, float size, FontStyles style, Vector2 position, Vector2 boxSize)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = boxSize;
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static string Finish(SetupUtils u, Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Boomerang Guardian: Sprint 2 setup report\n" + u.Log + WiringValidator.Report();
        }
    }
}
