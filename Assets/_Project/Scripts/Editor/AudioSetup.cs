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
    /// Menu: Boomerang Guardian → Setup → Audio
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

        public static void RunBatch() => Debug.Log(Run());

        private static string Run()
        {
            var u = new SetupUtils();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            ConfigureImporters(u);

            AudioClip[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null)
                .ToArray();
            AudioClip Find(string prefix) => clips.FirstOrDefault(c => c.name.StartsWith(prefix));

            AudioManager manager = Object.FindFirstObjectByType<AudioManager>();
            if (manager == null) u.Warn("AudioManager not found. Run the Sprint 2 setup first.");
            else
            {
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

                settings.loadType = loadType;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = quality;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = isMusic;
                importer.SaveAndReimport();
                u.Linked($"import {file}: {loadType}, Vorbis q{quality:0.0}");
            }
        }

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

            Undo.RecordObject(source, "Setup");
            source.clip = music;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = MusicVolume;
            source.spatialBlend = 0f;
            source.priority = 0;   // music never gets culled by SFX
            u.Linked($"Systems/Music → {music.name} (loop, play on awake, volume {MusicVolume})");
        }
    }
}
