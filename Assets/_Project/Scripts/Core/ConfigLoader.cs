using System;
using System.Collections.Generic;
using System.IO;
using BoomerangGuardian.Player;
using UnityEngine;

namespace BoomerangGuardian.Core
{
    /// <summary>
    /// Loads config.json at startup and on F5 (ADR-0008, PB-24).
    /// Lookup order:
    ///   1. next to the built app (macOS: the folder containing the .app; Editor: the project root)
    ///   2. Assets/StreamingAssets/config.json (shipped default)
    ///   3. built-in defaults
    /// Other scripts read <see cref="Current"/> and listen to <see cref="Changed"/>.
    /// When no loader is in the scene, Current is null and scripts use their Inspector values.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ConfigLoader : MonoBehaviour
    {
        public const string FileName = "config.json";

        [Tooltip("Input source for F5 (reload). Optional.")]
        [SerializeField] private PlayerInputReader input;

        public static GameConfig Current { get; private set; }
        public static string LoadedFrom { get; private set; }
        public static event Action<GameConfig> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Needed when "Enter Play Mode Options" skip the domain reload.
            Current = null;
            LoadedFrom = null;
            Changed = null;
        }

        private void Awake() => Load();

        private void OnEnable()
        {
            if (input != null) input.ReloadConfigPressed += Reload;
        }

        private void OnDisable()
        {
            if (input != null) input.ReloadConfigPressed -= Reload;
        }

        private void OnDestroy()
        {
            Current = null;
        }

        public void Reload()
        {
            Debug.Log("Reloading config (F5)…");
            Load();
        }

        private void Load()
        {
            var config = new GameConfig();
            LoadedFrom = "built-in defaults";

            foreach (string path in CandidatePaths())
            {
                if (!File.Exists(path)) continue;
                try
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
                    LoadedFrom = path;
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not parse {path}: {e.Message}. Trying the next location.");
                    config = new GameConfig();
                }
            }

            string changes = config.Validate();
            if (changes.Length > 0) Debug.LogWarning($"config.json values were clamped: {changes}");

            Current = config;
            Debug.Log($"Config loaded from: {LoadedFrom}\n{JsonUtility.ToJson(config, true)}");
            Changed?.Invoke(config);
        }

        /// <summary>Paths checked for config.json, in priority order.</summary>
        public static IEnumerable<string> CandidatePaths()
        {
            // Application.dataPath: Editor → <project>/Assets, macOS → <name>.app/Contents, Windows → <name>_Data
            string appFolder = Application.platform == RuntimePlatform.OSXPlayer
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."))
                : Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            yield return Path.Combine(appFolder, FileName);
            yield return Path.Combine(Application.streamingAssetsPath, FileName);
        }
    }
}
