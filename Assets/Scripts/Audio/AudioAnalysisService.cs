using System;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Lightweight offline music analyser. Splits the mono mix into a low band
    /// (one-pole low-pass ≈ kick/sub-bass) and a high band (residual), builds an
    /// onset envelope, estimates the BPM by autocorrelation and aggregates the
    /// energy per beat. No FFT dependency so it is fast and WebGL friendly.
    /// </summary>
    public static class AudioAnalysisService
    {
        private const int EnvelopeFps = 50;          // 20 ms frames
        private const float BassCutoffHz = 180f;

        public static AudioAnalysisResult Analyze(AudioClip clip)
        {
            if (clip == null || clip.samples <= 0)
            {
                return AudioAnalysisResult.CreateFlat();
            }

            int channels = Mathf.Max(1, clip.channels);
            int sampleRate = Mathf.Max(1, clip.frequency);
            long totalSamples = (long)clip.samples * channels;

            float[] raw = new float[totalSamples];
            if (!clip.GetData(raw, 0))
            {
                return AudioAnalysisResult.CreateFlat();
            }

            int hop = Mathf.Max(1, sampleRate / EnvelopeFps);
            int frameCount = Mathf.Max(1, raw.Length / (hop * channels));

            float[] env = new float[frameCount];
            float[] bass = new float[frameCount];
            float[] treble = new float[frameCount];

            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * BassCutoffHz / sampleRate);
            float lowPass = 0f;

            for (int frame = 0; frame < frameCount; frame++)
            {
                int start = frame * hop * channels;
                int end = Mathf.Min(start + hop * channels, raw.Length);

                float sumSq = 0f;
                float bassSq = 0f;
                float trebleSq = 0f;
                int count = 0;

                for (int i = start; i < end; i += channels)
                {
                    float sample = raw[i]; // mono mix-down (channel 0)
                    lowPass += (sample - lowPass) * alpha;

                    float low = lowPass;
                    float high = sample - lowPass;

                    sumSq += sample * sample;
                    bassSq += low * low;
                    trebleSq += high * high;
                    count++;
                }

                if (count == 0) count = 1;
                env[frame] = Mathf.Sqrt(sumSq / count);
                bass[frame] = Mathf.Sqrt(bassSq / count);
                treble[frame] = Mathf.Sqrt(trebleSq / count);
            }

            float bpm = EstimateBpm(env, EnvelopeFps);

            AudioAnalysisResult result = new AudioAnalysisResult
            {
                bpm = bpm,
                duration = clip.length
            };

            Aggregate(result, env, bass, treble, bpm, EnvelopeFps);
            return result;
        }

        private static float EstimateBpm(float[] envelope, int fps)
        {
            // Onset strength = positive energy difference.
            float[] onset = new float[envelope.Length];
            for (int i = 1; i < envelope.Length; i++)
            {
                onset[i] = Mathf.Max(0f, envelope[i] - envelope[i - 1]);
            }

            float bestScore = 0f;
            int bestLag = 0;

            // Search 70..180 BPM.
            int minLag = Mathf.RoundToInt(fps * 60f / 180f);
            int maxLag = Mathf.RoundToInt(fps * 60f / 70f);

            for (int lag = minLag; lag <= maxLag && lag < onset.Length; lag++)
            {
                float score = 0f;
                for (int i = lag; i < onset.Length; i++)
                {
                    score += onset[i] * onset[i - lag];
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestLag = lag;
                }
            }

            if (bestLag <= 0 || bestScore <= 0.00001f)
            {
                return 120f;
            }

            float bpm = 60f * fps / bestLag;

            // Fold into the musically plausible 70..180 range.
            while (bpm < 70f) bpm *= 2f;
            while (bpm > 180f) bpm *= 0.5f;
            return bpm;
        }

        private static void Aggregate(
            AudioAnalysisResult result,
            float[] env, float[] bass, float[] treble,
            float bpm, int fps)
        {
            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);
            int framesPerBeat = Mathf.Max(1, Mathf.RoundToInt(secondsPerBeat * fps));
            int beats = Mathf.Max(1, env.Length / framesPerBeat);

            float[] beatEnergy = new float[beats];
            float[] bassEnergy = new float[beats];
            float[] trebleEnergy = new float[beats];

            float total = 0f;

            for (int beat = 0; beat < beats; beat++)
            {
                int start = beat * framesPerBeat;
                int end = Mathf.Min(start + framesPerBeat, env.Length);

                float eSum = 0f, bSum = 0f, tSum = 0f;
                int n = 0;
                for (int i = start; i < end; i++)
                {
                    eSum += env[i];
                    bSum += bass[i];
                    tSum += treble[i];
                    n++;
                }

                if (n == 0) n = 1;
                beatEnergy[beat] = eSum / n;
                bassEnergy[beat] = bSum / n;
                trebleEnergy[beat] = tSum / n;
                total += beatEnergy[beat];
            }

            result.beatCount = beats;
            result.beatEnergy = beatEnergy;
            result.bassEnergy = bassEnergy;
            result.trebleEnergy = trebleEnergy;
            result.averageEnergy = total / beats;
        }
    }
}
