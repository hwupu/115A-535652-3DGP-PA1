using System;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using UnityEngine;

namespace BoomerangGuardian.Boomerang
{
    /// <summary>
    /// Throws boomerangs from the player's hand and enforces the cooldown (PB-07).
    /// Each throw spawns its own projectile, so the 3 s cooldown is the only gate,
    /// even if an earlier boomerang is still on its way back.
    /// </summary>
    public class BoomerangThrower : MonoBehaviour
    {
        [Tooltip("Boomerang projectile prefab.")]
        [SerializeField] private BoomerangProjectile boomerangPrefab;
        [Tooltip("Spawn / catch point (a child of the player).")]
        [SerializeField] private Transform hand;
        [Tooltip("Seconds between throws (spec: 3 s). Used when no config.json is loaded.")]
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [Tooltip("Initial boomerang speed in m/s. Used when no config.json is loaded.")]
        [SerializeField, Min(0.1f)] private float speed = 20f;

        public event Action<BoomerangProjectile> Thrown;
        public event Action<Target> TargetHit;
        public event Action<Obstacle> ObstacleHit;
        public event Action Caught;

        public BoomerangProjectile Prefab => boomerangPrefab;
        public float Cooldown => ConfigLoader.Current?.boomerangCooldown ?? cooldown;
        public float Speed => ConfigLoader.Current?.boomerangSpeed ?? speed;
        public Vector3 ThrowOrigin => hand != null ? hand.position : transform.position + Vector3.up;
        public float CooldownRemaining => Mathf.Max(0f, lastThrowTime + Cooldown - Time.time);
        /// <summary>0 right after a throw → 1 when ready.</summary>
        public float CooldownProgress => Cooldown <= 0f ? 1f : 1f - CooldownRemaining / Cooldown;
        public bool IsReady => CooldownRemaining <= 0f;

        private float lastThrowTime = float.NegativeInfinity;

        public bool Throw(Target target)
        {
            if (!IsReady || target == null) return false;
            if (boomerangPrefab == null)
            {
                Debug.LogError($"{nameof(BoomerangThrower)}: no boomerang prefab assigned.", this);
                return false;
            }

            lastThrowTime = Time.time;
            Vector3 direction = target.AimPoint - ThrowOrigin;
            direction.y = 0f;
            Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : transform.rotation;

            BoomerangProjectile boomerang = Instantiate(boomerangPrefab, ThrowOrigin, rotation);
            boomerang.name = $"{boomerangPrefab.name} (in flight)";
            boomerang.Launch(this, target, Speed);
            Thrown?.Invoke(boomerang);
            return true;
        }

        internal void ReportTargetHit(Target target) => TargetHit?.Invoke(target);
        internal void ReportObstacleHit(Obstacle obstacle) => ObstacleHit?.Invoke(obstacle);
        internal void ReportCaught() => Caught?.Invoke();
    }
}
