using System;
using BoomerangGuardian.Player;
using BoomerangGuardian.Spawning;
using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    /// <summary>
    /// Trigger volume over the Guardian Platform (PB-12).
    /// Enter → all targets, obstacles and collectibles disappear.
    /// Exit  → they respawn according to config.json respawnMode (Regenerate / Restore).
    /// The platform itself stays visible; this script sits on a separate trigger object.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GuardianPlatform : MonoBehaviour
    {
        [Tooltip("Spawn manager whose objects are hidden / respawned.")]
        [SerializeField] private SpawnManager spawnManager;

        public event Action PlayerEntered;
        public event Action PlayerExited;

        public bool IsPlayerOnPlatform => playerColliderCount > 0;

        // Counted per collider, so a player with several colliders doesn't toggle twice.
        private int playerColliderCount;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            if (!trigger.isTrigger)
            {
                Debug.LogWarning($"{name}: collider was not a trigger; enabling Is Trigger.", this);
                trigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            playerColliderCount++;
            if (playerColliderCount != 1) return;

            Debug.Log("Player entered the Guardian Platform: the world disappears.");
            if (spawnManager != null) spawnManager.HideAll();
            PlayerEntered?.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other) || playerColliderCount == 0) return;
            playerColliderCount--;
            if (playerColliderCount != 0) return;

            Debug.Log("Player left the Guardian Platform: the world returns.");
            if (spawnManager != null) spawnManager.Respawn();
            PlayerExited?.Invoke();
        }

        private static bool IsPlayer(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            return body != null && body.GetComponent<PlayerMotor>() != null;
        }
    }
}
