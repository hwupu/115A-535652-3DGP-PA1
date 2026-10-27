using System;
using BoomerangGuardian.Interaction;
using UnityEngine;

namespace BoomerangGuardian.Core
{
    /// <summary>
    /// Keeps the score (PB-14): targets hit by a boomerang and collectibles picked up.
    /// Point values come from config.json, or from the Inspector when no config is loaded.
    /// Decoupled by events: it listens to the static Target.AnyHit and Collectible.AnyCollected,
    /// so it needs no reference to the hundreds of spawned objects. HudController listens to
    /// <see cref="ScoreChanged"/> to update the score text.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Tooltip("Points per target hit (used when no config.json is loaded).")]
        [SerializeField, Min(0)] private int targetScore = 10;
        [Tooltip("Points per collectible (used when no config.json is loaded).")]
        [SerializeField, Min(0)] private int collectibleScore = 5;

        /// <summary>Raised with (new score, points added).</summary>
        public event Action<int, int> ScoreChanged;

        public int Score { get; private set; }
        public int TargetsHit { get; private set; }
        public int CollectiblesCollected { get; private set; }

        // Read on every hit (not cached), so an F5 config reload takes effect immediately.
        public int PointsPerTarget => ConfigLoader.Current?.targetScore ?? targetScore;
        public int PointsPerCollectible => ConfigLoader.Current?.collectibleScore ?? collectibleScore;

        // Static events outlive this object, so always unsubscribe in OnDisable;
        // otherwise a destroyed ScoreManager would still be called (and leak).
        private void OnEnable()
        {
            Target.AnyHit += OnTargetHit;
            Collectible.AnyCollected += OnCollected;
        }

        private void OnDisable()
        {
            Target.AnyHit -= OnTargetHit;
            Collectible.AnyCollected -= OnCollected;
        }

        private void OnTargetHit(Target target)
        {
            TargetsHit++;
            Add(PointsPerTarget);
        }

        private void OnCollected(Collectible collectible)
        {
            CollectiblesCollected++;
            Add(PointsPerCollectible);
        }

        /// <summary>Single place where the score changes, so the UI event is always raised.</summary>
        private void Add(int points)
        {
            Score += points;
            ScoreChanged?.Invoke(Score, points);
        }
    }
}
