using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BoomerangGuardian.EditorTools
{
    /// <summary>
    /// Standalone build (PB-21).
    /// Menu: Boomerang Guardian → Build → macOS
    /// Batch: -executeMethod BoomerangGuardian.EditorTools.BuildScript.BuildMacBatch [-buildOutput path/to/App.app]
    /// After building, a copy of config.json is placed next to the .app so it can be edited
    /// without rebuilding (ADR-0008).
    /// Editor-only code (in an Editor folder), so it is never included in the game build itself.
    /// </summary>
    public static class BuildScript
    {
        private const string DefaultOutput = "Builds/macOS/BoomerangGuardian.app";
        private const string StreamingConfig = "Assets/StreamingAssets/config.json";

        /// <summary>Menu entry: builds to the default path and shows the result in Finder.</summary>
        [MenuItem("Boomerang Guardian/Build/macOS")]
        public static void BuildMacMenu()
        {
            if (BuildMac(DefaultOutput))
                EditorUtility.RevealInFinder(DefaultOutput);
        }

        /// <summary>
        /// Command-line entry (-batchmode -executeMethod). Exits with code 1 on failure so a
        /// script can detect a broken build.
        /// </summary>
        public static void BuildMacBatch()
        {
            string output = GetArgument("-buildOutput") ?? DefaultOutput;
            if (!BuildMac(output)) EditorApplication.Exit(1);
        }

        /// <summary>Builds every enabled scene in Build Profiles for macOS; returns true on success.</summary>
        private static bool BuildMac(string output)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Build: no enabled scenes in Build Profiles.");
                return false;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Build {summary.result}: {summary.totalErrors} errors. See the log above.");
                return false;
            }

            // The build already has config.json inside StreamingAssets. Also place an editable copy
            // next to the .app (the game checks there first, ADR-0008). An existing copy is kept,
            // so rebuilding does not overwrite values the user tuned.
            string folder = Path.GetDirectoryName(Path.GetFullPath(output));
            string editableConfig = Path.Combine(folder, "config.json");
            if (!File.Exists(editableConfig) && File.Exists(StreamingConfig))
                File.Copy(StreamingConfig, editableConfig);

            Debug.Log($"Build succeeded: {Path.GetFullPath(output)} ({summary.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"{summary.totalTime.TotalSeconds:0} s). Editable config: {editableConfig}");
            return true;
        }

        /// <summary>Returns the value after a command-line flag (e.g. "-buildOutput path"), or null if absent.</summary>
        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
