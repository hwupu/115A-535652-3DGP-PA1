using BoomerangGuardian.Boomerang;
using UnityEngine;

namespace BoomerangGuardian.Player
{
    /// <summary>
    /// Drives the character's Animator from gameplay (PB-28, ADR-0011). Kept deliberately simple:
    /// physics moves the player; the animation only follows. Speed → Idle/Walk/Run blend,
    /// jump → Jump clip, throw → Throw clip. Known, accepted glitches: some foot sliding,
    /// a slightly doubled jump hop, and the throw pose isn't synced to the boomerang release.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int ThrowId = Animator.StringToHash("Throw");

        [Tooltip("Animator on the character model (child of the Player).")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private BoomerangThrower thrower;
        [Tooltip("Smoothing of the Speed parameter in seconds.")]
        [SerializeField, Min(0f)] private float speedDampTime = 0.1f;

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
            animator.SetFloat(SpeedId, motor.HorizontalSpeed, speedDampTime, Time.deltaTime);
        }

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
