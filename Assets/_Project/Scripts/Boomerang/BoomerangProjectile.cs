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
    /// Spawned and launched by <see cref="BoomerangThrower.Throw"/>; it reports hits and the catch back
    /// to that thrower (ReportTargetHit / ReportObstacleHit / ReportCaught), which raises the public events.
    /// Why kinematic: a purely physical boomerang is unstable and can get lost; a scripted path is
    /// deterministic, while the impact on targets/obstacles is still real physics (impulses) (ADR-0005).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoomerangProjectile : MonoBehaviour
    {
        // Small state machine: Outbound → Returning → Done (Done = caught, about to be destroyed).
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
        private float pathT;              // Bézier parameter: 0 = hand, 1 = target
        private Vector3 start;            // P0: launch position (fixed)
        private Vector3 control;          // P1: control point that bends the curve (fixed at launch)
        private Vector3 previousPosition; // last physics-step position, used to derive the flight direction

        /// <summary>
        /// Current direction of travel, derived from the last physics step's movement (a kinematic body
        /// has no useful velocity). Falls back to forward if it hasn't moved yet.
        /// </summary>
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
            // Force the physics setup in code so a mis-configured prefab can't break it:
            // kinematic = moved only by MovePosition, no gravity; Interpolate smooths rendering between
            // FixedUpdate steps; speculative CCD avoids tunnelling through thin colliders at high speed;
            // trigger colliders report contacts (OnTriggerEnter) without physically pushing anything.
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

        /// <summary>
        /// Starts the flight. Called by the thrower right after Instantiate. The start point and the
        /// control point are fixed here; only the end point (the target) is re-read every step.
        /// </summary>
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

        // Update (per rendered frame) only for the cosmetic spin; all movement is in FixedUpdate.
        private void Update()
        {
            // Spin around the boomerang's own vertical axis through its center, independent of the
            // model's import rotation (FBX models often carry e.g. -90° X), so it always spins flat.
            if (spinner != null) spinner.RotateAround(transform.position, transform.up, spinSpeed * Time.deltaTime);
        }

        // Movement runs in FixedUpdate because MovePosition on a Rigidbody is a physics operation:
        // it moves in step with the physics engine, so trigger contacts are detected reliably.
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
                    // Constant speed along the curve: t is not distance, so convert. The control polygon
                    // length (start→control→end) approximates the curve length; advancing t by
                    // (meters this step / length) gives roughly `speed` m/s. MoveTowards clamps t at 1.
                    float approxLength = Vector3.Distance(start, control) + Vector3.Distance(control, end);
                    pathT = Mathf.MoveTowards(pathT, 1f, speed * Time.fixedDeltaTime / Mathf.Max(approxLength, 0.01f));
                    Vector3 next = Bezier(start, control, end, pathT);
                    body.MovePosition(next);

                    // Reached the end of the path without a trigger contact: count it as a hit
                    // (guards against tunnelling: it can't pass through its target without scoring).
                    if (pathT >= 1f) HitTarget(target, next);
                    return;
                }
            }

            // Returning: fly straight to the hand, slightly faster than the throw.
            // The hand position is re-read each step, so it homes in even while the player moves.
            // Catch when within catchRadius, or when this step would overshoot (distance <= step).
            Vector3 hand = owner.ThrowOrigin;
            float step = speed * returnSpeedMultiplier * Time.fixedDeltaTime;
            if (Vector3.Distance(current, hand) <= Mathf.Max(catchRadius, step))
            {
                Catch();
                return;
            }
            body.MovePosition(Vector3.MoveTowards(current, hand, step));
        }

        /// <summary>
        /// Physics callback for our trigger colliders. Objects are identified by component
        /// (Target / Obstacle) rather than tags; GetComponentInParent finds the component on the
        /// prefab root even when the collider sits on a child.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (state != State.Outbound) return;   // ignore everything on the way back

            // Never collide with the player who threw it (it spawns at the hand, right next to the player's collider).
            Rigidbody otherBody = other.attachedRigidbody;
            if (otherBody != null && otherBody.GetComponent<PlayerMotor>() != null) return;

            // Any un-hit target touched on the way counts, not only the selected one (ADR-0005 amendment).
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

        /// <summary>Pushes the target away and turns back. Shared by the trigger hit and the end-of-path hit.</summary>
        private void HitTarget(Target hitTarget, Vector3 point)
        {
            // Push direction: the horizontal flight direction plus a bit of "up" (targetLift),
            // re-normalized so the impulse magnitude is exactly targetImpulse.
            Vector3 direction = FlightDirection;
            direction.y = 0f;
            direction = (direction.normalized + Vector3.up * targetLift).normalized;

            // Hit() returns false if the target was already hit, so the event is raised only once.
            if (hitTarget.Hit(direction * targetImpulse, point)) owner.ReportTargetHit(hitTarget);
            state = State.Returning;
        }

        /// <summary>Ends the flight: tells the thrower (catch sound) and removes the projectile.</summary>
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
            // Cross(up, forward) gives the horizontal vector perpendicular to the throw, pointing right.
            Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized);
            // Midpoint + sideways offset (scales with distance, so long throws bend more) + fixed lift.
            return (start + end) * 0.5f + right * (toTarget.magnitude * curve) + Vector3.up * arcHeight;
        }

        /// <summary>Path shape values, read from the prefab by the hover preview.</summary>
        public float Curve => curve;
        public float ArcHeight => arcHeight;

        /// <summary>
        /// Quadratic Bézier: B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2, for t in [0, 1].
        /// Starts at P0, ends at P2, and is pulled toward (but does not pass through) the control point P1.
        /// </summary>
        public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
    }
}
