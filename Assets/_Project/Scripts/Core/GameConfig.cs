using System;
using System.Text;
using UnityEngine;

namespace BoomerangGuardian.Core
{
    public enum RespawnMode
    {
        /// <summary>Generate a fresh random layout at full count.</summary>
        Regenerate,
        /// <summary>Re-show the objects that were hidden; destroyed targets stay gone.</summary>
        Restore,
    }

    /// <summary>
    /// Runtime-tunable settings read from config.json (ADR-0008).
    /// Field names are the JSON keys, so they stay lowerCamelCase.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public const int MaxTotalObjects = 500;
        public const int MinTargets = 100;
        public const int MinObstacles = 100;

        [Tooltip("\"Regenerate\" or \"Restore\" — what happens when the player leaves the platform.")]
        public string respawnMode = nameof(RespawnMode.Regenerate);
        [Tooltip("0 = different layout every run; any other value = repeatable layout.")]
        public int randomSeed = 0;

        public int targetCount = 150;
        public int obstacleCount = 150;
        public int collectibleCount = 30;

        public float normalSpeed = 5f;
        public float fastSpeed = 10f;

        public int targetScore = 10;
        public int collectibleScore = 5;

        public float maxThrowRange = 40f;
        public float boomerangSpeed = 20f;
        public float boomerangCooldown = 3f;

        public RespawnMode RespawnMode =>
            string.Equals(respawnMode, nameof(RespawnMode.Restore), StringComparison.OrdinalIgnoreCase)
                ? RespawnMode.Restore
                : RespawnMode.Regenerate;

        public int TotalObjects => targetCount + obstacleCount + collectibleCount;

        /// <summary>
        /// Clamps values to the ranges the assignment requires (200–500 objects,
        /// ≥ 100 targets, ≥ 100 obstacles) and to sane gameplay limits.
        /// Returns a description of every value that was changed (empty if none).
        /// </summary>
        public string Validate()
        {
            var changes = new StringBuilder();
            void Note<T>(string key, T before, T after)
            {
                if (!Equals(before, after)) changes.Append($"{key}: {before} → {after}; ");
            }

            string mode = RespawnMode.ToString();
            Note(nameof(respawnMode), respawnMode, mode);
            respawnMode = mode;

            int t = targetCount, o = obstacleCount, c = collectibleCount;
            targetCount = Mathf.Clamp(targetCount, MinTargets, 300);
            obstacleCount = Mathf.Clamp(obstacleCount, MinObstacles, 300);
            collectibleCount = Mathf.Clamp(collectibleCount, 0, 100);
            if (TotalObjects > MaxTotalObjects)
                collectibleCount = Mathf.Max(0, MaxTotalObjects - targetCount - obstacleCount);
            if (TotalObjects > MaxTotalObjects)
                obstacleCount = MaxTotalObjects - targetCount;
            Note(nameof(targetCount), t, targetCount);
            Note(nameof(obstacleCount), o, obstacleCount);
            Note(nameof(collectibleCount), c, collectibleCount);

            float n = normalSpeed, f = fastSpeed;
            normalSpeed = Mathf.Clamp(normalSpeed, 1f, 20f);
            fastSpeed = Mathf.Clamp(fastSpeed, normalSpeed + 1f, 40f);
            Note(nameof(normalSpeed), n, normalSpeed);
            Note(nameof(fastSpeed), f, fastSpeed);

            int ts = targetScore, cs = collectibleScore;
            targetScore = Mathf.Clamp(targetScore, 0, 1000);
            collectibleScore = Mathf.Clamp(collectibleScore, 0, 1000);
            Note(nameof(targetScore), ts, targetScore);
            Note(nameof(collectibleScore), cs, collectibleScore);

            float r = maxThrowRange, s = boomerangSpeed, cd = boomerangCooldown;
            maxThrowRange = Mathf.Clamp(maxThrowRange, 5f, 150f);
            boomerangSpeed = Mathf.Clamp(boomerangSpeed, 5f, 60f);
            boomerangCooldown = Mathf.Clamp(boomerangCooldown, 0.5f, 10f);
            Note(nameof(maxThrowRange), r, maxThrowRange);
            Note(nameof(boomerangSpeed), s, boomerangSpeed);
            Note(nameof(boomerangCooldown), cd, boomerangCooldown);

            return changes.ToString();
        }
    }
}
