using System.Collections;
using NUnit.Framework;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Data;
using NitroRhythm.UI;
using NitroRhythm.World;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>Round 2, phases 2-5: themed worlds, per-domain audio, fonts and the new interface.</summary>
    public class Phase2Tests
    {
        private static string[] PlayableIds()
        {
            LevelDefinition[] playable = PrototypeData.Instance.PlayableLevels;
            string[] ids = new string[playable.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = playable[i].id;
            return ids;
        }

        [Test]
        public void EveryPlayableDomain_HasAThemeWithPropsAndDistinctLook()
        {
            System.Collections.Generic.HashSet<string> looks = new System.Collections.Generic.HashSet<string>();
            foreach (string id in PlayableIds())
            {
                DomainTheme theme = DomainTheme.Get(id);
                Assert.AreEqual(id, theme.id, $"{id}: falta el tema.");
                Assert.Greater(theme.props.Length, 2, $"{id}: debe tener al menos 3 tipos de props.");
                Assert.IsNotEmpty(theme.platformPattern);
                looks.Add($"{theme.platformPattern}|{theme.skyTint}|{theme.fogColor}");
            }
            Assert.AreEqual(PlayableIds().Length, looks.Count, "Cada dominio debe verse distinto.");
        }

        [Test]
        public void EveryDomain_HasItsOwnMusicThatTheAnalyserUnderstands()
        {
            foreach (string id in PlayableIds())
            {
                MusicProfile profile = DomainMusic.ProfileFor(id);
                AudioClip clip = DomainMusic.Create(id);
                Assert.IsNotNull(clip, id);
                AudioAnalysisResult analysis = AudioAnalysisService.Analyze(clip);
                Assert.IsTrue(analysis.IsValid, $"{id}: el análisis debe ser válido.");
                Assert.Greater(analysis.beatCount, 16, $"{id}: debe haber pulsos suficientes.");
                Assert.Greater(analysis.bpm, 55f, id);
                Assert.Less(analysis.bpm, 200f, id);
                Debug.Log($"[MusicTest] {id}: perfil {profile.Bpm} BPM, detectado {analysis.bpm:F1}, pulsos {analysis.beatCount}");
            }
        }

        [Test]
        public void EveryDomain_HasAnAudibleAmbience()
        {
            foreach (string id in PlayableIds())
            {
                AudioClip clip = AmbientBed.Create(id);
                float[] data = new float[clip.samples];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.Greater(peak, 0.3f, $"{id}: ambiente demasiado bajo.");
                Assert.LessOrEqual(Mathf.Abs(data[0] - data[data.Length - 1]), 0.6f, $"{id}: el bucle no debe tener un salto brusco.");
            }
        }

        [UnityTest]
        public IEnumerator Decorator_KeepsEveryPropClearOfTheTrack()
        {
            foreach (string id in new[] { "percusalia", "treble_spire", "bassline_abyss" })
            {
                GameObject go = new GameObject("TestLevelManager");
                LevelManager manager = go.AddComponent<LevelManager>();
                manager.SetAutoBuild(false);
                int index = System.Array.IndexOf(PlayableIds(), id) + 1;
                DomainTheme.SetCurrent(DomainTheme.Get(id));
                manager.GenerateSingleLevel(AudioAnalysisResult.CreateFlat(140f, 64), index, PlayableIds().Length, 0f);
                DomainDecorator decorator = DomainDecorator.Build(manager, index, id, null);
                yield return null;

                Assert.Greater(decorator.PropCount, 20, $"{id}: debe haber muchos props.");

                Transform scenery = decorator.transform.Find("Scenery");
                Assert.IsNotNull(scenery);
                foreach (Transform prop in scenery)
                {
                    Renderer[] renderers = prop.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers)
                    {
                        float nearest = Mathf.Min(Mathf.Abs(r.bounds.min.z), Mathf.Abs(r.bounds.max.z));
                        bool straddles = r.bounds.min.z < 0f && r.bounds.max.z > 0f;
                        Assert.IsFalse(straddles, $"{id}: {prop.name} cruza la pista.");
                        // Allow small parts of a prop (e.g. a hanging crystal) to sit closer than its centre.
                        Assert.GreaterOrEqual(nearest, manager.TrackHalfWidth - 0.5f, $"{id}: {prop.name} invade la pista.");
                    }
                }

                Object.Destroy(decorator.gameObject);
                Object.Destroy(go);
                yield return null;
            }
        }

        [Test]
        public void Fonts_OrbitronRajdhaniAndIconsAreAvailable()
        {
            Assert.IsNotNull(UIFactory.TitleFont, "Orbitron");
            Assert.IsNotNull(UIFactory.Font, "Rajdhani");
            Assert.IsNotNull(UIFactory.FontBold, "Rajdhani Bold");
            Assert.IsNotNull(UIFactory.IconFont, "Material Icons");
            StringAssert.Contains("Orbitron", UIFactory.TitleFont.name);
            StringAssert.Contains("Rajdhani", UIFactory.Font.name);
            StringAssert.Contains("MaterialIcons", UIFactory.IconFont.name);
            Assert.IsNotNull(Resources.Load<Material>("NitroRhythm/Fonts/Orbitron SDF Glow"), "Material con brillo (sobrevive al build).");
        }

        [UnityTest]
        public IEnumerator Hud_BuildsAndUpdatesWithoutErrors()
        {
            GameObject go = new GameObject("TestHud");
            go.AddComponent<GameplayHud>();
            yield return null;
            yield return null;
            yield return null;

            Canvas canvas = go.GetComponentInChildren<Canvas>();
            Assert.IsNotNull(canvas, "El HUD debe crear su canvas.");
            Assert.Greater(canvas.transform.childCount, 8, "El HUD debe tener sus tarjetas, anillo y barras.");
            TMP_Text[] texts = go.GetComponentsInChildren<TMP_Text>(true);
            Assert.Greater(texts.Length, 10);

            Object.Destroy(go);
            yield return null;
        }

        [Test]
        public void Results_RankThresholds()
        {
            Assert.AreEqual("S", ResultsScreen.RankFor(13000));
            Assert.AreEqual("A", ResultsScreen.RankFor(9000));
            Assert.AreEqual("B", ResultsScreen.RankFor(6000));
            Assert.AreEqual("C", ResultsScreen.RankFor(100));
        }

        [Test]
        public void UiFactory_BuildsNeonControls()
        {
            Canvas canvas = UIFactory.CreateCanvas("TestCanvas");
            UnityEngine.UI.Button button = UIFactory.CreateButton(canvas.transform, "PRUEBA", Vector2.zero, new Vector2(300f, 70f), UIFactory.NeonCyan, null, UIIcons.Play);
            Assert.IsNotNull(button.transform.Find("Glow"), "El botón debe tener halo.");
            Assert.IsNotNull(button.transform.Find("Frame"), "El botón debe tener marco.");
            NeonBar bar = UIFactory.CreateNeonBar(canvas.transform, "Bar", UIFactory.NeonGreen, new Vector2(200f, 16f), Vector2.zero);
            bar.SetValue(0.5f, true);
            Assert.IsNotNull(UIArt.Portrait("lyra"), "Retrato de Lyra.");
            Assert.IsNotNull(UIArt.Portrait("karel"));
            Assert.IsNotNull(UIArt.Portrait("vox"));
            foreach (string id in PlayableIds()) Assert.IsNotNull(UIArt.Domain(id), $"Ilustración de {id}.");
            Object.DestroyImmediate(canvas.gameObject);
        }
    }
}
