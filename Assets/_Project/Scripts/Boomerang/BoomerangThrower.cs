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
    /// Called by <see cref="TargetSelector"/> after a valid click. It is the single "event hub" for
    /// boomerang events: projectiles report back through the internal Report* methods, and the
    /// thrower re-raises them as C# events (AudioManager, PlayerAnimator, JourneyController listen),
    /// so listeners subscribe once here instead of to every short-lived projectile.
    /// Cooldown and speed come from config.json when loaded (ADR-0008), else the Inspector values.
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

        /// <summary>Raised right after a projectile is spawned (throw sound, throw animation, Hero's Journey stage).</summary>
        public event Action<BoomerangProjectile> Thrown;
        /// <summary>Raised when one of our boomerangs hits a target (hit sound).</summary>
        public event Action<Target> TargetHit;
        /// <summary>Raised when one of our boomerangs bounces off an obstacle (bump sound).</summary>
        public event Action<Obstacle> ObstacleHit;
        /// <summary>Raised when a boomerang returns to the hand (catch sound).</summary>
        public event Action Caught;

        /// <summary>The projectile prefab; the hover preview reads its Curve/ArcHeight from it.</summary>
        public BoomerangProjectile Prefab => boomerangPrefab;
        // "?." + "??": if no config.json was loaded, Current is null and we fall back to the Inspector value.
        public float Cooldown => ConfigLoader.Current?.boomerangCooldown ?? cooldown;
        public float Speed => ConfigLoader.Current?.boomerangSpeed ?? speed;
        /// <summary>World position where boomerangs spawn and are caught (the hand, or 1 m above the player).</summary>
        public Vector3 ThrowOrigin => hand != null ? hand.position : transform.position + Vector3.up;
        // Timestamp-based cooldown: no timer to tick down. Uses scaled Time.time, so it also freezes while paused.
        public float CooldownRemaining => Mathf.Max(0f, lastThrowTime + Cooldown - Time.time);
        /// <summary>0 right after a throw → 1 when ready.</summary>
        public float CooldownProgress => Cooldown <= 0f ? 1f : 1f - CooldownRemaining / Cooldown;
        public bool IsReady => CooldownRemaining <= 0f;

        // -Infinity so the boomerang is ready immediately at game start.
        private float lastThrowTime = float.NegativeInfinity;

        /// <summary>
        /// Spawns a boomerang aimed at <paramref name="target"/> if the cooldown allows it.
        /// Returns false (and spawns nothing) while on cooldown or without a target.
        /// </summary>
        public bool Throw(Target target)
        {
            if (!IsReady || target == null) return false;
            if (boomerangPrefab == null)
            {
                Debug.LogError($"{nameof(BoomerangThrower)}: no boomerang prefab assigned.", this);
                return false;
            }

            lastThrowTime = Time.time;   // start the cooldown
            // Face the projectile toward the target on the horizontal plane only (y = 0), so the
            // boomerang stays level. LookRotation needs a non-zero vector, hence the fallback.
            Vector3 direction = target.AimPoint - ThrowOrigin;
            direction.y = 0f;
            Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : transform.rotation;

            // One new projectile per throw; it destroys itself when caught.
            BoomerangProjectile boomerang = Instantiate(boomerangPrefab, ThrowOrigin, rotation);
            boomerang.name = $"{boomerangPrefab.name} (in flight)";
            boomerang.Launch(this, target, Speed);
            Thrown?.Invoke(boomerang);
            return true;
        }

        // Called by BoomerangProjectile (internal = same assembly only); they forward to the public events.
        internal void ReportTargetHit(Target target) => TargetHit?.Invoke(target);
        internal void ReportObstacleHit(Obstacle obstacle) => ObstacleHit?.Invoke(obstacle);
        internal void ReportCaught() => Caught?.Invoke();
    }
}
