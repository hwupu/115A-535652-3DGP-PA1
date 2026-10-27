using BoomerangGuardian.Boomerang;
using UnityEngine;

namespace BoomerangGuardian.Player
{
    /// <summary>
    /// Drives the character's Animator from gameplay (PB-28, ADR-0011). Kept deliberately simple:
    /// physics moves the player; the animation only follows. Speed → Idle/Walk/Run blend,
    /// jump → Jump clip, throw → Throw clip. Known, accepted glitches: some foot sliding,
    /// a slightly doubled jump hop, and the throw pose isn't synced to the boomerang release.
    /// Talks to: PlayerMotor (polls HorizontalSpeed, listens to Jumped) and
    /// BoomerangThrower (listens to Thrown). It never moves the player itself.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        // Animator parameter names hashed once; setting by int id avoids a string lookup every frame.
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int ThrowId = Animator.StringToHash("Throw");

        [Tooltip("Animator on the character model (child of the Player).")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private BoomerangThrower thrower;
        [Tooltip("Smoothing of the Speed parameter in seconds.")]
        [SerializeField, Min(0f)] private float speedDampTime = 0.1f;

        // Event-driven: one-shot actions (jump, throw) arrive as events; subscribe/unsubscribe in pairs.
        private void OnEnable()
        {
            if (motor != null) motor.Jumped += OnJumped;
            if (thrower != null) thrower.Thrown += OnThrown;
        }

        private void OnDisable()
        {
            if (motor != null) motor.Jumped -= OnJumped;
            if (thrower != null) thrower.Thrown -= OnThrown;
        }

        private void Update()
        {
            if (animator == null || motor == null) return;
            // Speed (m/s) drives the Idle/Walk/Run blend tree. The dampTime overload eases the value
            // toward the real speed, so the blend does not snap when velocity changes suddenly.
            animator.SetFloat(SpeedId, motor.HorizontalSpeed, speedDampTime, Time.deltaTime);
        }

        // Triggers fire once and reset automatically after the transition consumes them.
        private void OnJumped()
        {
            if (animator != null) animator.SetTrigger(JumpId);
        }

        private void OnThrown(BoomerangProjectile _)
        {
            if (animator != null) animator.SetTrigger(ThrowId);
        }
    }
}
