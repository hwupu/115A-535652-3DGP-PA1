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
    /// Two data paths:
    /// - Polling: Update() reads continuously changing values (score, counts, speed, view,
    ///   cooldown) from the source components every frame.
    /// - Events: one-off moments (TargetSelector.SelectionRejected, ScoreManager.ScoreChanged,
    ///   GuardianPlatform.PlayerEntered/PlayerExited) trigger a fading message.
    /// JourneyController also calls <see cref="ShowMessage"/> for stage announcements.
    /// The HUD only reads game state; it never changes it.
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

        // Time the current message was shown. -Infinity means "very old", so no message is visible at start.
        private float messageTime = float.NegativeInfinity;

        // Subscribe in OnEnable and unsubscribe in OnDisable, a matched pair. This avoids
        // callbacks into a disabled or destroyed HUD and avoids double subscriptions.
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
            // SetText with {0} placeholders formats numbers without allocating a new string each frame.
            if (scoreText != null && score != null)
                scoreText.SetText("Score: {0}", score.Score);

            if (targetsText != null && spawner != null && score != null)
            {
                // While the player stands on the Guardian Platform the SpawnManager hides every object.
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

        /// <summary>Draws the cooldown bar and label from BoomerangThrower's cooldown state.</summary>
        private void UpdateCooldown()
        {
            if (thrower == null) return;
            if (cooldownFill != null)
            {
                // The bar is stretched by its anchors, not by scale: anchorMax.x is the right edge
                // as a fraction of the parent width. CooldownProgress goes 0 (just thrown) to 1 (ready).
                // anchorMax is a struct, so copy it, change x, and assign it back.
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

        /// <summary>Message alpha: fully visible for messageDuration, then a linear fade over messageFade seconds.</summary>
        private void UpdateMessageFade()
        {
            if (messageText == null) return;
            // Time.time is scaled time, so a message freezes (does not fade) while the game is paused.
            float age = Time.time - messageTime;
            // After the hold time, (age - duration) / fade goes 0 → 1, so alpha goes 1 → 0; Clamp01 keeps it in range.
            messageText.alpha = age <= messageDuration ? 1f : Mathf.Clamp01(1f - (age - messageDuration) / messageFade);
        }

        /// <summary>
        /// Shows a short message in the given color. Resetting messageTime restarts the
        /// hold-then-fade timer, so a new message replaces the old one. Also used by JourneyController.
        /// </summary>
        public void ShowMessage(string message, Color color)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageText.color = color;
            messageTime = Time.time;
        }

        // Event handlers: each one-off game event becomes a colored message (red = error, green = points, blue = info).
        private void OnSelectionRejected(SelectionResult reason, string message) => ShowMessage(message, errorColor);
        private void OnScoreChanged(int total, int added) => ShowMessage($"+{added}", goodColor);
        private void OnPlatformEntered() => ShowMessage("The Guardian Platform hides the world…", infoColor);
        private void OnPlatformExited() => ShowMessage("The world returns!", infoColor);
    }
}
