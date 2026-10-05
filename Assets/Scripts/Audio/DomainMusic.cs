using System.Collections.Generic;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// A synthesized music profile per domain, so every level sounds (and, because the track is built
    /// from the music, plays) differently: tempo, key, groove, bass, arpeggio and texture change.
    /// </summary>
    public struct MusicProfile
    {
        public float Bpm;
        public int Bars;
        public float RootHz;
        public int[] Scale;
        public float Kick;          // 0..1
        public float Tom;           // off-beat tom hits (percussive domains)
        public float Bass;
        public float Hat;
        public bool SixteenthHats;
        public float Arp;
        public float ArpOctave;     // frequency multiplier of the arpeggio
        public bool Bell;           // adds an inharmonic partial: crystal / bell tone
        public float Pad;
        public float Swing;         // 0 = straight, 1 = triplet feel
        public float Echo;          // dotted-eighth delay feedback
        public bool Heartbeat;      // "lub-dub" instead of a four-on-the-floor kick
        public bool Crescendo;      // master gain ramps up across the loop
    }

    public static class DomainMusic
    {
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        private static readonly int[] MinorPent = { 0, 3, 5, 7, 10, 12, 15 };
        private static readonly int[] MajorPent = { 0, 2, 4, 7, 9, 12, 14 };
        private static readonly int[] Dorian = { 0, 2, 3, 5, 7, 9, 10, 12 };
        private static readonly int[] Phrygian = { 0, 1, 3, 5, 7, 8, 10 };

        public static MusicProfile ProfileFor(string domainId)
        {
            switch (domainId)
            {
                case "percusalia":
                    return new MusicProfile { Bpm = 118f, Bars = 8, RootHz = 49f, Scale = MinorPent, Kick = 1f, Tom = 0.8f, Bass = 0.55f, Hat = 0.35f, Arp = 0.1f, ArpOctave = 4f };
                case "echoris":
                    return new MusicProfile { Bpm = 128f, Bars = 8, RootHz = 55f, Scale = Dorian, Kick = 0.9f, Bass = 0.45f, Hat = 0.28f, SixteenthHats = true, Arp = 0.3f, ArpOctave = 8f, Echo = 0.42f };
                case "bassline_abyss":
                    return new MusicProfile { Bpm = 98f, Bars = 8, RootHz = 41.2f, Scale = Phrygian, Kick = 0.7f, Bass = 1f, Hat = 0.1f, Arp = 0.08f, ArpOctave = 2f, Pad = 0.28f };
                case "arpeggion":
                    return new MusicProfile { Bpm = 124f, Bars = 8, RootHz = 65.4f, Scale = MajorPent, Kick = 0.55f, Bass = 0.4f, Hat = 0.18f, Arp = 0.45f, ArpOctave = 8f, Bell = true, Pad = 0.12f };
                case "treble_spire":
                    return new MusicProfile { Bpm = 134f, Bars = 8, RootHz = 73.4f, Scale = MajorPent, Kick = 0.7f, Bass = 0.3f, Hat = 0.3f, SixteenthHats = true, Arp = 0.38f, ArpOctave = 16f, Pad = 0.1f };
                case "noctua_chord":
                    return new MusicProfile { Bpm = 106f, Bars = 8, RootHz = 55f, Scale = Dorian, Kick = 0.35f, Bass = 0.5f, Hat = 0.22f, Arp = 0.3f, ArpOctave = 4f, Pad = 0.2f, Swing = 1f, Echo = 0.3f };
                case "void_crescendo":
                    return new MusicProfile { Bpm = 72f, Bars = 8, RootHz = 36.7f, Scale = Phrygian, Kick = 1f, Bass = 0.8f, Hat = 0.1f, Arp = 0.1f, ArpOctave = 2f, Pad = 0.3f, Heartbeat = true, Crescendo = true };
                default:
                    return new MusicProfile { Bpm = 128f, Bars = 8, RootHz = 55f, Scale = MinorPent, Kick = 1f, Bass = 0.5f, Hat = 0.12f, Arp = 0.18f, ArpOctave = 4f };
            }
        }

        public static AudioClip Create(string domainId)
        {
            if (Cache.TryGetValue(domainId, out AudioClip cached) && cached != null) return cached;
            AudioClip clip = Render(ProfileFor(domainId), domainId);
            Cache[domainId] = clip;
            return clip;
        }

        private static float Noise(float t) => (Mathf.Sin(t * 12989.8f + 78.233f) * 43758.5453f % 1f + 1f) % 1f * 2f - 1f;
        private static float Tone(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static AudioClip Render(MusicProfile p, string name)
        {
            const int sr = MusicGenerator.SampleRate;
            float spb = 60f / p.Bpm;
            int samples = Mathf.CeilToInt(spb * 4f * p.Bars * sr);
            float[] data = new float[samples];
            int[] chordRoots = { 0, 0, 5, 3 };       // semitone offsets per bar

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float beatPos = t / spb;
                int beat = Mathf.FloorToInt(beatPos);
                float phase = beatPos - beat;
                int barIndex = (beat / 4) % chordRoots.Length;
                float rootHz = p.RootHz * Mathf.Pow(2f, chordRoots[barIndex] / 12f);
                float sample = 0f;

                // Kick: four on the floor, or a "lub-dub" heartbeat.
                if (p.Heartbeat)
                {
                    float lub = Tone(Mathf.Lerp(80f, 44f, Mathf.Clamp01(phase * 5f)), t) * Mathf.Exp(-phase * 11f);
                    float dubPhase = Mathf.Max(0f, phase - 0.34f);
                    float dub = phase >= 0.34f ? Tone(Mathf.Lerp(70f, 40f, Mathf.Clamp01(dubPhase * 5f)), t) * Mathf.Exp(-dubPhase * 12f) * 0.7f : 0f;
                    sample += (lub + dub) * p.Kick * 0.9f;
                }
                else if (p.Kick > 0f)
                {
                    float kickEnv = Mathf.Exp(-phase * 12f);
                    sample += Tone(Mathf.Lerp(120f, 45f, Mathf.Clamp01(phase * 6f)), t) * kickEnv * 0.85f * p.Kick;
                }

                // Toms on the off-beat (percussive domains).
                if (p.Tom > 0f && (beat % 4 == 1 || beat % 4 == 3))
                {
                    float tp = Mathf.Max(0f, phase - 0.5f);
                    if (phase >= 0.5f) sample += Tone(Mathf.Lerp(110f, 62f, Mathf.Clamp01(tp * 6f)), t) * Mathf.Exp(-tp * 9f) * 0.45f * p.Tom;
                }

                // Bass line following the chord roots.
                if (p.Bass > 0f)
                {
                    float bassEnv = p.Heartbeat ? 0.8f : Mathf.Exp(-phase * 2.4f) * 0.9f + 0.1f;
                    float saw = ((t * rootHz) % 1f) * 2f - 1f;
                    sample += (Tone(rootHz, t) * 0.8f + saw * 0.12f) * bassEnv * 0.5f * p.Bass;
                }

                // Hats.
                if (p.Hat > 0f)
                {
                    float subdivisions = p.SixteenthHats ? 4f : 2f;
                    float hp = (beatPos * subdivisions) % 1f;
                    bool hit = p.SixteenthHats ? true : (beat % 2 == 1 || phase >= 0.5f);
                    if (hit) sample += Noise(t) * Mathf.Exp(-hp * 28f) * 0.13f * p.Hat * 3f;
                }

                // Pad: sustained triad.
                if (p.Pad > 0f)
                {
                    float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                    float pad = Tone(rootHz * 4f, t) + 0.7f * Tone(rootHz * 4f * 1.189f, t) + 0.6f * Tone(rootHz * 4f * 1.498f, t);
                    sample += pad * 0.1f * p.Pad * trem;
                }

                // Arpeggio on eighth notes (with optional swing).
                if (p.Arp > 0f)
                {
                    int step;
                    float local;
                    if (p.Swing > 0f)
                    {
                        if (phase < 0.667f) { step = beat * 2; local = phase / 0.667f; }
                        else { step = beat * 2 + 1; local = (phase - 0.667f) / 0.333f; }
                    }
                    else
                    {
                        float eighth = beatPos * 2f;
                        step = Mathf.FloorToInt(eighth);
                        local = eighth - step;
                    }

                    int noteIndex = (step * 3 + chordRoots[barIndex]) % p.Scale.Length;
                    float noteHz = rootHz * Mathf.Pow(2f, p.Scale[noteIndex] / 12f) * p.ArpOctave;
                    float env = Mathf.Exp(-local * (p.Bell ? 5f : 8f));
                    float voice = Tone(noteHz, t);
                    if (p.Bell) voice += 0.35f * Tone(noteHz * 2.76f, t) * Mathf.Exp(-local * 12f);
                    sample += voice * env * 0.2f * p.Arp * 2f;
                }

                if (p.Crescendo) sample *= Mathf.Lerp(0.55f, 1f, (float)i / samples);
                data[i] = sample;
            }

            if (p.Echo > 0f)
            {
                int delay = Mathf.RoundToInt(spb * 0.75f * sr);
                for (int i = delay; i < samples; i++) data[i] += data[i - delay] * p.Echo;
                for (int i = 0; i < delay; i++) data[i] += data[samples - delay + i] * p.Echo * 0.5f;   // wrap tail
            }

            float peak = 0.0001f;
            for (int i = 0; i < samples; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = 0.9f / peak;
            for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(data[i] * gain, -1f, 1f);

            AudioClip clip = AudioClip.Create($"NitroDomain_{name}_{Mathf.RoundToInt(p.Bpm)}BPM", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
