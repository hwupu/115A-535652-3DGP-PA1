using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    /// <summary>
    /// An obstacle object. It is never destroyed by boomerangs, which bounce back on contact
    /// (PO decision). As a dynamic Rigidbody, the player can push it (PB-11).
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
            body.WakeUp();
            body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
        }
    }
}
