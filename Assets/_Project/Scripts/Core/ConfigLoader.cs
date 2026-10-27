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
    ///
    /// Readers of Current: SpawnManager (counts, seed, respawnMode), ScoreManager (points),
    /// PlayerMotor (speeds, also subscribes to Changed), BoomerangThrower and TargetSelector
    /// (range, speed, cooldown). F5 comes from PlayerInputReader.ReloadConfigPressed.
    /// DefaultExecutionOrder(-100) makes this Awake run before other scripts' Awake/Start,
    /// so the config is ready when they first read it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ConfigLoader : MonoBehaviour
    {
        public const string FileName = "config.json";

        [Tooltip("Input source for F5 (reload). Optional.")]
        [SerializeField, OptionalReference] private PlayerInputReader input;

        // Static so any script can read the config without a scene reference.
        /// <summary>The active config, or null when no ConfigLoader exists (callers then use "?? inspectorValue").</summary>
        public static GameConfig Current { get; private set; }
        /// <summary>File path the config came from, or "built-in defaults".</summary>
        public static string LoadedFrom { get; private set; }
        /// <summary>Raised after every load (startup and F5) with the new config.</summary>
        public static event Action<GameConfig> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Needed when "Enter Play Mode Options" skip the domain reload.
            Current = null;
            LoadedFrom = null;
            Changed = null;
        }

        // Load in Awake (earliest callback, plus execution order -100) so others see the config in their Start.
        private void Awake() => Load();

        // Subscribe in OnEnable / unsubscribe in OnDisable: the standard pair, so a disabled or
        // destroyed loader never receives events (no dangling delegates).
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
            Current = null;   // don't leave a stale static config behind after the scene unloads
        }

        /// <summary>Re-reads config.json at runtime (F5), so values can be tuned without rebuilding.</summary>
        public void Reload()
        {
            Debug.Log("Reloading config (F5)…");
            Load();
        }

        /// <summary>Builds a fresh GameConfig, fills it from the first readable file, validates it, then publishes it.</summary>
        private void Load()
        {
            // Start from the C# field defaults; this is the fallback if no file is found.
            var config = new GameConfig();
            LoadedFrom = "built-in defaults";

            foreach (string path in CandidatePaths())
            {
                if (!File.Exists(path)) continue;
                try
                {
                    // FromJsonOverwrite only sets keys present in the file; missing keys keep their defaults.
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
                    LoadedFrom = path;
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not parse {path}: {e.Message}. Trying the next location.");
                    config = new GameConfig();   // discard any half-applied values from the broken file
                }
            }

            // Clamp to the assignment's limits (e.g. 200–500 objects) so a bad file can't break the game.
            string changes = config.Validate();
            if (changes.Length > 0) Debug.LogWarning($"config.json values were clamped: {changes}");

            Current = config;
            Debug.Log($"Config loaded from: {LoadedFrom}\n{JsonUtility.ToJson(config, true)}");
            Changed?.Invoke(config);   // "?." = only if someone is subscribed
        }

        /// <summary>Paths checked for config.json, in priority order.</summary>
        public static IEnumerable<string> CandidatePaths()
        {
            // Application.dataPath: Editor → <project>/Assets, macOS → <name>.app/Contents, Windows → <name>_Data
            string appFolder = Application.platform == RuntimePlatform.OSXPlayer
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."))
                : Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            // 1. Editable copy next to the .app (the build script places one there).
            yield return Path.Combine(appFolder, FileName);
            // 2. Shipped default: StreamingAssets is copied into the build unchanged.
            yield return Path.Combine(Application.streamingAssetsPath, FileName);
        }
    }
}
