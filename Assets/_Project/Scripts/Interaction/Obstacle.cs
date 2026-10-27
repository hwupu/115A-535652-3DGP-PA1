using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    /// <summary>
    /// An obstacle object. It is never destroyed by boomerangs, which bounce back on contact
    /// (PO decision). As a dynamic Rigidbody, the player can push it (PB-11).
    /// Mostly a marker component: BoomerangProjectile and <see cref="TargetSelector"/> recognise
    /// obstacles by this component (not by tag); the projectile calls <see cref="Bump"/> on contact.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Obstacle : MonoBehaviour
    {
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        /// <summary>Small physical reaction when a boomerang bounces off.</summary>
        public void Bump(Vector3 impulse, Vector3 point)
        {
            body.WakeUp();   // a sleeping Rigidbody would ignore the force
            // One-off impulse at the contact point: a push plus a little torque, scaled by the obstacle's mass.
            body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
        }
    }
}
