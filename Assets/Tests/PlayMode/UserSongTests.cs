using System.Collections;
using System.IO;
using NUnit.Framework;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>Player songs: loading, analysis and one section of the song per domain.</summary>
    public class UserSongTests
    {
        /// <summary>Earlier gameplay tests can leave the game paused (intro card, cutscene).</summary>
        [SetUp]
        public void Unpause() => Time.timeScale = 1f;

        private const int Rate = 22050;

        /// <summary>Kick (low thump) on every beat and a hat on every off-beat, like a simple dance track.</summary>
        private static float[] ClickTrack(float bpm, float seconds, int channels = 1)
        {
            int frames = Mathf.RoundToInt(seconds * Rate);
            float[] data = new float[frames * channels];
            float spb = 60f / bpm;
            for (int f = 0; f < frames; f++)
            {
                float t = f / (float)Rate;
                float beatPhase = t % spb;
                float halfPhase = (t + spb * 0.5f) % spb;
                float kick = beatPhase < 0.12f ? Mathf.Sin(2f * Mathf.PI * 55f * beatPhase) * Mathf.Exp(-beatPhase * 30f) : 0f;
                float hat = halfPhase < 0.03f ? (Mathf.PerlinNoise(t * 9000f, 0.3f) * 2f - 1f) * Mathf.Exp(-halfPhase * 120f) * 0.4f : 0f;
                float s = kick * 0.9f + hat;
                for (int c = 0; c < channels; c++) data[f * channels + c] = s;
            }
            return data;
        }

        private static UserSong MakeSong(float seconds, float bpm = 120f)
        {
            return UserSong.FromPcm("prueba_cancion.mp3", ClickTrack(bpm, seconds), 1, Rate);
        }

        [Test]
        public void Analysis_FindsTheTempoOfRawSamples()
        {
            AudioAnalysisResult result = AudioAnalysisService.AnalyzeSamples(ClickTrack(120f, 20f), 1, Rate);
            Assert.That(result.bpm, Is.InRange(115f, 125f), $"BPM detectado {result.bpm:F1}");
            Assert.That(result.duration, Is.EqualTo(20f).Within(0.1f));
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ToMono_AveragesChannelsAndCapsTheLength()
        {
            float[] stereo = { 1f, 0f, 0.5f, 0.5f, -1f, 1f };
            float[] mono = UserSong.ToMono(stereo, 2, Rate);
            CollectionAssert.AreEqual(new[] { 0.5f, 0.5f, 0f }, mono);

            int tooLong = Mathf.RoundToInt((UserSong.MaxSeconds + 5f) * 100);
            Assert.AreEqual(Mathf.RoundToInt(UserSong.MaxSeconds * 100), UserSong.ToMono(new float[tooLong], 1, 100).Length);
        }

        [Test]
        public void CleanName_DropsExtensionAndUnderscores()
        {
            Assert.AreEqual("mi cancion favorita", UserSong.CleanName("/home/x/mi_cancion_favorita.mp3"));
            Assert.AreEqual("Canción", UserSong.CleanName(""));
        }

        [Test]
        public void Sections_SplitALongSongInOrderFromStartToEnd()
        {
            UserSong song = MakeSong(210f);
            int domains = 7;
            float previousStart = -1f;
            for (int i = 0; i < domains; i++)
            {
                song.GetSectionTime(i, domains, out float start, out float length);
                Assert.Greater(start, previousStart, $"El tramo {i + 1} debe empezar después del anterior.");
                Assert.That(length, Is.EqualTo(30f).Within(1.5f), $"Tramo {i + 1}: 210 s / 7 ≈ 30 s");
                previousStart = start;
            }

            song.GetSectionTime(0, domains, out float firstStart, out _);
            song.GetSectionTime(domains - 1, domains, out float lastStart, out float lastLength);
            Assert.That(firstStart, Is.EqualTo(0f).Within(0.01f));
            Assert.That(lastStart + lastLength, Is.EqualTo(210f).Within(1.5f), "El último dominio llega al final de la canción.");
        }

        [Test]
        public void Sections_OfAShortSongOverlapButKeepAPlayableLength()
        {
            UserSong song = MakeSong(60f);
            for (int i = 0; i < 7; i++)
            {
                song.GetSectionTime(i, 7, out float start, out float length);
                Assert.That(length, Is.GreaterThanOrEqualTo(UserSong.MinSectionSeconds - 1f));
                Assert.That(start + length, Is.LessThanOrEqualTo(60.5f));
            }
        }

        [Test]
        public void SectionAnalysis_KeepsTempoAndBuildsATrack()
        {
            UserSong song = MakeSong(140f);
            song.GetSection(3, 7, out int startBeat, out int beats);
            AudioAnalysisResult section = song.SectionAnalysis(3, 7);

            Assert.AreEqual(beats, section.beatCount);
            Assert.AreEqual(song.Analysis.bpm, section.bpm);
            Assert.AreEqual(song.Analysis.bassEnergy[startBeat], section.bassEnergy[0]);

            ProceduralTrackPlan plan = ProceduralTrackBuilder.Build(section, PrototypeData.Instance.PlayableLevels[3], 0f);
            Assert.Greater(plan.segments.Count, 8, "Cada tramo debe dar una pista jugable.");
        }

        [Test]
        public void SectionClip_HasTheSectionLengthAndFadedEdges()
        {
            UserSong song = MakeSong(90f);
            song.GetSectionTime(2, 7, out _, out float length);
            AudioClip clip = song.CreateSectionClip(2, 7);
            try
            {
                Assert.AreEqual(1, clip.channels);
                Assert.AreEqual(Rate, clip.frequency);
                Assert.That(clip.length, Is.EqualTo(length).Within(0.05f));
                float[] first = new float[1];
                clip.GetData(first, 0);
                Assert.That(first[0], Is.EqualTo(0f).Within(0.0001f), "El borde lleva fundido para que el bucle no haga clic.");
            }
            finally
            {
                Object.Destroy(clip);
            }
        }

        [UnityTest]
        public IEnumerator MusicController_PlaysTheDomainSectionOfTheUserSong()
        {
            GameSession session = GameSession.EnsureExists();
            UserSong song = MakeSong(150f);
            session.UserSong = song;
            session.CurrentLevelIndex = 3;
            int domains = PrototypeData.Instance.PlayableCount;

            // A gameplay scene left by an earlier test keeps its own (singleton) music controller.
            if (AudioReactiveMusicController.Instance != null)
            {
                Object.Destroy(AudioReactiveMusicController.Instance.gameObject);
                yield return null;
            }

            GameObject go = new GameObject("MusicUnderTest");
            AudioReactiveMusicController music = go.AddComponent<AudioReactiveMusicController>();
            try
            {
                float timeout = 5f;
                while (!music.IsReady && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(music.IsReady);

                song.GetSection(2, domains, out _, out int beats);
                Assert.AreEqual(beats, music.Analysis.beatCount, "El dominio 3 usa su propio tramo de la canción.");
                StringAssert.Contains(song.Name, music.SectionLabel);
                song.GetSectionTime(2, domains, out _, out float length);
                Assert.That(music.Source.clip.length, Is.EqualTo(length).Within(0.05f));
            }
            finally
            {
                Object.Destroy(go);
                session.UserSong = null;
                session.CurrentLevelIndex = 1;
            }
        }

        [UnityTest]
        public IEnumerator Loader_ReadsALocalWavFile()
        {
            string path = Path.Combine(Application.temporaryCachePath, "nitro_test_song.wav");
            WriteWav(path, ClickTrack(128f, 18f, 2), 2, Rate);

            SongLoader loader = SongLoader.Instance;
            loader.LoadFile(path);
            float timeout = 15f;
            while ((loader.Busy || loader.State == SongLoader.LoadState.Idle) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            try
            {
                Assert.AreEqual(SongLoader.LoadState.Done, loader.State, loader.Error);
                Assert.AreEqual("nitro test song", loader.Result.Name);
                Assert.That(loader.Result.Duration, Is.EqualTo(18f).Within(0.1f));
                Assert.That(loader.Result.Analysis.bpm, Is.InRange(120f, 136f));
            }
            finally
            {
                loader.Cancel();
                File.Delete(path);
            }
        }

        /// <summary>MP3 is what most players have: check the desktop decoder handles it (Docs/Tools/test_data).</summary>
        [UnityTest]
        public IEnumerator Loader_ReadsAnMp3File()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Tools", "test_data", "cancion_prueba_128bpm.mp3"));
            if (!File.Exists(path)) Assert.Ignore("Falta el MP3 de prueba: " + path);

            SongLoader loader = SongLoader.Instance;
            loader.LoadFile(path);
            float timeout = 20f;
            while ((loader.Busy || loader.State == SongLoader.LoadState.Idle) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            try
            {
                Assert.AreEqual(SongLoader.LoadState.Done, loader.State, loader.Error);
                Assert.That(loader.Result.Duration, Is.EqualTo(20f).Within(0.6f), "El MP3 añade unas décimas de relleno.");
                Assert.That(loader.Result.Analysis.bpm, Is.InRange(122f, 134f), $"BPM {loader.Result.Analysis.bpm:F1}");
            }
            finally
            {
                loader.Cancel();
            }
        }

        [UnityTest]
        public IEnumerator Loader_ReportsAMissingFile()
        {
            SongLoader loader = SongLoader.Instance;
            loader.LoadFile("/no/existe/cancion.mp3");
            yield return null;
            Assert.AreEqual(SongLoader.LoadState.Failed, loader.State);
            StringAssert.Contains("No existe", loader.Error);
            loader.Cancel();
        }

        private static void WriteWav(string path, float[] pcm, int channels, int rate)
        {
            using (BinaryWriter w = new BinaryWriter(File.Create(path)))
            {
                int dataBytes = pcm.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)channels);
                w.Write(rate);
                w.Write(rate * channels * 2);
                w.Write((short)(channels * 2));
                w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                foreach (float s in pcm) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
        }
    }
}
