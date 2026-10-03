using System.Collections.Generic;
using BoomerangGuardian.Interaction;
using UnityEngine;

namespace BoomerangGuardian.Spawning
{
    /// <summary>
    /// Which prefabs the SpawnManager uses (ADR-0007: theme-agnostic content).
    /// Swap the greybox prefabs for themed ones here, with no code changes.
    /// Prefab convention: the root pivot sits at the ground contact point (bottom center).
    /// </summary>
    [CreateAssetMenu(menuName = "Boomerang Guardian/Spawn Config", fileName = "SpawnConfig")]
    public class SpawnConfig : ScriptableObject
    {
        [Tooltip("Target prefabs; one is picked at random for each spawn.")]
        [SerializeField] private Target[] targetPrefabs = new Target[0];
        [Tooltip("Obstacle prefabs; one is picked at random for each spawn.")]
        [SerializeField] private Obstacle[] obstaclePrefabs = new Obstacle[0];
        [Tooltip("Collectible prefabs; one is picked at random for each spawn.")]
        [SerializeField] private Collectible[] collectiblePrefabs = new Collectible[0];

        public IReadOnlyList<Target> TargetPrefabs => targetPrefabs;
        public IReadOnlyList<Obstacle> ObstaclePrefabs => obstaclePrefabs;
        public IReadOnlyList<Collectible> CollectiblePrefabs => collectiblePrefabs;
    }
}
