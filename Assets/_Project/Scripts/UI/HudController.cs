using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using BoomerangGuardian.Player;
using BoomerangGuardian.Spawning;
using TMPro;
using UnityEngine;

namespace BoomerangGuardian.UI
{
    /// <summary>
    /// Heads-up display (PB-14): score, targets hit / remaining, speed + camera mode,
    /// boomerang cooldown bar, and short messages (invalid selection, hits, platform).
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private ScoreManager score;
        [SerializeField] private SpawnManager spawner;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private BoomerangThrower thrower;
        [SerializeField] private TargetSelector selector;
        [SerializeField] private GuardianPlatform platform;

        [Header("Widgets")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text targetsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text cooldownText;
        [Tooltip("Fill bar; its right anchor is moved from 0 (just thrown) to 1 (ready).")]
        [SerializeField] private RectTransform cooldownFill;
        [SerializeField] private TMP_Text messageText;

        [Header("Messages")]
        [Tooltip("Seconds a message stays fully visible before fading.")]
        [SerializeField, Min(0f)] private float messageDuration = 1.5f;
        [SerializeField, Min(0.01f)] private float messageFade = 0.5f;
        [SerializeField] private Color errorColor = new Color(1f, 0.4f, 0.35f);
        [SerializeField] private Color goodColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private Color infoColor = new Color(0.6f, 0.85f, 1f);

        private float messageTime = float.NegativeInfinity;

        private void OnEnable()
        {
            if (selector != null) selector.SelectionRejected += OnSelectionRejected;
            if (score != null) score.ScoreChanged += OnScoreChanged;
            if (platform != null)
            {
                platform.PlayerEntered += OnPlatformEntered;
                platform.PlayerExited += OnPlatformExited;
            }
        }

        private void OnDisable()
        {
            if (selector != null) selector.SelectionRejected -= OnSelectionRejected;
            if (score != null) score.ScoreChanged -= OnScoreChanged;
            if (platform != null)
            {
                platform.PlayerEntered -= OnPlatformEntered;
                platform.PlayerExited -= OnPlatformExited;
            }
        }

        private void Start()
        {
            if (messageText != null) messageText.alpha = 0f;
        }

        private void Update()
        {
            if (scoreText != null && score != null)
                scoreText.SetText("Score: {0}", score.Score);

            if (targetsText != null && spawner != null && score != null)
            {
                if (spawner.IsHidden) targetsText.SetText("The world has vanished. Leave the platform!");
                else targetsText.SetText("Targets hit: {0}   Remaining: {1}", score.TargetsHit, spawner.TargetsRemaining);
            }

            if (statusText != null && motor != null && cameraRig != null)
            {
                string speed = motor.IsFast ? "FAST" : "Normal";
                string view = cameraRig.Mode == CameraMode.FirstPerson ? "1st person" : "3rd person";
                statusText.text = $"Speed: {speed}   View: {view}";
            }

            UpdateCooldown();
            UpdateMessageFade();
        }

        private void UpdateCooldown()
        {
            if (thrower == null) return;
            if (cooldownFill != null)
            {
                Vector2 max = cooldownFill.anchorMax;
                max.x = thrower.CooldownProgress;
                cooldownFill.anchorMax = max;
            }
            if (cooldownText != null)
            {
                if (thrower.IsReady) cooldownText.SetText("Boomerang ready");
                else cooldownText.SetText("Cooldown {0:1} s", thrower.CooldownRemaining);
            }
        }

        private void UpdateMessageFade()
        {
            if (messageText == null) return;
            float age = Time.time - messageTime;
            messageText.alpha = age <= messageDuration ? 1f : Mathf.Clamp01(1f - (age - messageDuration) / messageFade);
        }

        public void ShowMessage(string message, Color color)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageText.color = color;
            messageTime = Time.time;
        }

        private void OnSelectionRejected(SelectionResult reason, string message) => ShowMessage(message, errorColor);
        private void OnScoreChanged(int total, int added) => ShowMessage($"+{added}", goodColor);
        private void OnPlatformEntered() => ShowMessage("The Guardian Platform hides the world…", infoColor);
        private void OnPlatformExited() => ShowMessage("The world returns!", infoColor);
    }
}
