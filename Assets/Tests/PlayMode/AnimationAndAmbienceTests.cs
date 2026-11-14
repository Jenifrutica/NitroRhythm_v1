using System.Collections;
using NUnit.Framework;
using NitroRhythm.Core;
using NitroRhythm.Data;
using NitroRhythm.Player;
using NitroRhythm.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>Procedural kart/pilot animation and the music-reactive scenery.</summary>
    public class AnimationAndAmbienceTests
    {
        /// <summary>Earlier gameplay tests can leave the game paused (intro card, cutscene).</summary>
        [SetUp]
        public void Unpause() => Time.timeScale = 1f;

        /// <summary>Runs <paramref name="seconds"/> of real time (frame-capped so it can never hang).</summary>
        private static IEnumerator ForSeconds(float seconds, System.Action everyFrame)
        {
            float t = 0f;
            for (int frame = 0; t < seconds && frame < 2000; frame++)
            {
                everyFrame?.Invoke();
                yield return null;
                t += Time.unscaledDeltaTime;
            }
        }
        private static GameObject MakeKart(out CharacterDefinition def)
        {
            def = PrototypeData.Instance.GetCharacter("lyra");
            GameObject kart = VisualEntityFactory.CreateKartEntity("AnimKart", def.Color, null, def.kartModel, def.texture, def.pilotModel, def.pilotOffset, def.pilotScale);
            Rigidbody rb = kart.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            kart.transform.position = new Vector3(0f, 80f, 0f);
            return kart;
        }

        [UnityTest]
        public IEnumerator Animator_KeepsKartAndPilotUnderTheRigWithoutShear()
        {
            GameObject kart = MakeKart(out CharacterDefinition def);
            yield return null;
            yield return null;

            KartAnimator animator = kart.GetComponent<KartAnimator>();
            Assert.IsNotNull(animator, "Todo kart debe llevar animación procedural.");
            Assert.IsNotNull(animator.Pivot);

            Transform visual = null, pilot = null;
            foreach (Transform t in kart.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == $"{def.kartModel}_Visual") visual = t;
                if (t.name == $"{def.pilotModel}_Pilot") pilot = t;
            }
            Assert.IsNotNull(visual);
            Assert.IsNotNull(pilot);
            Assert.AreEqual(animator.Pivot, visual.parent, "El kart cuelga del pivote animado.");

            // The pivot's world matrix must be a pure rotation (no shear from the collider scale).
            Vector3 lossy = animator.Pivot.lossyScale;
            Assert.That(lossy.x, Is.EqualTo(lossy.y).Within(0.05f));
            Assert.That(lossy.z, Is.EqualTo(lossy.y).Within(0.05f));

            Object.Destroy(kart);
        }

        [UnityTest]
        public IEnumerator Animator_RollsIntoTurnsAndSquashesOnLanding()
        {
            GameObject kart = MakeKart(out _);
            Rigidbody rb = kart.GetComponent<Rigidbody>();
            yield return null;
            KartAnimator animator = kart.GetComponent<KartAnimator>();

            // Turn right at speed for a few frames.
            float maxRoll = 0f;
            yield return ForSeconds(0.6f, () =>
            {
                rb.linearVelocity = kart.transform.forward * 25f;
                kart.transform.Rotate(0f, 90f * Time.deltaTime, 0f);
                float roll = Mathf.DeltaAngle(0f, animator.Pivot.localEulerAngles.z);
                if (Mathf.Abs(roll) > Mathf.Abs(maxRoll)) maxRoll = roll;
            });
            Assert.Greater(Mathf.Abs(maxRoll), 1f, "El kart debe inclinarse en las curvas.");

            // Fall, then stop: a landing squash.
            yield return ForSeconds(0.6f, () => rb.linearVelocity = new Vector3(0f, -16f, 0f));
            float minY = 1f;
            yield return ForSeconds(0.3f, () =>
            {
                rb.linearVelocity = Vector3.zero;
                minY = Mathf.Min(minY, animator.Pivot.localScale.y);
            });
            Assert.Less(minY, 0.97f, "Al aterrizar el kart se aplasta.");

            yield return ForSeconds(1.2f, () => rb.linearVelocity = Vector3.zero);
            Assert.That(animator.Pivot.localScale.y, Is.EqualTo(1f).Within(0.03f), "Y vuelve a su forma.");

            Object.Destroy(kart);
        }

        /// <summary>Regression: the hover effect used to repaint buttons with their creation colour,
        /// so the selected game mode and the P1/P2 buttons never showed the choice.</summary>
        [UnityTest]
        public IEnumerator HoverEffect_KeepsTheSelectionColourSetByTheScreen()
        {
            Canvas canvas = NitroRhythm.UI.UIFactory.CreateCanvas("HoverTestCanvas", 0);
            UnityEngine.UI.Button button = NitroRhythm.UI.UIFactory.CreateButton(canvas.transform, "MODO", Vector2.zero, new Vector2(300f, 60f), Color.yellow, null);
            yield return null;

            UnityEngine.UI.Image image = button.GetComponent<UnityEngine.UI.Image>();
            Color selected = new Color(0.45f, 0.34f, 0.04f, 1f);
            image.color = selected;
            for (int i = 0; i < 30; i++) yield return null;

            Assert.That(image.color.r, Is.EqualTo(selected.r).Within(0.01f), "El color de selección no debe perderse.");
            Assert.That(image.color.g, Is.EqualTo(selected.g).Within(0.01f));
            Object.Destroy(canvas.gameObject);
        }

        [UnityTest]
        public IEnumerator Visualizer_BuildsTwoEqualizerWallsThatFollowTheLeader()
        {
            GameObject leader = new GameObject("Leader");
            leader.transform.position = new Vector3(120f, 0f, 0f);
            MusicVisualizer visualizer = MusicVisualizer.Create(DomainTheme.Get("percusalia"), leader.transform, 6f, null);
            yield return null;

            int bars = 0;
            foreach (Transform t in visualizer.GetComponentsInChildren<Transform>())
            {
                if (t.name != "EqBar") continue;
                bars++;
                Assert.Greater(Mathf.Abs(t.position.z), 30f, "Las barras quedan lejos de la pista.");
                Assert.IsNull(t.GetComponent<Collider>(), "El ecualizador es decorado: sin colisiones.");
            }
            Assert.AreEqual(60, bars);
            Assert.That(visualizer.transform.position.x, Is.EqualTo(120f).Within(0.01f));

            Object.Destroy(visualizer.gameObject);
            Object.Destroy(leader);
        }
    }
}
