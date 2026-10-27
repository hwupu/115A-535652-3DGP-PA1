using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoomerangGuardian.Player
{
    /// <summary>
    /// Single entry point for player input (ADR-0003).
    /// Wraps the "Player" map of PlayerControls.inputactions and exposes polled values
    /// plus button events, so gameplay scripts never touch the Input System directly.
    /// Two styles of input:
    /// - Continuous values (Move, LookDelta, Zoom, PointerPosition, held buttons) are polled
    ///   when needed by PlayerMotor, CameraRig, TargetSelector and TrajectoryPreview.
    /// - One-shot buttons are C# events: PlayerMotor (SPACE speed, F jump), CameraRig (V view),
    ///   TargetSelector (LMB select), ApplicationController (ESC quit), ConfigLoader (F5 reload);
    ///   JourneyController also listens to Jump for tutorial progress.
    /// JourneyController calls <see cref="SetGameplayEnabled"/> to pause gameplay input while a panel is open.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("PlayerControls.inputactions (Assets/_Project/Settings).")]
        [SerializeField] private InputActionAsset actions;

        [Tooltip("Action map to read from.")]
        [SerializeField] private string actionMapName = "Player";

        // Button events. Listeners subscribe in their OnEnable and unsubscribe in OnDisable,
        // so a destroyed listener is never called. The reader itself does not know who listens.
        public event Action ToggleSpeedPressed;
        public event Action JumpPressed;
        public event Action SwitchViewPressed;
        public event Action SelectPressed;
        public event Action QuitPressed;
        public event Action ReloadConfigPressed;

        /// <summary>
        /// False while a menu (e.g. the intro panel) is open: gameplay values read as zero and
        /// gameplay events are suppressed. Quit (ESC) and ReloadConfig (F5) keep working.
        /// </summary>
        public bool GameplayEnabled { get; private set; } = true;

        /// <summary>WASD as (x = strafe, y = forward), each in [-1, 1].</summary>
        public Vector2 Move => GameplayEnabled && move != null ? move.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Mouse movement this frame, in pixels.</summary>
        public Vector2 LookDelta => GameplayEnabled && look != null ? look.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>True while the right mouse button is held.</summary>
        public bool IsLookHeld => GameplayEnabled && lookHold != null && lookHold.IsPressed();

        /// <summary>True while the middle mouse button is held (orbit preview, PB-30).</summary>
        public bool IsOrbitHeld => GameplayEnabled && orbitHold != null && orbitHold.IsPressed();

        /// <summary>Scroll wheel delta this frame (positive = away from the user).</summary>
        public float Zoom => GameplayEnabled && zoom != null ? zoom.ReadValue<float>() : 0f;

        /// <summary>
        /// Cursor position in screen pixels, used for ray casting (click-to-throw and hover preview).
        /// Not gated by GameplayEnabled; callers check that themselves.
        /// </summary>
        public Vector2 PointerPosition => point != null ? point.ReadValue<Vector2>() : Vector2.zero;

        private InputActionMap map;
        private InputAction move, look, lookHold, orbitHold, zoom, point;
        private InputAction toggleSpeed, jump, switchView, select, quit, reloadConfig;

        // Awake runs before any OnEnable/Start, so the actions are looked up once here,
        // before other scripts can poll them. throwIfNotFound: a typo in an action name fails loudly.
        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)}: no InputActionAsset assigned.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(actionMapName, throwIfNotFound: true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            lookHold = map.FindAction("LookHold", true);
            orbitHold = map.FindAction("OrbitHold", true);
            zoom = map.FindAction("Zoom", true);
            point = map.FindAction("Point", true);
            toggleSpeed = map.FindAction("ToggleSpeed", true);
            jump = map.FindAction("Jump", true);
            switchView = map.FindAction("SwitchView", true);
            select = map.FindAction("Select", true);
            quit = map.FindAction("Quit", true);
            reloadConfig = map.FindAction("ReloadConfig", true);
        }

        private void OnEnable()
        {
            if (map == null) return;

            // "performed" fires once per button press. Actions only produce input after map.Enable().
            toggleSpeed.performed += OnToggleSpeed;
            jump.performed += OnJump;
            switchView.performed += OnSwitchView;
            select.performed += OnSelect;
            quit.performed += OnQuit;
            reloadConfig.performed += OnReloadConfig;
            map.Enable();
        }

        private void OnDisable()
        {
            if (map == null) return;

            // Mirror of OnEnable: stop the map and remove every handler, so nothing leaks.
            map.Disable();
            toggleSpeed.performed -= OnToggleSpeed;
            jump.performed -= OnJump;
            switchView.performed -= OnSwitchView;
            select.performed -= OnSelect;
            quit.performed -= OnQuit;
            reloadConfig.performed -= OnReloadConfig;
        }

        /// <summary>
        /// Turns gameplay input on/off (menus call this together with Time.timeScale = 0).
        /// The action map stays enabled, so ESC and F5 still work while paused.
        /// </summary>
        public void SetGameplayEnabled(bool value) => GameplayEnabled = value;

        // Gameplay buttons are dropped while a menu is open; Quit and ReloadConfig always pass through.
        private void OnToggleSpeed(InputAction.CallbackContext _) { if (GameplayEnabled) ToggleSpeedPressed?.Invoke(); }
        private void OnJump(InputAction.CallbackContext _) { if (GameplayEnabled) JumpPressed?.Invoke(); }
        private void OnSwitchView(InputAction.CallbackContext _) { if (GameplayEnabled) SwitchViewPressed?.Invoke(); }
        private void OnSelect(InputAction.CallbackContext _) { if (GameplayEnabled) SelectPressed?.Invoke(); }
        private void OnQuit(InputAction.CallbackContext _) => QuitPressed?.Invoke();
        private void OnReloadConfig(InputAction.CallbackContext _) => ReloadConfigPressed?.Invoke();
    }
}
