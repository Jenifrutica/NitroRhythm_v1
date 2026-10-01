using System;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Result of analysing a music clip: tempo plus per-beat energy of the
    /// low (kick/sub-bass) and high (hat/synth) bands. Serializable so it can
    /// be cached and reused between the menu and the gameplay scene.
    /// </summary>
    [Serializable]
    public class AudioAnalysisResult
    {
        public float bpm = 120f;
        public int beatCount;
        public float duration;
        public float averageEnergy;
        public float[] beatEnergy = new float[0];
        public float[] bassEnergy = new float[0];
        public float[] trebleEnergy = new float[0];

        public float SecondsPerBeat => 60f / Mathf.Max(1f, bpm);
        public bool IsValid => beatCount > 0 && bassEnergy != null && bassEnergy.Length > 0;

        public float GetBass(int beat)
        {
            if (!IsValid) return 0f;
            return bassEnergy[((beat % bassEnergy.Length) + bassEnergy.Length) % bassEnergy.Length];
        }

        public float GetTreble(int beat)
        {
            if (!IsValid) return 0f;
            return trebleEnergy[((beat % trebleEnergy.Length) + trebleEnergy.Length) % trebleEnergy.Length];
        }

        /// <summary>Normalizes a raw band value to 0..1 against the clip average.</summary>
        public float Normalized(float value)
        {
            if (averageEnergy <= 0.0001f) return 0f;
            return Mathf.Clamp01(value / (averageEnergy * 2.5f));
        }

        public static AudioAnalysisResult CreateFlat(float bpm = 120f, int beats = 64, float energy = 0.3f)
        {
            AudioAnalysisResult result = new AudioAnalysisResult
            {
                bpm = bpm,
                beatCount = beats,
                duration = beats * (60f / Mathf.Max(1f, bpm)),
                averageEnergy = energy,
                beatEnergy = new float[beats],
                bassEnergy = new float[beats],
                trebleEnergy = new float[beats]
            };

            for (int i = 0; i < beats; i++)
            {
                bool strong = i % 4 == 0;
                result.beatEnergy[i] = strong ? energy * 1.4f : energy;
                result.bassEnergy[i] = strong ? energy * 2.4f : energy * 0.5f;
                result.trebleEnergy[i] = i % 2 == 0 ? energy * 2.4f : energy * 0.5f;
            }

            return result;
        }
    }
}
