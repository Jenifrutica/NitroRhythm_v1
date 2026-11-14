using System.Collections;
using NUnit.Framework;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Data;
using NitroRhythm.Player;
using NitroRhythm.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>Beat-locked tracks, rigged karts/pilots and the 2D level transition.</summary>
    public class Round6Tests
    {
        [SetUp]
        public void Unpause() => Time.timeScale = 1f;

        [Test]
        public void Track_PlatformEdgesFallOnTheBeats()
        {
            AudioAnalysisResult analysis = AudioAnalysisResult.CreateFlat(128f, 40);
            ProceduralTrackPlan plan = ProceduralTrackBuilder.Build(analysis, PrototypeData.Instance.PlayableLevels[3], 100f);
            float beat = plan.cruiseSpeed * analysis.SecondsPerBeat;

            Assert.AreEqual(analysis.beatCount + 1, plan.beatXs.Count);
            foreach (TrackSegment s in plan.segments)
            {
                float beats = (s.startX - 100f) / beat;
                Assert.That(beats - Mathf.Round(beats), Is.EqualTo(0f).Within(0.01f), $"La plataforma en x={s.startX:F1} debe empezar en un pulso.");
                float gapStart = (s.startX + s.length + s.gap - 100f) / beat;
                Assert.That(gapStart - Mathf.Round(gapStart), Is.EqualTo(0f).Within(0.01f), "El hueco termina justo en el siguiente pulso.");
            }
        }

        [Test]
        public void Track_LevelLastsAsLongAsItsMusicAtCruiseSpeed()
        {
            AudioAnalysisResult analysis = AudioAnalysisResult.CreateFlat(100f, 48);
            ProceduralTrackPlan plan = ProceduralTrackBuilder.Build(analysis, PrototypeData.Instance.PlayableLevels[0], 0f);
            TrackSegment last = plan.segments[plan.segments.Count - 1];
            float seconds = (last.startX + last.length + last.gap) / plan.cruiseSpeed;
            Assert.That(seconds, Is.EqualTo(analysis.duration).Within(analysis.SecondsPerBeat));
        }

        [UnityTest]
        public IEnumerator RiggedKart_HasFourWheelsThatSpinWhenDriving()
        {
            CharacterDefinition def = PrototypeData.Instance.GetCharacter("karel");
            GameObject kart = VisualEntityFactory.CreateKartEntity("RigKart", def.Color, null, def.kartModel, def.texture, def.pilotModel, def.pilotOffset, def.pilotScale);
            Rigidbody rb = kart.AddComponent<Rigidbody>();
            rb.useGravity = false;
            kart.transform.position = new Vector3(0f, 90f, 0f);
            yield return null;

            KartAnimator animator = kart.GetComponent<KartAnimator>();
            Assert.AreEqual(4, animator.WheelCount, "El kart riggeado tiene 4 huesos de rueda.");
            Transform wheel = null;
            foreach (Transform t in kart.GetComponentsInChildren<Transform>()) if (t.name == "Wheel_RL") wheel = t;
            Quaternion previous = wheel.localRotation;
            float turned = 0f;

            float time = 0f;
            for (int frame = 0; time < 0.4f && frame < 2000; frame++)
            {
                rb.linearVelocity = kart.transform.forward * 20f;
                yield return null;
                time += Time.unscaledDeltaTime;
                turned += Quaternion.Angle(previous, wheel.localRotation);   // accumulated (full turns included)
                previous = wheel.localRotation;
            }
            Assert.Greater(turned, 90f, $"Las ruedas giran con la velocidad (giro acumulado {turned:F0}°).");
            Object.Destroy(kart);
        }

        [UnityTest]
        public IEnumerator Pilot_HasSkeletalClipsAndSteersWithTheKart()
        {
            CharacterDefinition def = PrototypeData.Instance.GetCharacter("vox");
            GameObject kart = VisualEntityFactory.CreateKartEntity("RigPilot", def.Color, null, def.kartModel, def.texture, def.pilotModel, def.pilotOffset, def.pilotScale);
            Rigidbody rb = kart.AddComponent<Rigidbody>();
            rb.useGravity = false;
            kart.transform.position = new Vector3(0f, 90f, 0f);
            yield return null;

            PilotAnimator pilot = kart.GetComponent<KartAnimator>().Pilot;
            Assert.IsNotNull(pilot, "El piloto debe tener sus clips (FBX con huesos y animaciones).");
            Assert.IsTrue(pilot.HasClips);

            float time = 0f, maxSteer = 0f;
            for (int frame = 0; time < 0.6f && frame < 2000; frame++)
            {
                rb.linearVelocity = kart.transform.forward * 25f;
                kart.transform.Rotate(0f, -90f * Time.deltaTime, 0f);   // turning left
                yield return null;
                time += Time.unscaledDeltaTime;
                maxSteer = Mathf.Max(maxSteer, pilot.Steer);
            }
            Assert.Greater(maxSteer, 0.2f, "Al girar a la izquierda el piloto gira el volante a la izquierda.");
            Assert.Greater(pilot.Weight("SteerLeft"), 0.1f, "El clip SteerLeft se mezcla.");
            Object.Destroy(kart);
        }

        [UnityTest]
        public IEnumerator LevelTransition_PlaysAndThenContinues()
        {
            bool done = false;
            LevelTransition.Play(2, () => done = true);
            Assert.IsTrue(LevelTransition.IsPlaying);
            Assert.AreEqual(0f, Time.timeScale, "El juego se detiene durante la transición.");

            float time = 0f;
            for (int frame = 0; !done && time < 12f && frame < 20000; frame++)
            {
                yield return null;
                time += Time.unscaledDeltaTime;
            }
            Assert.IsTrue(done, "La transición debe terminar y continuar.");
            Assert.That(time, Is.InRange(2.5f, 8f), $"Duración {time:F1} s");
            Assert.AreEqual(1f, Time.timeScale);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsNull(GameObject.Find("LevelTransition"), "Se limpia sola.");
        }
    }
}
