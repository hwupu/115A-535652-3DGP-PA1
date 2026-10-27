using UnityEngine;

namespace BoomerangGuardian.Audio
{
    /// <summary>
    /// Generates short synthesized sound effects, so every required sound is audible
    /// before real SFX assets are imported (they replace these in Sprint 3).
    /// Used by AudioManager as the fallback for any empty clip slot (PB-16).
    /// It builds raw PCM samples in code and wraps them in an AudioClip, so no audio file is needed.
    /// </summary>
    public static class PlaceholderTones
    {
        /// <summary>Waveform: Sine sounds soft and pure, Square sounds harsh and buzzy (used for errors and impacts).</summary>
        public enum Wave
        {
            Sine,
            Square,
        }

        private const int SampleRate = 44100;   // samples per second (CD quality)

        /// <summary>A tone sliding from <paramref name="fromHz"/> to <paramref name="toHz"/> with a short fade in/out.</summary>
        public static AudioClip Sweep(string name, float fromHz, float toHz, float duration, Wave wave = Wave.Sine, float volume = 0.4f)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var samples = new float[sampleCount];
            float phase = 0f;
            const float fadeIn = 0.005f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;   // progress through the clip, 0 → 1
                float frequency = Mathf.Lerp(fromHz, toHz, t);
                // Accumulate phase step by step instead of computing sin(2π·f·time): when the
                // frequency changes, a direct formula would jump and click, accumulation stays smooth.
                // One sample advances the phase by 2π·f / sampleRate radians.
                phase += 2f * Mathf.PI * frequency / SampleRate;

                float value = Mathf.Sin(phase);
                // Square wave = sign of the sine (+1 or -1). Scaled to 0.6 because square waves sound louder.
                if (wave == Wave.Square) value = Mathf.Sign(value) * 0.6f;

                float seconds = (float)i / SampleRate;
                // Envelope: a 5 ms linear fade-in avoids a click at the start; (1 - t)² fades out to 0 at the end.
                float envelope = Mathf.Min(1f, seconds / fadeIn) * (1f - t) * (1f - t);   // quick attack, smooth decay
                samples[i] = value * envelope * volume;
            }

            // Mono (1 channel), not streamed (false): all samples are written at once with SetData.
            AudioClip clip = AudioClip.Create($"Placeholder_{name}", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
