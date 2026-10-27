using System;
using BoomerangGuardian.Boomerang;
using BoomerangGuardian.Cameras;
using BoomerangGuardian.Core;
using BoomerangGuardian.Interaction;
using BoomerangGuardian.Player;
using BoomerangGuardian.Spawning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoomerangGuardian.UI
{
    /// <summary>
    /// The seven Hero's Journey stages, in story order. The enum order matters:
    /// stages are compared with &lt; / &gt;= and cast to int to index the name/message arrays.
    /// </summary>
    public enum JourneyStage
    {
        OrdinaryWorld,
        CallToAdventure,
        CrossingTheThreshold,
        TrialsAndChallenges,
        MasteryOfSkills,
        TheOrdeal,
        TheReturn,
    }

    /// <summary>
    /// The Hero's Journey framing from the assignment story (PB-19).
    /// - Intro panel (story + controls). Gameplay input is paused until "Begin".
    /// - Seven stages that advance through play and are shown at the top of the HUD:
    ///   Ordinary World → Call to Adventure (begin) → Crossing the Threshold (first throw)
    ///   → Trials and Challenges (first hit) → Mastery of Skills (jump, fast speed, both views,
    ///   N hits) → The Ordeal (enter the platform) → The Return (back in the world with M hits,
    ///   or every target cleared).
    /// - End panel "Balance Restored" with the score, Continue or Quit.
    /// How it works: a small one-way state machine. It listens to gameplay events
    /// (PlayerInputReader.JumpPressed, PlayerMotor.SpeedModeChanged, CameraRig.ModeChanged,
    /// BoomerangThrower.Thrown, GuardianPlatform.PlayerEntered, static Target.AnyHit) and
    /// calls <see cref="Advance"/>. Conditions that are not events (leaving the platform with
    /// enough hits, all targets cleared) are polled in Update(). Each new stage is announced
    /// through HudController.ShowMessage and the <see cref="StageChanged"/> event.
    /// Pausing (panels open): PlayerInputReader.SetGameplayEnabled(false) + Time.timeScale = 0.
    /// </summary>
    public class JourneyController : MonoBehaviour
    {
        [Header("Game references")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private BoomerangThrower thrower;
        [SerializeField] private ScoreManager score;
        [SerializeField] private SpawnManager spawner;
        [SerializeField] private GuardianPlatform platform;
        [SerializeField] private ApplicationController app;
        [SerializeField] private HudController hud;

        [Header("UI")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private Button beginButton;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private TMP_Text endSummaryText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Stage title + hint at the top of the screen.")]
        [SerializeField] private TMP_Text stageText;

        [Header("Goals")]
        [Tooltip("Show the story / controls panel at start.")]
        [SerializeField] private bool showIntro = true;
        [Tooltip("Targets to hit (with jump, fast speed and both views used) to reach Mastery of Skills.")]
        [SerializeField, Min(1)] private int masteryTargetHits = 10;
        [Tooltip("Total targets hit needed after the Ordeal to complete The Return.")]
        [SerializeField, Min(1)] private int returnTargetHits = 25;
        [SerializeField] private Color stageMessageColor = new Color(1f, 0.85f, 0.4f);

        /// <summary>Raised once each time the journey moves to a new stage.</summary>
        public event Action<JourneyStage> StageChanged;

        /// <summary>Current stage. Public read, private write: only this class can advance it.</summary>
        public JourneyStage Stage { get; private set; } = JourneyStage.OrdinaryWorld;

        // Skill checklist for "Mastery of Skills"; each flag is set once by its event handler and never cleared.
        private bool usedJump, usedFast, usedFirstPerson, usedThirdPerson;
        private float startTime;
        private string lastStageText;

        // Indexed by (int)JourneyStage, so the order must match the enum.
        private static readonly string[] StageNames =
        {
            "The Ordinary World", "The Call to Adventure", "Crossing the Threshold",
            "Trials and Challenges", "Mastery of Skills", "The Ordeal", "The Return",
        };

        private static readonly string[] StageMessages =
        {
            "You begin as a novice guardian on the plains of Aurelia.",
            "The realm grows unstable: hundreds of objects appear across the land!",
            "You wield the legendary boomerang. It always returns to its owner.",
            "Destroy the corrupted targets. Navigate the obstacles, collect the gems.",
            "You have mastered movement, jumping, perspective and targeting.",
            "The Guardian Platform tests you: the world disappears!",
            "Balance is restored. Peace returns to Aurelia Plains.",
        };

        // Subscribe in OnEnable, unsubscribe in OnDisable (matched pairs). UI buttons use UnityEvent
        // (AddListener/RemoveListener); game components use C# events (+= / -=).
        // Target.AnyHit is static (one event for all targets), so we never need a reference to each spawned target.
        private void OnEnable()
        {
            if (beginButton != null) beginButton.onClick.AddListener(Begin);
            if (continueButton != null) continueButton.onClick.AddListener(CloseEndPanel);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (input != null) input.JumpPressed += OnJump;
            if (motor != null) motor.SpeedModeChanged += OnSpeedChanged;
            if (cameraRig != null) cameraRig.ModeChanged += OnViewChanged;
            if (thrower != null) thrower.Thrown += OnThrown;
            if (platform != null) platform.PlayerEntered += OnPlatformEntered;
            Target.AnyHit += OnTargetHit;
        }

        private void OnDisable()
        {
            if (beginButton != null) beginButton.onClick.RemoveListener(Begin);
            if (continueButton != null) continueButton.onClick.RemoveListener(CloseEndPanel);
            if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
            if (input != null) input.JumpPressed -= OnJump;
            if (motor != null) motor.SpeedModeChanged -= OnSpeedChanged;
            if (cameraRig != null) cameraRig.ModeChanged -= OnViewChanged;
            if (thrower != null) thrower.Thrown -= OnThrown;
            if (platform != null) platform.PlayerEntered -= OnPlatformEntered;
            Target.AnyHit -= OnTargetHit;
        }

        // Start (not Awake) runs after every object's Awake/OnEnable, so the referenced
        // components (e.g. CameraRig.Mode) are initialised before we read them.
        private void Start()
        {
            if (endPanel != null) endPanel.SetActive(false);
            if (cameraRig != null) OnViewChanged(cameraRig.Mode);   // the starting view counts as used

            if (showIntro && introPanel != null)
            {
                introPanel.SetActive(true);
                SetGameplayPaused(true);   // freeze the world until the player clicks "Begin"
            }
            else
            {
                // Intro disabled (e.g. for quick testing): start the journey right away.
                if (introPanel != null) introPanel.SetActive(false);
                Begin();
            }
            RefreshStageText();
        }

        // Polls the two conditions that have no single event: they combine several states.
        private void Update()
        {
            // The Return: back in the world after the Ordeal with enough hits, or every target cleared.
            if (Stage == JourneyStage.TheOrdeal && platform != null && !platform.IsPlayerOnPlatform
                && score != null && score.TargetsHit >= returnTargetHits)
                Advance(JourneyStage.TheReturn);

            // Shortcut ending: every spawned target destroyed. IsHidden is excluded because hidden
            // objects (player on the platform) must not count as cleared; TargetsSpawned > 0 avoids
            // ending before the spawner has run.
            if (Stage >= JourneyStage.TrialsAndChallenges && Stage < JourneyStage.TheReturn && spawner != null
                && !spawner.IsHidden && spawner.TargetsSpawned > 0 && spawner.TargetsRemaining == 0)
                Advance(JourneyStage.TheReturn);

            RefreshStageText();
        }

        /// <summary>"Begin" button: hides the intro, unpauses gameplay, starts the play timer, enters Call to Adventure.</summary>
        public void Begin()
        {
            if (introPanel != null) introPanel.SetActive(false);
            SetGameplayPaused(false);
            startTime = Time.time;
            Advance(JourneyStage.CallToAdventure);
        }

        /// <summary>
        /// Moves to <paramref name="next"/> if it is later than the current stage. Because of the
        /// forward-only guard, handlers can call this freely (e.g. on every hit) without repeating stages.
        /// </summary>
        private void Advance(JourneyStage next)
        {
            if (next <= Stage) return;   // stages only move forward
            Stage = next;
            Debug.Log($"Hero's Journey: stage {(int)next + 1}/7 — {StageNames[(int)next]}");
            if (hud != null) hud.ShowMessage(StageMessages[(int)next], stageMessageColor);
            StageChanged?.Invoke(next);
            RefreshStageText();

            if (next == JourneyStage.TheReturn) ShowEndPanel();
        }

        /// <summary>Called after every skill event and hit: Mastery needs all four skills used and enough hits.</summary>
        private void CheckMastery()
        {
            if (Stage != JourneyStage.TrialsAndChallenges || score == null) return;
            if (usedJump && usedFast && usedFirstPerson && usedThirdPerson && score.TargetsHit >= masteryTargetHits)
                Advance(JourneyStage.MasteryOfSkills);
        }

        /// <summary>Top-of-screen stage title plus a hint, using TextMeshPro rich-text tags (&lt;b&gt;, &lt;size&gt;).</summary>
        private void RefreshStageText()
        {
            if (stageText == null) return;
            string text = $"<b>Stage {(int)Stage + 1}/7 · {StageNames[(int)Stage]}</b>\n<size=75%>{Hint()}</size>";
            if (text == lastStageText) return;   // avoid rebuilding the text mesh every frame
            lastStageText = text;
            stageText.text = text;
        }

        /// <summary>What the player should do next in the current stage, with live progress (checkboxes, hit counts).</summary>
        private string Hint()
        {
            int hits = score != null ? score.TargetsHit : 0;
            switch (Stage)
            {
                case JourneyStage.OrdinaryWorld: return "Read the story, then begin your journey.";
                case JourneyStage.CallToAdventure: return "Left-click a red target to throw your boomerang.";
                case JourneyStage.CrossingTheThreshold: return "Hit a target with your boomerang.";
                case JourneyStage.TrialsAndChallenges:
                    return $"Master your skills: Jump (F) {Check(usedJump)}   Fast (SPACE) {Check(usedFast)}   " +
                           $"Both views (V) {Check(usedFirstPerson && usedThirdPerson)}   Hits {Mathf.Min(hits, masteryTargetHits)}/{masteryTargetHits}";
                case JourneyStage.MasteryOfSkills: return "Step onto the Guardian Platform at the center of the realm.";
                case JourneyStage.TheOrdeal:
                    return platform != null && platform.IsPlayerOnPlatform
                        ? "Leave the platform to restore the world."
                        : $"Restore balance: hit targets ({Mathf.Min(hits, returnTargetHits)}/{returnTargetHits}).";
                default: return "Peace returns to Aurelia Plains. Keep playing, or press ESC to quit.";
            }
        }

        private static string Check(bool done) => done ? "<color=#7CFC7C>[x]</color>" : "[ ]";

        /// <summary>"Balance Restored" summary (score, hits, gems, play time); pauses the game behind it.</summary>
        private void ShowEndPanel()
        {
            if (endPanel == null) return;
            // Play time since Begin(). Time.time is scaled, so time spent paused is not counted.
            float minutes = (Time.time - startTime) / 60f;
            if (endSummaryText != null && score != null)
                endSummaryText.text = $"Score: {score.Score}\nTargets hit: {score.TargetsHit}   Gems: {score.CollectiblesCollected}\nTime: {minutes:0.0} min";
            endPanel.SetActive(true);
            SetGameplayPaused(true);
        }

        /// <summary>"Continue": close the end panel and keep playing in the same world (stage stays at The Return).</summary>
        private void CloseEndPanel()
        {
            if (endPanel != null) endPanel.SetActive(false);
            SetGameplayPaused(false);
        }

        // Reuses ApplicationController.Quit (same path as ESC): exits Play Mode in the Editor, quits in a build.
        private void Quit()
        {
            if (app != null) app.Quit();
        }

        /// <summary>
        /// Project-wide pause pattern. Two parts:
        /// 1) SetGameplayEnabled(false) mutes gameplay input (move, jump, throw...) while
        ///    ESC (quit) and F5 (reload config) keep working.
        /// 2) Time.timeScale = 0 freezes physics, scaled Time.deltaTime and Time.time, so the world stops.
        /// The UI buttons still work: the uGUI EventSystem handles clicks in Update, which does not depend on timeScale.
        /// </summary>
        private void SetGameplayPaused(bool paused)
        {
            if (input != null) input.SetGameplayEnabled(!paused);
            Time.timeScale = paused ? 0f : 1f;
        }

        // Skill-event handlers: record the skill, then re-check the Mastery condition.
        private void OnJump() { usedJump = true; CheckMastery(); }
        private void OnSpeedChanged(bool fast) { if (fast) usedFast = true; CheckMastery(); }

        private void OnViewChanged(CameraMode mode)
        {
            if (mode == CameraMode.FirstPerson) usedFirstPerson = true;
            else usedThirdPerson = true;
            CheckMastery();
        }

        private void OnThrown(BoomerangProjectile _) => Advance(JourneyStage.CrossingTheThreshold);

        // Any target hit: the first one enters Trials (Advance ignores later calls), and each hit may complete Mastery.
        private void OnTargetHit(Target _)
        {
            Advance(JourneyStage.TrialsAndChallenges);
            CheckMastery();
        }

        // The Ordeal only starts once Mastery is reached; stepping on the platform earlier does not skip stages.
        private void OnPlatformEntered()
        {
            if (Stage == JourneyStage.MasteryOfSkills) Advance(JourneyStage.TheOrdeal);
        }

        private void OnDestroy()
        {
            // timeScale is global and outlives this object: reset it so stopping Play Mode
            // while a panel is open does not leave the Editor (or the next scene) frozen.
            Time.timeScale = 1f;   // never leave the Editor paused
        }
    }
}
