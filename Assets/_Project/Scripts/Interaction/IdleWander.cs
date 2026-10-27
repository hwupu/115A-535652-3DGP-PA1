using UnityEngine;

namespace BoomerangGuardian.Interaction
{
    /// <summary>
    /// Makes a visual drift around and rotate on its own (PB-20, e.g. ghosts swirling).
    /// Only the visual child moves: the root, its collider and Rigidbody stay put, so spawning
    /// (no overlap) and ray-cast picking are unaffected. Each instance gets its own random rhythm.
    /// Self-contained: no events or references besides the visual child set in the prefab.
    /// </summary>
    public class IdleWander : MonoBehaviour
    {
        [Tooltip("Visual child to move (e.g. the ghost model).")]
        [SerializeField] private Transform visual;
        [Tooltip("How far the visual drifts from its rest position (m).")]
        [SerializeField, Min(0f)] private float driftRadius = 0.35f;
        [Tooltip("How quickly the drift direction changes.")]
        [SerializeField, Min(0f)] private float driftSpeed = 0.4f;
        [Tooltip("Up/down float amplitude (m).")]
        [SerializeField, Min(0f)] private float bobHeight = 0.12f;
        [Tooltip("Spin speed range in degrees per second; each instance picks one, in a random direction.")]
        [SerializeField] private Vector2 spinSpeedRange = new Vector2(30f, 120f);

        private Vector3 restPosition;
        private Quaternion restRotation;
        private float seedX, seedZ, seedY, spinSpeed, angle;

        private void Awake()
        {
            if (visual == null)
            {
                Debug.LogWarning($"{nameof(IdleWander)} on {name}: 'Visual' is not assigned; nothing will move.", this);
                enabled = false;   // disabling the component stops Update from being called
                return;
            }
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
            // Random offsets into the Perlin noise field: each ghost samples a different path.
            // seedZ is shifted by 50 so the X and Z curves of one ghost differ from each other.
            seedX = Random.value * 100f;
            seedZ = Random.value * 100f + 50f;
            seedY = Random.value * Mathf.PI * 2f;   // phase of the bobbing sine wave
            // Random speed and random sign (clockwise or counter-clockwise).
            spinSpeed = Random.Range(spinSpeedRange.x, spinSpeedRange.y) * (Random.value < 0.5f ? -1f : 1f);
            angle = Random.Range(0f, 360f);
        }

        // Visual-only motion on a child transform, so Update (per frame) is right; no physics involved.
        private void Update()
        {
            float t = Time.time * driftSpeed;
            // Perlin noise in [0,1] → [-1,1]: smooth, random-looking wandering around the rest position.
            float x = (Mathf.PerlinNoise(seedX, t) * 2f - 1f) * driftRadius;
            float z = (Mathf.PerlinNoise(seedZ, t) * 2f - 1f) * driftRadius;
            float y = Mathf.Sin(Time.time * 2f + seedY) * bobHeight;
            visual.localPosition = restPosition + new Vector3(x, y, z);

            // Accumulate the angle (frame-rate independent), then rebuild the rotation from the rest pose:
            // yaw around local up, applied on top of the authored rotation (A * B applies B first).
            angle += spinSpeed * Time.deltaTime;
            visual.localRotation = Quaternion.AngleAxis(angle, Vector3.up) * restRotation;
        }
    }
}
