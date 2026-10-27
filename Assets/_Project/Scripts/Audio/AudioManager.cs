using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using UnityEngine;

namespace BoomerangGuardian.Audio
{
    /// <summary>
    /// Plays the game's sound effects (PB-16). The five required ones are throw, hit,
    /// invalid selection, enter platform and leave platform (+ collect and catch).
    /// Any empty clip slot uses a synthesized placeholder tone, so the game is never silent.
    /// Event-driven: it never polls. It subscribes to BoomerangThrower (Thrown, TargetHit,
    /// ObstacleHit, Caught), TargetSelector.SelectionRejected, GuardianPlatform
    /// (PlayerEntered, PlayerExited) and the static Collectible.AnyCollected. Gameplay code
    /// therefore does not know that audio exists (loose coupling).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Event sources")]
        [SerializeField] private BoomerangThrower thrower;
        [SerializeField] private TargetSelector selector;
        [SerializeField] private GuardianPlatform platform;

        [Header("Clips (leave empty to use a placeholder tone)")]
        [SerializeField, OptionalReference] private AudioClip throwClip;
        [SerializeField, OptionalReference] private AudioClip hitTargetClip;
        [SerializeField, OptionalReference] private AudioClip hitObstacleClip;
        [SerializeField, OptionalReference] private AudioClip invalidSelectionClip;
        [SerializeField, OptionalReference] private AudioClip enterPlatformClip;
        [SerializeField, OptionalReference] private AudioClip leavePlatformClip;
        [SerializeField, OptionalReference] private AudioClip collectClip;
        [SerializeField, OptionalReference] private AudioClip catchClip;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

        private AudioSource source;
        // The clips actually played: the assigned clip, or its placeholder fallback (resolved once in Awake).
        private AudioClip throwSfx, hitTargetSfx, hitObstacleSfx, invalidSfx, enterSfx, leaveSfx, collectSfx, catchSfx;

        // Awake: set up our own component and clips (runs before OnEnable). [RequireComponent]
        // guarantees the AudioSource exists, so GetComponent cannot return null here.
        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // 2D UI-style sounds

            // Fallback per slot: use the Inspector clip if set, else synthesize a tone once.
            // Rising pitch = positive (throw, enter, collect), falling = return/impact,
            // low square wave = error/thud.
            throwSfx = throwClip != null ? throwClip : PlaceholderTones.Sweep("Throw", 300f, 750f, 0.2f);
            hitTargetSfx = hitTargetClip != null ? hitTargetClip : PlaceholderTones.Sweep("HitTarget", 1100f, 600f, 0.15f, PlaceholderTones.Wave.Square);
            hitObstacleSfx = hitObstacleClip != null ? hitObstacleClip : PlaceholderTones.Sweep("HitObstacle", 220f, 110f, 0.18f, PlaceholderTones.Wave.Square);
            invalidSfx = invalidSelectionClip != null ? invalidSelectionClip : PlaceholderTones.Sweep("Invalid", 160f, 130f, 0.3f, PlaceholderTones.Wave.Square);
            enterSfx = enterPlatformClip != null ? enterPlatformClip : PlaceholderTones.Sweep("EnterPlatform", 440f, 880f, 0.45f);
            leaveSfx = leavePlatformClip != null ? leavePlatformClip : PlaceholderTones.Sweep("LeavePlatform", 880f, 440f, 0.45f);
            collectSfx = collectClip != null ? collectClip : PlaceholderTones.Sweep("Collect", 1200f, 1900f, 0.15f);
            catchSfx = catchClip != null ? catchClip : PlaceholderTones.Sweep("Catch", 700f, 500f, 0.08f);
        }

        // Subscribe in OnEnable, unsubscribe in OnDisable (matched pairs), so a disabled or
        // destroyed AudioManager never receives callbacks. Static events (Collectible.AnyCollected)
        // especially need this, because they outlive individual objects.
        private void OnEnable()
        {
            if (thrower != null)
            {
                thrower.Thrown += OnThrown;
                thrower.TargetHit += OnTargetHit;
                thrower.ObstacleHit += OnObstacleHit;
                thrower.Caught += OnCaught;
            }
            if (selector != null) selector.SelectionRejected += OnSelectionRejected;
            if (platform != null)
            {
                platform.PlayerEntered += OnPlatformEntered;
                platform.PlayerExited += OnPlatformExited;
            }
            Collectible.AnyCollected += OnCollected;
        }

        private void OnDisable()
        {
            if (thrower != null)
            {
                thrower.Thrown -= OnThrown;
                thrower.TargetHit -= OnTargetHit;
                thrower.ObstacleHit -= OnObstacleHit;
                thrower.Caught -= OnCaught;
            }
            if (selector != null) selector.SelectionRejected -= OnSelectionRejected;
            if (platform != null)
            {
                platform.PlayerEntered -= OnPlatformEntered;
                platform.PlayerExited -= OnPlatformExited;
            }
            Collectible.AnyCollected -= OnCollected;
        }

        // Handlers ignore the event arguments (named _ / __); each one just plays its sound.
        private void OnThrown(BoomerangProjectile _) => Play(throwSfx);
        private void OnTargetHit(Target _) => Play(hitTargetSfx);
        private void OnObstacleHit(Obstacle _) => Play(hitObstacleSfx);
        private void OnCaught() => Play(catchSfx, 0.5f);
        private void OnSelectionRejected(SelectionResult _, string __) => Play(invalidSfx);
        private void OnPlatformEntered() => Play(enterSfx);
        private void OnPlatformExited() => Play(leaveSfx);
        private void OnCollected(Collectible _) => Play(collectSfx);

        /// <summary>
        /// PlayOneShot lets several sounds overlap on one AudioSource (e.g. a hit while the throw
        /// sound still plays) instead of cutting each other off. <paramref name="scale"/> makes a clip quieter.
        /// </summary>
        private void Play(AudioClip clip, float scale = 1f)
        {
            if (clip != null) source.PlayOneShot(clip, volume * scale);
        }
    }
}
