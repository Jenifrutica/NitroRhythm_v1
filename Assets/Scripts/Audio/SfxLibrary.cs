using System;
using System.Collections.Generic;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Procedural sound effects (no audio files needed, so they also work on WebGL).
    /// Same philosophy as <see cref="MusicGenerator"/>: everything is synthesized once
    /// and cached.
    /// </summary>
    public static class SfxLibrary
    {
        public const int SampleRate = 44100;

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Impact => Get("sfx_impact", 0.5f, Synth_Impact);
        public static AudioClip Jump => Get("sfx_jump", 0.2f, Synth_Jump);
        public static AudioClip Boost => Get("sfx_boost", 0.45f, Synth_Boost);
        public static AudioClip Checkpoint => Get("sfx_checkpoint", 0.55f, Synth_Checkpoint);
        public static AudioClip Fire => Get("sfx_fire", 0.25f, Synth_Fire);
        public static AudioClip Goal => Get("sfx_goal", 0.8f, Synth_Goal);
        public static AudioClip Pickup => Get("sfx_pickup", 0.35f, Synth_Pickup);
        public static AudioClip UiHover => Get("sfx_ui_hover", 0.06f, Synth_Hover);

        /// <summary>Seamless 1 s engine hum meant to be looped with a speed-driven pitch.</summary>
        public static AudioClip EngineLoop => Get("sfx_engine", 1f, Synth_Engine);

        private static AudioClip Get(string name, float seconds, Func<float, float, float> synth)
        {
            if (Cache.TryGetValue(name, out AudioClip cached) && cached != null) return cached;

            int samples = Mathf.CeilToInt(seconds * SampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                data[i] = Mathf.Clamp(synth(t, t / seconds), -1f, 1f);
            }

            // Short fade-out so one-shots never click at the end.
            int fade = Mathf.Min(samples, 200);
            if (name != "sfx_engine")
            {
                for (int i = 0; i < fade; i++) data[samples - 1 - i] *= (float)i / fade;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            Cache[name] = clip;
            return clip;
        }

        private static float Noise(float t)
        {
            // Deterministic hash noise: stable across runs and cheap.
            float x = Mathf.Sin(t * 12989.8f + 78.233f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        private static float Tone(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static float Synth_Impact(float t, float u)
        {
            float thud = Tone(Mathf.Lerp(120f, 38f, Mathf.Clamp01(t * 8f)), t) * Mathf.Exp(-t * 9f) * 0.9f;
            float crack = Noise(t) * Mathf.Exp(-t * 28f) * 0.55f;
            float alarm = Tone(Mathf.Lerp(520f, 240f, u), t) * Mathf.Exp(-t * 7f) * 0.25f;
            return thud + crack + alarm;
        }

        private static float Synth_Jump(float t, float u)
        {
            float hz = Mathf.Lerp(260f, 640f, u);
            return (Tone(hz, t) + 0.3f * Tone(hz * 2f, t)) * Mathf.Pow(1f - u, 1.4f) * 0.4f;
        }

        private static float Synth_Boost(float t, float u)
        {
            float env = Mathf.Sin(u * Mathf.PI);
            float hz = Mathf.Lerp(180f, 980f, u * u);
            return (Tone(hz, t) * 0.35f + Noise(t) * 0.18f * u) * env;
        }

        private static float Synth_Checkpoint(float t, float u)
        {
            float a = t < 0.12f ? 1f : 0f;
            float first = (Tone(660f, t) + 0.3f * Tone(1320f, t)) * Mathf.Exp(-t * 14f) * a;
            float tt = Mathf.Max(0f, t - 0.12f);
            float second = t >= 0.12f ? (Tone(990f, t) + 0.3f * Tone(1980f, t)) * Mathf.Exp(-tt * 6f) : 0f;
            return (first + second) * 0.35f;
        }

        private static float Synth_Fire(float t, float u)
        {
            float hz = Mathf.Lerp(900f, 180f, u);
            float square = Mathf.Sign(Tone(hz, t)) * 0.18f;
            return (square + Noise(t) * 0.05f) * Mathf.Pow(1f - u, 1.2f);
        }

        private static float Synth_Goal(float t, float u)
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float result = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * 0.12f;
                if (t < start) continue;
                float local = t - start;
                result += (Tone(notes[i], t) + 0.25f * Tone(notes[i] * 2f, t)) * Mathf.Exp(-local * 5f) * 0.3f;
            }
            return result;
        }

        private static float Synth_Pickup(float t, float u)
        {
            // Quick rising two-note sparkle.
            float first = Tone(880f, t) * Mathf.Exp(-t * 16f);
            float tt = Mathf.Max(0f, t - 0.07f);
            float second = t >= 0.07f ? Tone(1320f, t) * Mathf.Exp(-tt * 9f) : 0f;
            return (first + second + 0.2f * Tone(2640f, t) * Mathf.Exp(-t * 20f)) * 0.35f;
        }

        private static float Synth_Hover(float t, float u)
        {
            return Tone(1100f, t) * Mathf.Pow(1f - u, 2f) * 0.25f;
        }

        private static float Synth_Engine(float t, float u)
        {
            // Integer number of cycles per second => seamless loop.
            return (Tone(55f, t) * 0.5f + Tone(110f, t) * 0.28f + Tone(165f, t) * 0.14f + Tone(220f, t) * 0.06f) * 0.7f;
        }
    }
}
