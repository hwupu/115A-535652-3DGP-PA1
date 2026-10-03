using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Interaction;
using UnityEngine;

namespace BoomerangGuardian.Audio
{
    /// <summary>
    /// Plays the game's sound effects (PB-16). The five required ones are throw, hit,
    /// invalid selection, enter platform and leave platform (+ collect and catch).
    /// Any empty clip slot uses a synthesized placeholder tone, so the game is never silent.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Event sources")]
        [SerializeField] private BoomerangThrower thrower;
        [SerializeField] private TargetSelector selector;
        [SerializeField] private GuardianPlatform platform;

        [Header("Clips (leave empty to use a placeholder tone)")]
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private AudioClip hitTargetClip;
        [SerializeField] private AudioClip hitObstacleClip;
        [SerializeField] private AudioClip invalidSelectionClip;
        [SerializeField] private AudioClip enterPlatformClip;
        [SerializeField] private AudioClip leavePlatformClip;
        [SerializeField] private AudioClip collectClip;
        [SerializeField] private AudioClip catchClip;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

        private AudioSource source;
        private AudioClip throwSfx, hitTargetSfx, hitObstacleSfx, invalidSfx, enterSfx, leaveSfx, collectSfx, catchSfx;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // 2D UI-style sounds

            throwSfx = throwClip != null ? throwClip : PlaceholderTones.Sweep("Throw", 300f, 750f, 0.2f);
            hitTargetSfx = hitTargetClip != null ? hitTargetClip : PlaceholderTones.Sweep("HitTarget", 1100f, 600f, 0.15f, PlaceholderTones.Wave.Square);
            hitObstacleSfx = hitObstacleClip != null ? hitObstacleClip : PlaceholderTones.Sweep("HitObstacle", 220f, 110f, 0.18f, PlaceholderTones.Wave.Square);
            invalidSfx = invalidSelectionClip != null ? invalidSelectionClip : PlaceholderTones.Sweep("Invalid", 160f, 130f, 0.3f, PlaceholderTones.Wave.Square);
            enterSfx = enterPlatformClip != null ? enterPlatformClip : PlaceholderTones.Sweep("EnterPlatform", 440f, 880f, 0.45f);
            leaveSfx = leavePlatformClip != null ? leavePlatformClip : PlaceholderTones.Sweep("LeavePlatform", 880f, 440f, 0.45f);
            collectSfx = collectClip != null ? collectClip : PlaceholderTones.Sweep("Collect", 1200f, 1900f, 0.15f);
            catchSfx = catchClip != null ? catchClip : PlaceholderTones.Sweep("Catch", 700f, 500f, 0.08f);
        }

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

        private void OnThrown(BoomerangProjectile _) => Play(throwSfx);
        private void OnTargetHit(Target _) => Play(hitTargetSfx);
        private void OnObstacleHit(Obstacle _) => Play(hitObstacleSfx);
        private void OnCaught() => Play(catchSfx, 0.5f);
        private void OnSelectionRejected(SelectionResult _, string __) => Play(invalidSfx);
        private void OnPlatformEntered() => Play(enterSfx);
        private void OnPlatformExited() => Play(leaveSfx);
        private void OnCollected(Collectible _) => Play(collectSfx);

        private void Play(AudioClip clip, float scale = 1f)
        {
            if (clip != null) source.PlayOneShot(clip, volume * scale);
        }
    }
}
