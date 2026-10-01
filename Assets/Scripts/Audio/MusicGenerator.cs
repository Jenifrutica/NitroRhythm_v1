using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Synthesizes a short neon-arcade loop (kick + sub-bass + hats + arpeggio)
    /// so the audio-reactive pipeline is always demonstrable, even without an
    /// external audio file (required on WebGL deployments).
    /// </summary>
    public static class MusicGenerator
    {
        public const int SampleRate = 44100;

        public static AudioClip CreateDemoLoop(float bpm = 128f, int bars = 8)
        {
            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);
            float secondsPerBar = secondsPerBeat * 4f;
            int totalSamples = Mathf.CeilToInt(secondsPerBar * bars * SampleRate);

            float[] data = new float[totalSamples];

            int[] scale = { 0, 3, 5, 7, 10, 12, 15 }; // minor pentatonic steps
            float rootHz = 55f; // A1

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float beatPos = t / secondsPerBeat;
                float beatPhase = beatPos - Mathf.Floor(beatPos);
                int beatIndex = Mathf.FloorToInt(beatPos);
                int beatInBar = beatIndex % 4;

                float sample = 0f;

                // Kick on every beat: pitch-swept sine with fast decay.
                float kickEnv = Mathf.Exp(-beatPhase * 12f);
                float kickFreq = Mathf.Lerp(120f, 45f, Mathf.Clamp01(beatPhase * 6f));
                sample += Mathf.Sin(2f * Mathf.PI * kickFreq * t) * kickEnv * 0.85f;

                // Snappy hat on off-beats.
                float hatPhase = (beatPos * 2f) % 1f;
                if (beatInBar % 2 == 1)
                {
                    float hatEnv = Mathf.Exp(-hatPhase * 30f);
                    sample += (Random.value * 2f - 1f) * hatEnv * 0.12f;
                }

                // Sub-bass on the downbeat of each bar.
                if (beatInBar == 0)
                {
                    float bassEnv = Mathf.Exp(-beatPhase * 3f);
                    sample += Mathf.Sin(2f * Mathf.PI * rootHz * t) * bassEnv * 0.5f;
                }

                // Arpeggio (treble) stepping through the scale.
                int step = beatIndex % scale.Length;
                float noteHz = rootHz * Mathf.Pow(2f, scale[step] / 12f) * 4f;
                float arpEnv = Mathf.Exp(-beatPhase * 9f);
                sample += Mathf.Sin(2f * Mathf.PI * noteHz * t) * arpEnv * 0.18f;

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create($"NitroDemo_{Mathf.RoundToInt(bpm)}BPM", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
