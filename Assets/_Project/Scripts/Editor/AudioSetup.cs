using System.Linq;
using BoomerangGuardian.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Audio setup (PB-16, ADR-0009). Assigns clips in Assets/_Project/Audio to the AudioManager by
    /// file-name prefix, creates a looping background-music source, and sets import options.
    /// Idempotent: only fills empty slots, so manual choices in the Inspector are kept.
    /// Slots with no matching file stay empty, and the AudioManager then plays a synthesized placeholder tone.
    /// Menu: Boomerang Guardian → Setup → Audio
    /// Batch: -executeMethod BoomerangGuardian.EditorTools.AudioSetup.RunBatch
    /// </summary>
    public static class AudioSetup
    {
        private const string MainScenePath = SetupUtils.ProjectRoot + "/Scenes/Main.unity";
        private const string AudioFolder = SetupUtils.ProjectRoot + "/Audio";
        private const string MusicPrefix = "bgm_";
        private const float MusicVolume = 0.3f;

        // AudioManager field ← file-name prefix. "Boomerang hits an object" uses one clip for both hit slots.
        private static readonly (string field, string prefix)[] Slots =
        {
            ("throwClip", "sfx_throw_"),
            ("hitTargetClip", "sfx_hit_"),
            ("hitObstacleClip", "sfx_hit_"),
            ("invalidSelectionClip", "sfx_invalid_"),
            ("enterPlatformClip", "sfx_platform_enter_"),
            ("leavePlatformClip", "sfx_platform_leave_"),
            ("collectClip", "sfx_collect_"),
            ("catchClip", "sfx_catch_"),
        };

        /// <summary>Menu entry: refuses to run in Play Mode and lets the user save open scene changes first.</summary>
        [MenuItem("Boomerang Guardian/Setup/Audio")]
        public static void RunFromMenu()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Audio setup", "Exit Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log(Run());
            EditorUtility.DisplayDialog("Audio setup finished", "Done. See the Console for the report.", "OK");
        }

        /// <summary>Entry point for -executeMethod (no dialogs, so it works in -batchmode).</summary>
        public static void RunBatch() => Debug.Log(Run());

        /// <summary>Sets import options, fills the AudioManager clip slots, sets up music, and returns the report.</summary>
        private static string Run()
        {
            var u = new SetupUtils();
            // Open the scene first: OpenScene unloads in-memory assets created earlier in a run.
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            ConfigureImporters(u);

            // Find all AudioClip assets in the audio folder (the search returns GUIDs, so map
            // GUID → path → loaded clip). Find(prefix) is a local function returning the first match.
            AudioClip[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null)
                .ToArray();
            AudioClip Find(string prefix) => clips.FirstOrDefault(c => c.name.StartsWith(prefix));

            AudioManager manager = Object.FindFirstObjectByType<AudioManager>();
            if (manager == null) u.Warn("AudioManager not found. Run the Sprint 2 setup first.");
            else
            {
                // LinkIfEmpty never overwrites a clip already chosen in the Inspector.
                foreach ((string field, string prefix) in Slots)
                {
                    AudioClip clip = Find(prefix);
                    if (clip != null) u.LinkIfEmpty(manager, field, clip);
                    else u.Skipped($"AudioManager.{field}: no '{prefix}*' file → placeholder tone");
                }
            }

            SetUpMusic(u, scene, Find(MusicPrefix));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Boomerang Guardian: Audio setup report\n" + u.Log + WiringValidator.Report();
        }

        /// <summary>
        /// Sets import options for bgm_* and sfx_* files: Vorbis compression; music streams from disk,
        /// SFX are decompressed in memory. Files already set this way are skipped (no needless reimport).
        /// </summary>
        private static void ConfigureImporters(SetupUtils u)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileName(path);
                bool isMusic = file.StartsWith(MusicPrefix);
                if (!isMusic && !file.StartsWith("sfx_")) continue;   // leave originals / other files alone

                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                // Music is long (13 min): stream it. SFX are short: decompress once for instant playback.
                AudioClipLoadType loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                float quality = isMusic ? 0.5f : 0.7f;
                if (settings.loadType == loadType && settings.compressionFormat == AudioCompressionFormat.Vorbis &&
                    Mathf.Approximately(settings.quality, quality) && importer.loadInBackground == isMusic)
                {
                    u.Skipped($"import settings {file}");
                    continue;
                }

                // AudioImporterSampleSettings is a struct: edit the copy, then assign it back.
                settings.loadType = loadType;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = quality;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = isMusic;
                importer.SaveAndReimport();
                u.Linked($"import {file}: {loadType}, Vorbis q{quality:0.0}");
            }
        }

        /// <summary>
        /// Creates Systems/Music with a looping 2D AudioSource that starts on Awake. It is separate from
        /// the AudioManager's SFX source, so music volume and priority are independent.
        /// </summary>
        private static void SetUpMusic(SetupUtils u, Scene scene, AudioClip music)
        {
            if (music == null)
            {
                u.Skipped($"music: no '{MusicPrefix}*' file");
                return;
            }

            GameObject go = u.FindOrCreate(scene, "Systems/Music");
            AudioSource source = u.GetOrAdd<AudioSource>(go);
            if (source.clip != null) return;   // keep manual choices

            // Undo.RecordObject makes the change undoable and marks the component as modified.
            Undo.RecordObject(source, "Setup");
            source.clip = music;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = MusicVolume;
            source.spatialBlend = 0f;   // 0 = fully 2D: same loudness wherever the listener is
            source.priority = 0;   // music never gets culled by SFX
            u.Linked($"Systems/Music → {music.name} (loop, play on awake, volume {MusicVolume})");
        }
    }
}
