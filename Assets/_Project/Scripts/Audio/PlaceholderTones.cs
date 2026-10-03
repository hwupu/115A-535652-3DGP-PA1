using UnityEngine;

namespace BoomerangGuardian.Audio
{
    /// <summary>
    /// Generates short synthesized sound effects, so every required sound is audible
    /// before real SFX assets are imported (they replace these in Sprint 3).
    /// </summary>
    public static class PlaceholderTones
    {
        public enum Wave
        {
            Sine,
            Square,
        }

        private const int SampleRate = 44100;

        /// <summary>A tone sliding from <paramref name="fromHz"/> to <paramref name="toHz"/> with a short fade in/out.</summary>
        public static AudioClip Sweep(string name, float fromHz, float toHz, float duration, Wave wave = Wave.Sine, float volume = 0.4f)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var samples = new float[sampleCount];
            float phase = 0f;
            const float fadeIn = 0.005f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float frequency = Mathf.Lerp(fromHz, toHz, t);
                phase += 2f * Mathf.PI * frequency / SampleRate;

                float value = Mathf.Sin(phase);
                if (wave == Wave.Square) value = Mathf.Sign(value) * 0.6f;

                float seconds = (float)i / SampleRate;
                float envelope = Mathf.Min(1f, seconds / fadeIn) * (1f - t) * (1f - t);   // quick attack, smooth decay
                samples[i] = value * envelope * volume;
            }

            AudioClip clip = AudioClip.Create($"Placeholder_{name}", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
