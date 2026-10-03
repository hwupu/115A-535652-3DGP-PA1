using System.Collections.Generic;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using BoomerangGuardian.Player;
using BoomerangGuardian.Spawning;
using BoomerangGuardian.UI;
using BoomerangGuardian.Boomerang;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Sprint 3 setup (ADR-0009): minimap (PB-15), Hero's Journey intro / stages / ending (PB-19),
    /// product name, and template clean-up. Idempotent, like Sprint2Setup.
    /// Menu: Boomerang Guardian → Setup → Sprint 3 (Feature Complete)
    /// Batch: -executeMethod BoomerangGuardian.EditorTools.Sprint3Setup.RunBatch
    /// </summary>
    public static class Sprint3Setup
    {
        private const string MainScenePath = SetupUtils.ProjectRoot + "/Scenes/Main.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string SpawnConfigPath = SetupUtils.ProjectRoot + "/Settings/SpawnConfig.asset";
        private const string RenderTexturePath = SetupUtils.ProjectRoot + "/Settings/RT_Minimap.renderTexture";
        private const string ArrowMeshPath = SetupUtils.ProjectRoot + "/Settings/MinimapArrow.asset";
        private const string MinimapLayer = "Minimap";

        // Icon heights: higher draws on top in the top-down view.
        private const float GroundIconY = 10f, StaticIconY = 11f, ObstacleIconY = 19f, TargetIconY = 20f, CollectibleIconY = 21f, PlayerIconY = 25f;

        [MenuItem("Boomerang Guardian/Setup/Sprint 3 (Feature Complete)")]
        public static void RunFromMenu()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sprint 3 setup", "Exit Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string log = Run();
            Debug.Log(log);
            EditorUtility.DisplayDialog("Sprint 3 setup finished",
                "Done. See the Console for the full report.\n\nNext: press Play and follow docs/guides/sprint-03-setup.md.", "OK");
        }

        public static void RunBatch() => Debug.Log(Run());

        private static string Run()
        {
            var u = new SetupUtils();

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            SetProductInfo(u);
            CleanUpTemplate(u);

            // ---------- Minimap assets ----------
            int layer = u.EnsureLayer(MinimapLayer);
            if (layer < 0) return Finish(u, scene);

            Material iconTarget = u.GetOrCreateUnlitMaterial("MI_Target", new Color(1f, 0.3f, 0.3f));
            Material iconObstacle = u.GetOrCreateUnlitMaterial("MI_Obstacle", new Color(0.75f, 0.75f, 0.75f));
            Material iconCollectible = u.GetOrCreateUnlitMaterial("MI_Collectible", new Color(1f, 0.85f, 0.3f));
            Material iconPlayer = u.GetOrCreateUnlitMaterial("MI_Player", new Color(0.3f, 0.85f, 1f));
            Material iconPlatform = u.GetOrCreateUnlitMaterial("MI_Platform", new Color(0.2f, 0.5f, 1f));
            Material iconWall = u.GetOrCreateUnlitMaterial("MI_Wall", Color.white);
            Material iconGround = u.GetOrCreateUnlitMaterial("MI_Ground", new Color(0.16f, 0.28f, 0.16f));
            Mesh arrow = GetOrCreateArrowMesh(u);
            RenderTexture minimapTexture = GetOrCreateRenderTexture(u);

            // Icons on every prefab listed in SpawnConfig (rerun after adding variants).
            var spawnConfig = AssetDatabase.LoadAssetAtPath<SpawnConfig>(SpawnConfigPath);
            if (spawnConfig == null)
            {
                u.Warn("SpawnConfig not found. Run the Sprint 2 setup first.");
                return Finish(u, scene);
            }
            foreach (Target t in spawnConfig.TargetPrefabs)
                AddIconToPrefab(u, t, layer, PrimitiveType.Cylinder, iconTarget, new Vector3(1.6f, 0.02f, 1.6f), 0f, TargetIconY);
            foreach (Obstacle o in spawnConfig.ObstaclePrefabs)
                AddIconToPrefab(u, o, layer, PrimitiveType.Cube, iconObstacle, new Vector3(1.5f, 0.02f, 1.5f), 0f, ObstacleIconY);
            foreach (Collectible c in spawnConfig.CollectiblePrefabs)
                AddIconToPrefab(u, c, layer, PrimitiveType.Cube, iconCollectible, new Vector3(1.1f, 0.02f, 1.1f), 45f, CollectibleIconY);

            // ---------- Scene: player icon, static icons, cameras ----------
            PlayerMotor motor = Object.FindFirstObjectByType<PlayerMotor>();
            CameraRig rig = Object.FindFirstObjectByType<CameraRig>();
            HudController hud = Object.FindFirstObjectByType<HudController>();
            if (motor == null || rig == null || hud == null)
            {
                u.Warn("Player, CameraRig or HUD not found. Run the Sprint 1 guide and the Sprint 2 setup first.");
                return Finish(u, scene);
            }

            AddPlayerIcon(u, motor.transform, layer, arrow, iconPlayer);
            AddStaticIcons(u, scene, layer, iconGround, iconWall, iconPlatform);

            Camera mainCamera = rig.GetComponent<Camera>();
            if ((mainCamera.cullingMask & (1 << layer)) != 0)
            {
                Undo.RecordObject(mainCamera, "Setup");
                mainCamera.cullingMask &= ~(1 << layer);
                u.Linked("Main Camera culling mask: Minimap layer excluded");
            }
            CreateMinimapCamera(u, scene, layer, minimapTexture, motor.transform);

            // ---------- UI ----------
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                u.Warn("TextMeshPro resources missing (Window → TextMeshPro → Import TMP Essential Resources). UI skipped.");
                return Finish(u, scene);
            }
            Transform canvas = hud.transform;
            BuildMinimapPanel(u, canvas, minimapTexture);
            TMP_Text stageText = BuildStagePanel(u, canvas);
            (GameObject intro, Button begin) = BuildIntroPanel(u, canvas);
            (GameObject end, TMP_Text summary, Button cont, Button quit) = BuildEndPanel(u, canvas);

            var journey = u.GetOrAdd<JourneyController>(hud.gameObject);
            GameObject player = motor.gameObject;
            u.LinkIfEmpty(journey, "input", player.GetComponent<PlayerInputReader>());
            u.LinkIfEmpty(journey, "motor", motor);
            u.LinkIfEmpty(journey, "cameraRig", rig);
            u.LinkIfEmpty(journey, "thrower", player.GetComponent<BoomerangThrower>());
            u.LinkIfEmpty(journey, "score", Object.FindFirstObjectByType<ScoreManager>());
            u.LinkIfEmpty(journey, "spawner", Object.FindFirstObjectByType<SpawnManager>());
            u.LinkIfEmpty(journey, "platform", Object.FindFirstObjectByType<GuardianPlatform>());
            u.LinkIfEmpty(journey, "app", Object.FindFirstObjectByType<ApplicationController>());
            u.LinkIfEmpty(journey, "hud", hud);
            u.LinkIfEmpty(journey, "introPanel", intro);
            u.LinkIfEmpty(journey, "beginButton", begin);
            u.LinkIfEmpty(journey, "endPanel", end);
            u.LinkIfEmpty(journey, "endSummaryText", summary);
            u.LinkIfEmpty(journey, "continueButton", cont);
            u.LinkIfEmpty(journey, "quitButton", quit);
            u.LinkIfEmpty(journey, "stageText", stageText);

            return Finish(u, scene);
        }

        // ---------- Project ----------

        private static void SetProductInfo(SetupUtils u)
        {
            if (PlayerSettings.productName == "My project" || string.IsNullOrEmpty(PlayerSettings.productName))
            {
                PlayerSettings.productName = "Boomerang Guardian";
                PlayerSettings.companyName = "NYCU 3DGP";
                u.Linked("Player Settings: product name 'Boomerang Guardian', company 'NYCU 3DGP'");
            }
            else
            {
                u.Skipped($"product name '{PlayerSettings.productName}'");
            }
        }

        private static void CleanUpTemplate(SetupUtils u)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) != null)
            {
                AssetDatabase.DeleteAsset(SampleScenePath);
                u.Created($"(deleted) {SampleScenePath}");
                if (AssetDatabase.IsValidFolder("Assets/Scenes") && AssetDatabase.FindAssets("", new[] { "Assets/Scenes" }).Length == 0)
                {
                    AssetDatabase.DeleteAsset("Assets/Scenes");
                    u.Created("(deleted) empty folder Assets/Scenes");
                }
            }

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.path == MainScenePath) scenes.Add(s);
            if (scenes.Count == 0) scenes.Add(new EditorBuildSettingsScene(MainScenePath, true));
            if (scenes.Count != EditorBuildSettings.scenes.Length || !scenes[0].enabled)
            {
                scenes[0].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                u.Linked("Build Profiles scene list: Main.unity only");
            }
        }

        // ---------- Minimap ----------

        private static Mesh GetOrCreateArrowMesh(SetupUtils u)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ArrowMeshPath);
            if (mesh != null)
            {
                u.Skipped(ArrowMeshPath);
                return mesh;
            }

            // Flat arrowhead in the XZ plane pointing +Z, facing up (clockwise seen from above).
            mesh = new Mesh { name = "MinimapArrow" };
            mesh.vertices = new[] { new Vector3(0f, 0f, 1f), new Vector3(0.7f, 0f, -0.7f), new Vector3(0f, 0f, -0.35f), new Vector3(-0.7f, 0f, -0.7f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, ArrowMeshPath);
            u.Created(ArrowMeshPath);
            return mesh;
        }

        private static RenderTexture GetOrCreateRenderTexture(SetupUtils u)
        {
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (texture != null)
            {
                u.Skipped(RenderTexturePath);
                return texture;
            }
            texture = new RenderTexture(512, 512, 16) { name = "RT_Minimap", antiAliasing = 2 };
            AssetDatabase.CreateAsset(texture, RenderTexturePath);
            u.Created(RenderTexturePath);
            return texture;
        }

        private static void AddIconToPrefab(SetupUtils u, Component prefabComponent, int layer, PrimitiveType shape,
            Material material, Vector3 scale, float yaw, float height)
        {
            if (prefabComponent == null) return;
            string path = AssetDatabase.GetAssetPath(prefabComponent);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponentInChildren<MinimapIcon>(true) != null)
                {
                    u.Skipped($"minimap icon in {path}");
                    return;
                }
                CreateIcon(root.transform, layer, shape, null, material, scale, yaw, height, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                u.Created($"minimap icon in {path}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddPlayerIcon(SetupUtils u, Transform player, int layer, Mesh arrow, Material material)
        {
            if (player.GetComponentInChildren<MinimapIcon>(true) != null)
            {
                u.Skipped("player minimap icon");
                return;
            }
            GameObject icon = CreateIcon(player, layer, PrimitiveType.Quad, arrow, material, new Vector3(3f, 1f, 3f), 0f, PlayerIconY, true);
            Undo.RegisterCreatedObjectUndo(icon, "Setup");
            u.Created("Player/MinimapIcon (arrow, turns with the player)");
        }

        /// <summary>Icon root (MinimapIcon, keeps level and height) with a flat "Shape" child.</summary>
        private static GameObject CreateIcon(Transform parent, int layer, PrimitiveType shape, Mesh customMesh, Material material,
            Vector3 scale, float yaw, float height, bool followYaw)
        {
            var icon = new GameObject("MinimapIcon");
            icon.transform.SetParent(parent, false);
            icon.transform.localPosition = Vector3.up * height;
            var component = icon.AddComponent<MinimapIcon>();
            SetupUtils.SetValue(component, "worldHeight", p => p.floatValue = height);
            SetupUtils.SetValue(component, "followYaw", p => p.boolValue = followYaw);

            GameObject shapeGo;
            if (customMesh != null)
            {
                shapeGo = new GameObject("Shape", typeof(MeshFilter), typeof(MeshRenderer));
                shapeGo.GetComponent<MeshFilter>().sharedMesh = customMesh;
                shapeGo.GetComponent<MeshRenderer>().sharedMaterial = material;
                shapeGo.transform.SetParent(icon.transform, false);
                shapeGo.transform.localScale = scale;
            }
            else
            {
                shapeGo = SetupUtils.Visual(shape, icon.transform, "Shape", Vector3.zero, scale, material, new Vector3(0f, yaw, 0f));
            }
            ConfigureIconRenderer(shapeGo.GetComponent<Renderer>());
            SetupUtils.SetLayerRecursively(icon, layer);
            return icon;
        }

        private static void ConfigureIconRenderer(Renderer r)
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void AddStaticIcons(SetupUtils u, Scene scene, int layer, Material ground, Material wall, Material platform)
        {
            Transform environment = SetupUtils.FindRoot(scene, "Environment");
            if (environment != null && environment.Find("MinimapIcons") != null)
            {
                u.Skipped("Environment/MinimapIcons");
                return;
            }

            GameObject group = u.FindOrCreate(scene, "Environment/MinimapIcons");
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.gameObject.scene != scene || t.IsChildOf(group.transform)) continue;
                Renderer r = t.GetComponent<Renderer>();
                if (r == null) continue;
                Bounds b = r.bounds;

                if (t.name == "Ground")
                    StaticIcon(group.transform, layer, PrimitiveType.Cube, ground, "Icon_Ground", new Vector3(b.center.x, GroundIconY, b.center.z), new Vector3(b.size.x, 0.02f, b.size.z));
                else if (t.name.StartsWith("Wall_"))
                    StaticIcon(group.transform, layer, PrimitiveType.Cube, wall, $"Icon_{t.name}", new Vector3(b.center.x, StaticIconY, b.center.z), new Vector3(b.size.x, 0.02f, b.size.z));
                else if (t.name == "GuardianPlatform")
                    StaticIcon(group.transform, layer, PrimitiveType.Cylinder, platform, "Icon_GuardianPlatform", new Vector3(b.center.x, StaticIconY, b.center.z), new Vector3(b.size.x, 0.01f, b.size.z));
            }
            u.Created($"static minimap icons ({group.transform.childCount}: ground, walls, platform)");
        }

        private static void StaticIcon(Transform parent, int layer, PrimitiveType shape, Material material, string name, Vector3 position, Vector3 scale)
        {
            GameObject go = SetupUtils.Visual(shape, parent, name, Vector3.zero, scale, material);
            go.transform.position = position;
            go.layer = layer;
            go.isStatic = false;
            ConfigureIconRenderer(go.GetComponent<Renderer>());
        }

        private static void CreateMinimapCamera(SetupUtils u, Scene scene, int layer, RenderTexture texture, Transform player)
        {
            if (Object.FindFirstObjectByType<MinimapCamera>() != null)
            {
                u.Skipped("MinimapCamera");
                return;
            }

            GameObject go = u.FindOrCreate(scene, "Cameras/MinimapCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.05f, 1f);
            cam.cullingMask = 1 << layer;
            cam.targetTexture = texture;
            cam.depth = -10f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 100f;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            go.transform.SetPositionAndRotation(new Vector3(0f, 60f, 0f), Quaternion.Euler(90f, 0f, 0f));

            var urp = cam.GetUniversalAdditionalCameraData();
            urp.renderShadows = false;
            urp.renderPostProcessing = false;

            var follow = go.AddComponent<MinimapCamera>();
            SetupUtils.SetValue(follow, "target", p => p.objectReferenceValue = player);
            u.Created("Cameras/MinimapCamera (orthographic, Minimap layer only → RT_Minimap)");
        }

        // ---------- UI ----------

        private static void BuildMinimapPanel(SetupUtils u, Transform canvas, RenderTexture texture)
        {
            if (canvas.Find("Panel_Minimap") != null)
            {
                u.Skipped("HUD/Panel_Minimap");
                return;
            }
            RectTransform panel = Panel(canvas, "Panel_Minimap", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(340f, 390f), new Color(0f, 0f, 0f, 0.55f));

            var map = new GameObject("Minimap", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
            map.SetParent(panel, false);
            map.anchorMin = new Vector2(0f, 1f);
            map.anchorMax = new Vector2(1f, 1f);
            map.pivot = new Vector2(0.5f, 1f);
            map.anchoredPosition = new Vector2(0f, -10f);
            map.sizeDelta = new Vector2(-20f, 320f);
            var raw = map.GetComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;

            TMP_Text legend = Text(panel, "Text_Legend",
                "<color=#4DD9FF>You</color>   <color=#FF5050>Target</color>   <color=#C0C0C0>Obstacle</color>   <color=#FFD94D>Gem</color>   <color=#3380FF>Platform</color>",
                18, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform lr = legend.rectTransform;
            lr.anchorMin = new Vector2(0f, 0f);
            lr.anchorMax = new Vector2(1f, 0f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.anchoredPosition = new Vector2(0f, 8f);
            lr.sizeDelta = new Vector2(-10f, 44f);
            u.Created("HUD/Panel_Minimap (upper-right, 320 px map + legend)");
        }

        private static TMP_Text BuildStagePanel(SetupUtils u, Transform canvas)
        {
            Transform existing = canvas.Find("Panel_Journey/Text_Stage");
            if (existing != null)
            {
                u.Skipped("HUD/Panel_Journey");
                return existing.GetComponent<TMP_Text>();
            }
            RectTransform panel = Panel(canvas, "Panel_Journey", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(760f, 96f), new Color(0f, 0f, 0f, 0.45f));
            TMP_Text text = Text(panel, "Text_Stage", "Stage 1/7 · The Ordinary World", 28, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 12f);
            u.Created("HUD/Panel_Journey (stage + hint, top-center)");
            return text;
        }

        private static (GameObject, Button) BuildIntroPanel(SetupUtils u, Transform canvas)
        {
            Transform existing = canvas.Find("Panel_Intro");
            if (existing != null)
            {
                u.Skipped("HUD/Panel_Intro");
                return (existing.gameObject, existing.GetComponentInChildren<Button>(true));
            }

            RectTransform panel = Panel(canvas, "Panel_Intro", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.05f, 0.08f, 0.93f));
            Stretch(panel, 0f);

            Place(Text(panel, "Text_Title", "The Boomerang Guardian", 76, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 330f, 1400f, 100f);
            Place(Text(panel, "Text_Subtitle", "A Hero's Journey", 38, FontStyles.Italic, TextAlignmentOptions.Center), 0f, 255f, 1400f, 56f);
            Place(Text(panel, "Text_Story",
                "The peaceful realm of <b>Aurelia Plains</b> has been overwhelmed by mysterious creatures and enchanted obstacles. " +
                "The ancient <b>Guardian Platform</b> at the center of the realm, the source of balance and harmony, has weakened, " +
                "and hundreds of magical entities now appear across the land.\n\n" +
                "You are the <b>Boomerang Guardian</b>, the chosen hero destined to restore order. " +
                "Wield the legendary boomerang, which always returns to its owner, destroy the corrupted targets, and bring peace back to Aurelia.",
                28, FontStyles.Normal, TextAlignmentOptions.Center), 0f, 70f, 1300f, 280f);
            Place(Text(panel, "Text_Controls",
                "<b>W / S</b> move   ·   <b>A / D</b> strafe   ·   <b>Hold right mouse</b> look   ·   <b>SPACE</b> normal / fast\n" +
                "<b>F</b> jump   ·   <b>V</b> first / third person   ·   <b>Scroll</b> camera distance\n" +
                "<b>Left-click a target</b> throw the boomerang (3 s cooldown)   ·   <b>ESC</b> quit",
                24, FontStyles.Normal, TextAlignmentOptions.Center), 0f, -170f, 1400f, 130f);
            Button begin = MakeButton(panel, "Button_Begin", "Begin the Journey", new Vector2(0f, -320f), new Vector2(380f, 72f));
            u.Created("HUD/Panel_Intro (story, controls, Begin button)");
            return (panel.gameObject, begin);
        }

        private static (GameObject, TMP_Text, Button, Button) BuildEndPanel(SetupUtils u, Transform canvas)
        {
            Transform existing = canvas.Find("Panel_End");
            if (existing != null)
            {
                u.Skipped("HUD/Panel_End");
                Button[] buttons = existing.GetComponentsInChildren<Button>(true);
                return (existing.gameObject, existing.Find("Text_Summary")?.GetComponent<TMP_Text>(),
                    buttons.Length > 0 ? buttons[0] : null, buttons.Length > 1 ? buttons[1] : null);
            }

            RectTransform panel = Panel(canvas, "Panel_End", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 440f), new Color(0.03f, 0.05f, 0.08f, 0.93f));
            Place(Text(panel, "Text_EndTitle", "Balance Restored!", 60, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 140f, 700f, 80f);
            TMP_Text summary = Text(panel, "Text_Summary", "Score: 0", 30, FontStyles.Normal, TextAlignmentOptions.Center);
            Place(summary, 0f, 15f, 680f, 150f);
            Button cont = MakeButton(panel, "Button_Continue", "Continue", new Vector2(-170f, -150f), new Vector2(290f, 66f));
            Button quit = MakeButton(panel, "Button_Quit", "Quit", new Vector2(170f, -150f), new Vector2(290f, 66f));
            panel.gameObject.SetActive(false);
            u.Created("HUD/Panel_End (Balance Restored, Continue / Quit)");
            return (panel.gameObject, summary, cont, quit);
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            rt.GetComponent<Image>().color = color;
            return rt;
        }

        private static TMP_Text Text(Transform parent, string name, string content, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.richText = true;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(TMP_Text text, float x, float y, float width, float height)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding * 0.5f);
            rt.offsetMax = new Vector2(-padding, -padding * 0.5f);
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            rt.GetComponent<Image>().color = new Color(0.95f, 0.6f, 0.15f, 1f);
            Button button = rt.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
            colors.pressedColor = new Color(0.8f, 0.5f, 0.1f);
            button.colors = colors;

            TMP_Text text = Text(rt, "Label", label, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            text.color = new Color(0.1f, 0.07f, 0.03f);
            Stretch(text.rectTransform, 0f);
            return button;
        }

        private static string Finish(SetupUtils u, Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Boomerang Guardian: Sprint 3 setup report\n" + u.Log;
        }
    }
}
