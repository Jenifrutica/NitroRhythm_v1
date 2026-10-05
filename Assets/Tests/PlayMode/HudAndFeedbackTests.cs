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
    /// <summary>HUD helpers, checkpoint signalling, smoothed difficulty and impact feedback.</summary>
    public class HudAndFeedbackTests
    {
        [Test]
        public void Hud_FormatTime_UsesMinutesSecondsCentiseconds()
        {
            Assert.AreEqual("00:00.00", GameplayHud.FormatTime(0f));
            Assert.AreEqual("01:05.50", GameplayHud.FormatTime(65.5f));
            Assert.AreEqual("00:00.00", GameplayHud.FormatTime(-3f));
        }

        [Test]
        public void Hud_ComputeRank_CountsOpponentsAhead()
        {
            Assert.AreEqual(1, GameplayHud.ComputeRank(100f, 50f, 20f));
            Assert.AreEqual(2, GameplayHud.ComputeRank(100f, 150f, 20f));
            Assert.AreEqual(3, GameplayHud.ComputeRank(100f, 150f, 200f));
            Assert.AreEqual(1, GameplayHud.ComputeRank(10f));
        }

        [Test]
        public void Track_FirstDomainIsGentlerThanTheLast()
        {
            AudioAnalysisResult analysis = AudioAnalysisResult.CreateFlat(140f, 64);
            ProceduralTrackPlan first = ProceduralTrackBuilder.Build(analysis, PrototypeData.Instance.GetLevel("percusalia"), 0f);
            ProceduralTrackPlan last = ProceduralTrackBuilder.Build(analysis, PrototypeData.Instance.GetLevel("void_crescendo"), 0f);

            float Average(ProceduralTrackPlan plan, System.Func<TrackSegment, float> f)
            {
                float sum = 0f;
                foreach (TrackSegment seg in plan.segments) sum += f(seg);
                return sum / plan.segments.Count;
            }

            Assert.Less(Average(first, s => s.gap), Average(last, s => s.gap), "Los huecos iniciales deben ser menores.");
            Assert.Less(Average(first, s => s.obstacleCount), Average(last, s => s.obstacleCount) + 0.0001f, "Debe haber menos peligros al inicio.");
        }

        [Test]
        public void Track_EveryDomainStartsWithASafeRunway()
        {
            AudioAnalysisResult analysis = AudioAnalysisResult.CreateFlat(150f, 64);
            foreach (LevelDefinition level in PrototypeData.Instance.PlayableLevels)
            {
                ProceduralTrackPlan plan = ProceduralTrackBuilder.Build(analysis, level, 0f);
                Assert.AreEqual(0, plan.segments[0].obstacleCount, $"{level.id}: el primer tramo no debe tener obstáculos.");
                Assert.LessOrEqual(plan.segments[0].gap, 3f, $"{level.id}: el primer hueco debe ser corto.");
            }
        }

        [UnityTest]
        public IEnumerator Checkpoint_RaisesEventAndMovesForwardOnly()
        {
            GameObject go = new GameObject("TestLevelManager");
            LevelManager levelManager = go.AddComponent<LevelManager>();
            levelManager.SetAutoBuild(false);
            yield return null;

            int raised = 0;
            levelManager.CheckpointReached += _ => raised++;

            Vector3 start = levelManager.CurrentCheckpoint;
            Assert.IsTrue(levelManager.ActivateCheckpoint(start + new Vector3(30f, 0f, 0f)));
            Assert.IsFalse(levelManager.ActivateCheckpoint(start + new Vector3(10f, 0f, 0f)), "No debe retroceder.");
            Assert.AreEqual(1, raised);

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VillainHit_RaisesHitEventAndExposesSlowTimer()
        {
            GameObject kartObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kartObject.AddComponent<Rigidbody>();
            PlayerKartController kart = kartObject.AddComponent<PlayerKartController>();
            KartHealthSpeed health = kartObject.AddComponent<KartHealthSpeed>();
            yield return null;

            float reportedDamage = 0f;
            System.Action<KartHealthSpeed, float> handler = (v, d) => reportedDamage = d;
            KartHealthSpeed.Hit += handler;

            health.ApplyVillainHit(15f);
            KartHealthSpeed.Hit -= handler;

            Assert.AreEqual(15f, reportedDamage);
            Assert.AreEqual(85f, health.Health);
            Assert.Greater(health.SlowTimeLeft, 2.5f);
            Assert.AreEqual(0.6f, kart.SpeedMultiplier, 0.001f);

            Object.Destroy(kartObject);
            yield return null;
        }

        [Test]
        public void Kart_BoostExposesTimeLeft()
        {
            GameObject kartObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kartObject.AddComponent<Rigidbody>();
            PlayerKartController kart = kartObject.AddComponent<PlayerKartController>();

            Assert.IsFalse(kart.IsBoosting);
            kart.ApplySpeedBoost(1.6f, 1.2f);
            Assert.IsTrue(kart.IsBoosting);
            Assert.AreEqual(1.2f, kart.BoostTimeLeft, 0.001f);
            Assert.AreEqual(1.6f, kart.BoostMultiplier, 0.001f);

            Object.DestroyImmediate(kartObject);
        }

        [Test]
        public void Sfx_ClipsAreSynthesizedAndAudible()
        {
            AudioClip[] clips = { SfxLibrary.Impact, SfxLibrary.Jump, SfxLibrary.Boost, SfxLibrary.Checkpoint, SfxLibrary.Fire, SfxLibrary.Goal, SfxLibrary.UiHover, SfxLibrary.EngineLoop };
            foreach (AudioClip clip in clips)
            {
                Assert.IsNotNull(clip);
                float[] data = new float[clip.samples];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.Greater(peak, 0.1f, $"{clip.name} suena demasiado bajo.");
                Assert.LessOrEqual(peak, 1f);
            }
        }

        [UnityTest]
        public IEnumerator CameraFollow_ShakeReturnsToRest()
        {
            GameObject target = new GameObject("Target");
            GameObject camObject = new GameObject("Cam");
            CameraFollow follow = camObject.AddComponent<CameraFollow>();
            follow.Target = target.transform;
            camObject.transform.position = target.transform.position + follow.Offset;
            yield return null;

            Vector3 rest = camObject.transform.position;
            follow.Shake(0.5f, 0.2f);
            yield return null;
            yield return new WaitForSecondsRealtime(0.4f);
            yield return null;
            yield return null;

            Assert.Less(Vector3.Distance(rest, camObject.transform.position), 0.15f, "La cámara debe volver a su sitio tras el temblor.");

            Object.Destroy(target);
            Object.Destroy(camObject);
            yield return null;
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Bot_JumpsOverTrackGaps()
        {
            // Regression: the bot used to probe the ground under itself, never saw the
            // chasm coming and fell at the very first gap of every run.
            GameObject go = new GameObject("TestLevelManager");
            LevelManager levelManager = go.AddComponent<LevelManager>();
            levelManager.SetAutoBuild(false);
            levelManager.GenerateFromAudio(AudioAnalysisResult.CreateFlat(140f, 64), 2, 0f);
            yield return new WaitForFixedUpdate();

            GameObject kartObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kartObject.transform.localScale = new Vector3(2.2f, 1.2f, 3.8f);
            Vector3 spawn = levelManager.GetPlayerSpawnPoint();
            kartObject.transform.position = new Vector3(spawn.x + 2f, 1.2f, 0f);
            kartObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            PlayerKartController kart = kartObject.AddComponent<PlayerKartController>();
            kart.SetInputScheme(PlayerKartController.InputScheme.Bot);
            kartObject.AddComponent<NitroRhythm.Player.BotKartAI>();

            float maxX = float.MinValue;
            float elapsed = 0f;
            while (elapsed < 6f)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                maxX = Mathf.Max(maxX, kartObject.transform.position.x);
            }

            Assert.Greater(maxX, spawn.x + 25f, "El bot debe saltar los primeros huecos y cruzar varias plataformas.");

            Object.Destroy(kartObject);
            Object.Destroy(go);
            yield return null;
        }
    }
}
