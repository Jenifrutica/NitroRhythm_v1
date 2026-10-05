using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// A looping ambience per domain, synthesized in code: wind, city hum, abyss drone, crystal shimmer,
    /// night pings or a heart-like void rumble. The loop end is cross-faded into its start.
    /// </summary>
    public static class AmbientBed
    {
        private const int SampleRate = 44100;
        private const float Seconds = 8f;

        private static readonly System.Collections.Generic.Dictionary<string, AudioClip> Cache = new System.Collections.Generic.Dictionary<string, AudioClip>();

        private static float Noise(int i, float seed)
        {
            float x = Mathf.Sin(i * 12.9898f + seed * 78.233f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        public static AudioClip Create(string domainId)
        {
            if (Cache.TryGetValue(domainId, out AudioClip cached) && cached != null) return cached;

            int n = Mathf.CeilToInt(Seconds * SampleRate);
            float[] d = new float[n];
            float lp = 0f, lp2 = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float white = Noise(i, 1f);
                float s = 0f;

                switch (domainId)
                {
                    case "percusalia":      // desert wind + distant drum rumble
                        lp += (white - lp) * 0.012f;
                        s = lp * (2.4f + Mathf.Sin(2f * Mathf.PI * t / 8f) * 1.1f) + Mathf.Sin(2f * Mathf.PI * 49f * t) * 0.05f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 4f));
                        break;
                    case "echoris":         // city hum: mains buzz + airy noise
                        lp += (white - lp) * 0.05f;
                        s = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.06f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.03f + lp * 0.35f;
                        break;
                    case "bassline_abyss":  // abyss drone
                        s = Mathf.Sin(2f * Mathf.PI * 41.2f * t) * 0.18f + Mathf.Sin(2f * Mathf.PI * 61.8f * t) * 0.09f * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / 4f));
                        break;
                    case "arpeggion":       // crystal shimmer
                        lp += (white - lp) * 0.2f;
                        lp2 += (lp - lp2) * 0.2f;
                        float ping = Mathf.Sin(2f * Mathf.PI * 1568f * t) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * t / 2f)), 12f) * 0.05f;
                        s = (lp - lp2) * 0.9f + ping;
                        break;
                    case "treble_spire":    // high wind
                        lp += (white - lp) * 0.03f;
                        s = lp * (1.6f + Mathf.Sin(2f * Mathf.PI * t / 8f + 1f) * 0.9f);
                        break;
                    case "noctua_chord":    // night: soft pad and sparse pings
                        s = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.05f
                            + Mathf.Sin(2f * Mathf.PI * 2093f * t) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * t / 4f)), 24f) * 0.05f;
                        break;
                    case "void_crescendo":  // rumble
                        lp += (white - lp) * 0.006f;
                        s = lp * 3.4f + Mathf.Sin(2f * Mathf.PI * 30f * t) * 0.12f;
                        break;
                    default:
                        lp += (white - lp) * 0.02f;
                        s = lp * 1.5f;
                        break;
                }

                d[i] = s;
            }

            // Cross-fade the last 0.5 s into the first 0.5 s so the loop point is inaudible.
            int fade = SampleRate / 2;
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                d[i] = Mathf.Lerp(d[n - fade + i], d[i], k);
            }

            float peak = 0.0001f;
            for (int i = 0; i < n - fade; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            float gain = 0.7f / peak;
            float[] result = new float[n - fade];
            for (int i = 0; i < result.Length; i++) result[i] = Mathf.Clamp(d[i] * gain, -1f, 1f);

            AudioClip clip = AudioClip.Create($"Ambient_{domainId}", result.Length, 1, SampleRate, false);
            clip.SetData(result, 0);
            Cache[domainId] = clip;
            return clip;
        }
    }
}
