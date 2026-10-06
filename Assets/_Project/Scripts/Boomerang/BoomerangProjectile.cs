using BoomerangGuardian.Interaction;
using BoomerangGuardian.Player;
using UnityEngine;

namespace BoomerangGuardian.Boomerang
{
    /// <summary>
    /// A boomerang in flight (ADR-0005). It is a kinematic Rigidbody with a trigger collider,
    /// moved along a scripted path so it is deterministic and never gets lost.
    /// Outbound: a curved quadratic Bézier path to the selected target (it follows the target if it moves).
    /// On hit: pushes the target (impulse), or bounces off an obstacle, then returns.
    /// Returning: homes in on the thrower's hand, ignores collisions, and is caught within a radius.
    /// Safety: after maxFlightTime it counts as caught.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoomerangProjectile : MonoBehaviour
    {
        private enum State
        {
            Outbound,
            Returning,
            Done,
        }

        [Header("Visual")]
        [Tooltip("Visual child that spins around the boomerang's vertical axis. Any import rotation is fine.")]
        [SerializeField] private Transform spinner;
        [Tooltip("Spin speed in degrees per second.")]
        [SerializeField] private float spinSpeed = 1080f;

        [Header("Path")]
        [Tooltip("Sideways bend of the outbound path, as a fraction of the throw distance.")]
        [SerializeField, Range(0f, 1f)] private float curve = 0.3f;
        [Tooltip("Extra height at the middle of the outbound path (m).")]
        [SerializeField, Min(0f)] private float arcHeight = 0.5f;
        [Tooltip("Return speed relative to the throw speed.")]
        [SerializeField, Min(0.1f)] private float returnSpeedMultiplier = 1.2f;
        [Tooltip("Distance from the hand at which the boomerang is caught (m).")]
        [SerializeField, Min(0.05f)] private float catchRadius = 0.8f;
        [Tooltip("Safety limit: after this many seconds the boomerang counts as caught.")]
        [SerializeField, Min(1f)] private float maxFlightTime = 8f;

        [Header("Impact")]
        [Tooltip("Impulse applied to a target on hit (N·s).")]
        [SerializeField, Min(0f)] private float targetImpulse = 10f;
        [Tooltip("Upward share of the hit direction, so targets lift a little.")]
        [SerializeField, Range(0f, 1f)] private float targetLift = 0.3f;
        [Tooltip("Impulse applied to an obstacle when bouncing off (N·s).")]
        [SerializeField, Min(0f)] private float obstacleImpulse = 4f;

        private Rigidbody body;
        private BoomerangThrower owner;
        private Target target;
        private State state = State.Done;
        private float speed;
        private float launchTime;
        private float pathT;
        private Vector3 start;
        private Vector3 control;
        private Vector3 previousPosition;

        private Vector3 FlightDirection
        {
            get
            {
                Vector3 delta = body.position - previousPosition;
                return delta.sqrMagnitude > 1e-6f ? delta.normalized : transform.forward;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.isTrigger = true;

            if (spinner == null)
            {
                // Fail loudly (ADR-0009): e.g. the link is lost when a model swap replaces the Spinner object.
                Renderer visual = GetComponentInChildren<Renderer>();
                spinner = visual != null && visual.transform != transform ? visual.transform : null;
                Debug.LogWarning($"{nameof(BoomerangProjectile)}: 'Spinner' is not assigned on the {name} prefab; " +
                                 (spinner != null ? $"spinning '{spinner.name}' instead. " : "nothing will spin. ") +
                                 "Assign it in the prefab (Boomerang Guardian → Validate Wiring lists such gaps).", this);
            }
        }

        public void Launch(BoomerangThrower thrower, Target selectedTarget, float throwSpeed)
        {
            owner = thrower;
            target = selectedTarget;
            speed = throwSpeed;
            launchTime = Time.time;
            pathT = 0f;
            start = previousPosition = transform.position;

            control = ControlPoint(start, target.AimPoint, curve, arcHeight);

            state = State.Outbound;
        }

        private void Update()
        {
            // Spin around the boomerang's own vertical axis through its center, independent of the
            // model's import rotation (FBX models often carry e.g. -90° X), so it always spins flat.
            if (spinner != null) spinner.RotateAround(transform.position, transform.up, spinSpeed * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (state == State.Done) return;
            if (owner == null) { Destroy(gameObject); return; }
            if (Time.time - launchTime > maxFlightTime) { Catch(); return; }   // stability fallback

            Vector3 current = body.position;
            previousPosition = current;

            if (state == State.Outbound)
            {
                // The target can disappear (platform) or be hit by another boomerang meanwhile.
                if (target == null || !target.isActiveAndEnabled || target.IsHit)
                {
                    state = State.Returning;
                }
                else
                {
                    Vector3 end = target.AimPoint;   // follow the target if it moves
                    float approxLength = Vector3.Distance(start, control) + Vector3.Distance(control, end);
                    pathT = Mathf.MoveTowards(pathT, 1f, speed * Time.fixedDeltaTime / Mathf.Max(approxLength, 0.01f));
                    Vector3 next = Bezier(start, control, end, pathT);
                    body.MovePosition(next);

                    // Reached the end of the path without a trigger contact: count it as a hit.
                    if (pathT >= 1f) HitTarget(target, next);
                    return;
                }
            }

            // Returning: fly straight to the hand, slightly faster than the throw.
            Vector3 hand = owner.ThrowOrigin;
            float step = speed * returnSpeedMultiplier * Time.fixedDeltaTime;
            if (Vector3.Distance(current, hand) <= Mathf.Max(catchRadius, step))
            {
                Catch();
                return;
            }
            body.MovePosition(Vector3.MoveTowards(current, hand, step));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (state != State.Outbound) return;   // ignore everything on the way back

            Rigidbody otherBody = other.attachedRigidbody;
            if (otherBody != null && otherBody.GetComponent<PlayerMotor>() != null) return;

            Target hitTarget = other.GetComponentInParent<Target>();
            if (hitTarget != null)
            {
                if (!hitTarget.IsHit) HitTarget(hitTarget, other.ClosestPoint(body.position));
                return;
            }

            Obstacle obstacle = other.GetComponentInParent<Obstacle>();
            if (obstacle != null)
            {
                obstacle.Bump(FlightDirection * obstacleImpulse, other.ClosestPoint(body.position));
                owner.ReportObstacleHit(obstacle);
                state = State.Returning;   // obstacles can't be destroyed: bounce back
            }
        }

        private void HitTarget(Target hitTarget, Vector3 point)
        {
            Vector3 direction = FlightDirection;
            direction.y = 0f;
            direction = (direction.normalized + Vector3.up * targetLift).normalized;

            if (hitTarget.Hit(direction * targetImpulse, point)) owner.ReportTargetHit(hitTarget);
            state = State.Returning;
        }

        private void Catch()
        {
            state = State.Done;
            if (owner != null) owner.ReportCaught();
            Destroy(gameObject);
        }

        /// <summary>
        /// Control point of the outbound curve: halfway, pushed to the right and up, which gives the
        /// classic curved throw. Shared with the hover preview (PB-29), so the preview matches the flight.
        /// </summary>
        public static Vector3 ControlPoint(Vector3 start, Vector3 end, float curve, float arcHeight)
        {
            Vector3 toTarget = end - start;
            Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized);
            return (start + end) * 0.5f + right * (toTarget.magnitude * curve) + Vector3.up * arcHeight;
        }

        public float Curve => curve;
        public float ArcHeight => arcHeight;

        public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
    }
}
