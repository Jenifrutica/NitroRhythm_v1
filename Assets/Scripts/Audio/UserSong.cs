using System;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// A song chosen by the player (local file). It keeps the decoded mono PCM and its full analysis,
    /// and splits the song into one consecutive section per playable domain: domain 1 races the
    /// opening of the song, the last domain races its ending. Sections are aligned to the beat grid so
    /// the track builder and the villain's on-beat attacks stay in sync with what is heard.
    /// </summary>
    public class UserSong
    {
        /// <summary>Longest song accepted (longer files are truncated to this).</summary>
        public const float MaxSeconds = 480f;

        /// <summary>Shortest section a domain gets; short songs reuse overlapping sections.</summary>
        public const float MinSectionSeconds = 24f;

        /// <summary>Short fade at the section edges so the looping section does not click.</summary>
        private const float EdgeFadeSeconds = 0.03f;

        public string Name { get; }
        public float[] Samples { get; }
        public int SampleRate { get; }
        public AudioAnalysisResult Analysis { get; }

        public float Duration => Samples.Length / (float)SampleRate;

        public UserSong(string name, float[] monoSamples, int sampleRate, AudioAnalysisResult analysis)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Canción" : name;
            Samples = monoSamples ?? new float[0];
            SampleRate = Mathf.Max(1, sampleRate);
            Analysis = analysis ?? AudioAnalysisResult.CreateFlat();
        }

        /// <summary>Builds a song from PCM (interleaved if stereo): mixes to mono, truncates and analyses it.</summary>
        public static UserSong FromPcm(string name, float[] pcm, int channels, int sampleRate)
        {
            float[] mono = ToMono(pcm, channels, sampleRate);
            AudioAnalysisResult analysis = AudioAnalysisService.AnalyzeSamples(mono, 1, sampleRate);
            return new UserSong(CleanName(name), mono, sampleRate, analysis);
        }

        /// <summary>Mixes interleaved PCM down to mono and drops anything past <see cref="MaxSeconds"/>.</summary>
        public static float[] ToMono(float[] pcm, int channels, int sampleRate)
        {
            if (pcm == null) return new float[0];
            channels = Mathf.Max(1, channels);
            long frames = pcm.Length / channels;
            long maxFrames = (long)(MaxSeconds * Mathf.Max(1, sampleRate));
            int count = (int)Math.Min(frames, maxFrames);

            if (channels == 1)
            {
                if (count == pcm.Length) return pcm;
                float[] cut = new float[count];
                Array.Copy(pcm, cut, count);
                return cut;
            }

            float[] mono = new float[count];
            float scale = 1f / channels;
            for (int f = 0; f < count; f++)
            {
                int i = f * channels;
                float sum = 0f;
                for (int c = 0; c < channels; c++) sum += pcm[i + c];
                mono[f] = sum * scale;
            }
            return mono;
        }

        /// <summary>"mi_cancion-final.mp3" → "mi cancion-final".</summary>
        public static string CleanName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "Canción";
            string name = System.IO.Path.GetFileNameWithoutExtension(fileName.Trim());
            name = name.Replace('_', ' ').Trim();
            if (name.Length > 48) name = name.Substring(0, 47) + "…";
            return name.Length == 0 ? "Canción" : name;
        }

        /// <summary>Section of the song (in beats) that domain <paramref name="index"/> (0-based) of <paramref name="count"/> uses.</summary>
        public void GetSection(int index, int count, out int startBeat, out int beatCount)
        {
            count = Mathf.Max(1, count);
            index = Mathf.Clamp(index, 0, count - 1);

            int total = Mathf.Max(1, Analysis.beatCount);
            float spb = Analysis.SecondsPerBeat;
            float duration = Mathf.Max(spb, total * spb);

            // Even split; if that is too short, sections grow and overlap, spread from start to end.
            float length = Mathf.Max(duration / count, Mathf.Min(duration, MinSectionSeconds));
            float start = count > 1 ? index * (duration - length) / (count - 1) : 0f;

            startBeat = Mathf.Clamp(Mathf.FloorToInt(start / spb + 0.001f), 0, total - 1);
            beatCount = Mathf.Clamp(Mathf.RoundToInt(length / spb), 1, total - startBeat);
        }

        /// <summary>Analysis of one domain's section (feeds <see cref="ProceduralTrackBuilder"/>).</summary>
        public AudioAnalysisResult SectionAnalysis(int index, int count)
        {
            GetSection(index, count, out int startBeat, out int beats);
            return Analysis.Slice(startBeat, beats);
        }

        /// <summary>Start/length of a domain's section in seconds.</summary>
        public void GetSectionTime(int index, int count, out float startSeconds, out float lengthSeconds)
        {
            GetSection(index, count, out int startBeat, out int beats);
            startSeconds = startBeat * Analysis.SecondsPerBeat;
            lengthSeconds = beats * Analysis.SecondsPerBeat;
        }

        /// <summary>Raw samples of a domain's section, with short fades so it loops without clicks.</summary>
        public float[] SectionSamples(int index, int count)
        {
            GetSectionTime(index, count, out float startSeconds, out float lengthSeconds);
            int start = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, Mathf.Max(0, Samples.Length - 1));
            int length = Mathf.Clamp(Mathf.RoundToInt(lengthSeconds * SampleRate), 1, Samples.Length - start);

            float[] data = new float[length];
            Array.Copy(Samples, start, data, 0, length);

            int fade = Mathf.Min(length / 2, Mathf.RoundToInt(EdgeFadeSeconds * SampleRate));
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[length - 1 - i] *= k;
            }
            return data;
        }

        /// <summary>Creates the playable clip of a domain's section (caller owns and destroys it).</summary>
        public AudioClip CreateSectionClip(int index, int count)
        {
            float[] data = SectionSamples(index, count);
            AudioClip clip = AudioClip.Create($"UserSong_{index + 1}of{count}", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
