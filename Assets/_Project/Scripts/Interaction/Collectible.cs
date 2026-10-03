using System;
using BoomerangGuardian.Core;
using BoomerangGuardian.Player;
using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    /// <summary>
    /// A collectible item (PB-13). When the player touches its trigger, it disappears
    /// and raises <see cref="AnyCollected"/>; the ScoreManager adds bonus points.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [Tooltip("Visual child that spins and bobs (optional).")]
        [SerializeField, OptionalReference] private Transform visual;
        [Tooltip("Spin speed in degrees per second.")]
        [SerializeField] private float spinSpeed = 90f;
        [Tooltip("Bob amplitude in meters.")]
        [SerializeField, Min(0f)] private float bobHeight = 0.15f;
        [Tooltip("Bob cycles per second.")]
        [SerializeField, Min(0f)] private float bobFrequency = 0.6f;

        public static event Action<Collectible> AnyCollected;

        private Vector3 visualStart;
        private float phase;
        private bool collected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => AnyCollected = null;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (visual != null) visualStart = visual.localPosition;
            phase = UnityEngine.Random.value * Mathf.PI * 2f;   // desynchronize the bobbing
        }

        private void Update()
        {
            if (visual == null) return;
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            float offset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f + phase) * bobHeight;
            visual.localPosition = visualStart + Vector3.up * offset;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.GetComponent<PlayerMotor>() == null) return;

            collected = true;
            AnyCollected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
