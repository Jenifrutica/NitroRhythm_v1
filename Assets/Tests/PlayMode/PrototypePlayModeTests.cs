using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NitroRhythm.Audio;
using NitroRhythm.Combat;
using NitroRhythm.Controls;
using NitroRhythm.Core;
using NitroRhythm.Data;
using NitroRhythm.Player;

namespace NitroRhythm.Tests
{
    /// <summary>
    /// PlayMode test suite proving the core NitroRhythm systems work end to end:
    /// narrative database, audio analysis, reactive track building, level
    /// generation, kart physics, jumping, villain driving and projectiles.
    /// </summary>
    public class PrototypePlayModeTests
    {
        [Test]
        public void PrototypeData_LoadsTenDomainsAndThreeCharacters()
        {
            PrototypeData data = PrototypeData.Instance;
            Assert.IsNotNull(data);
            Assert.AreEqual(10, data.levels.Length, "Debe haber 10 dominios musicales.");
            Assert.AreEqual(3, data.characters.Length, "Deben existir Lyra, Karel y Vox.");
            Assert.AreEqual(7, data.PlayableCount, "Deben existir 7 dominios jugables.");
            Assert.IsNotNull(data.GetCharacter("lyra"));
            Assert.IsNotNull(data.GetCharacter("vox"));
            Assert.IsNotNull(data.GetCutscene("intro"));
        }

        [UnityTest]
        public IEnumerator AudioAnalysis_DetectsTempoAndBeats()
        {
            AudioClip clip = MusicGenerator.CreateDemoLoop(150f, 4);
            Assert.IsNotNull(clip);

            AudioAnalysisResult result = AudioAnalysisService.Analyze(clip);
            Assert.IsTrue(result.IsValid, "El análisis debe producir beats válidos.");
            Assert.Greater(result.beatCount, 0);
            Assert.Greater(result.bpm, 60f);
            Assert.Less(result.bpm, 200f);

            yield return null;
        }

        [Test]
        public void ReactiveTrack_MapsBassToGapsAndTrebleToObstacles()
        {
            AudioAnalysisResult analysis = AudioAnalysisResult.CreateFlat(150f, 64);
            LevelDefinition level = PrototypeData.Instance.GetLevel("percusalia");

            ProceduralTrackPlan plan = ProceduralTrackBuilder.Build(analysis, level, 0f);

            Assert.Greater(plan.segments.Count, 6, "La pista debe tener varios segmentos.");

            bool hasGap = false;
            bool hasObstacle = false;
            bool hasJumpPad = false;
            foreach (TrackSegment segment in plan.segments)
            {
                if (segment.gap > 0f) hasGap = true;
                if (segment.obstacleCount > 0) hasObstacle = true;
                if (segment.hasJumpPad) hasJumpPad = true;
            }

            Assert.IsTrue(hasGap, "Los graves deben crear huecos.");
            Assert.IsTrue(hasObstacle, "Los agudos deben crear obstáculos.");
            Assert.IsTrue(hasJumpPad, "Los graves fuertes deben crear pads de salto.");
        }

        [UnityTest]
        public IEnumerator LevelManager_GeneratesSevenAudioReactiveDomains()
        {
            GameObject go = new GameObject("TestLevelManager");
            LevelManager levelManager = go.AddComponent<LevelManager>();
            levelManager.SetAutoBuild(false);

            levelManager.GenerateFromAudio(AudioAnalysisResult.CreateFlat(140f, 64), 7, 0f);
            yield return null;

            Assert.AreEqual(7, levelManager.TotalLevels);
            Assert.Greater(levelManager.GetLevelEndX(1), levelManager.GetLevelStartX(1));
            Assert.Less(levelManager.GetLevelStartX(2), levelManager.GetLevelEndX(2));

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Kart_AcceleratesForward_WithScriptedInput()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, 0f, 0f);
            ground.transform.localScale = new Vector3(80f, 1f, 80f);

            GameObject kartObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kartObject.transform.position = new Vector3(0f, 1f, 0f);

            PlayerKartController kart = kartObject.AddComponent<PlayerKartController>();
            ScriptedInputProvider input = new ScriptedInputProvider();
            kart.SetInputProvider(input);
            input.SetDrive(1f, 0f);

            Vector3 start = kartObject.transform.position;
            for (int i = 0; i < 90; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float travelled = Vector3.Distance(start, kartObject.transform.position);
            Assert.Greater(travelled, 1.5f, $"El kart debe avanzar. Recorrido: {travelled:F2}");

            Object.Destroy(kartObject);
            Object.Destroy(ground);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Kart_JumpsWhenGrounded()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(80f, 1f, 80f);

            GameObject kartObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kartObject.transform.position = new Vector3(0f, 1f, 0f);

            PlayerKartController kart = kartObject.AddComponent<PlayerKartController>();
            ScriptedInputProvider input = new ScriptedInputProvider();
            kart.SetInputProvider(input);
            input.SetDrive(1f, 0f);

            // Let the ground check settle.
            for (int i = 0; i < 6; i++) yield return new WaitForFixedUpdate();

            float yBefore = kartObject.transform.position.y;
            input.RequestJump();

            float peak = yBefore;
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, kartObject.transform.position.y);
            }

            Assert.Greater(peak, yBefore + 0.3f, "El kart debe saltar cuando está en el suelo.");

            Object.Destroy(kartObject);
            Object.Destroy(ground);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VillainBoss_DrivesForward()
        {
            GameObject villainObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            villainObject.transform.position = new Vector3(0f, 1.2f, 0f);

            Rigidbody body = villainObject.AddComponent<Rigidbody>();
            body.useGravity = true;

            VillainBoss villain = villainObject.AddComponent<VillainBoss>();
            villain.SetPlayerControlled(false);

            float x0 = villainObject.transform.position.x;
            float t = 0f;
            while (t < 1.0f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            Assert.Greater(villainObject.transform.position.x, x0 + 1f, "El villano IA debe avanzar por la pista.");

            Object.Destroy(villainObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BadReward_TravelsParabola_ThenReturnsToPhysics()
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectile.transform.position = Vector3.zero;
            BoxCollider collider = projectile.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            Rigidbody projectileBody = projectile.AddComponent<Rigidbody>();
            projectileBody.isKinematic = true;
            projectileBody.useGravity = false;

            BadReward reward = projectile.AddComponent<BadReward>();
            Vector3 start = projectile.transform.position;
            reward.Launch(start, new Vector3(12f, 0f, 0f), 5f);

            float t = 0f;
            float peakY = start.y;
            while (t < 2f)
            {
                t += Time.deltaTime;
                if (projectile != null)
                {
                    peakY = Mathf.Max(peakY, projectile.transform.position.y);
                }
                yield return null;
            }

            Assert.Greater(peakY, start.y + 1f, "El proyectil debe elevarse en arco parabólico.");
            Assert.IsTrue(projectile == null || !projectile.GetComponent<Rigidbody>().isKinematic,
                "Tras aterrizar, la física debe reactivarse.");

            if (projectile != null) Object.Destroy(projectile);
            yield return null;
        }

        [Test]
        public void GameSession_StoresCharacterSelection()
        {
            GameSession session = GameSession.EnsureExists();
            session.P1CharacterId = "lyra";
            session.P2CharacterId = "karel";

            Assert.AreEqual("Lyra Pulse", session.GetCharacter(1).displayName);
            Assert.AreEqual("Karel Volt", session.GetCharacter(2).displayName);
            Assert.IsTrue(session.GetVillain().IsVillain);
        }

        [Test]
        public void InputProviders_AreDecoupledFromPhysics()
        {
            PlayerKartController.InputScheme scheme = PlayerKartController.InputScheme.PlayerOne;
            KeyboardInputProvider keyboard = new KeyboardInputProvider(scheme);
            Assert.IsNotNull(keyboard);

            ScriptedInputProvider scripted = new ScriptedInputProvider();
            scripted.SetDrive(0.5f, -0.25f);
            Assert.AreEqual(0.5f, scripted.Throttle, 0.001f);
            Assert.AreEqual(-0.25f, scripted.Steer, 0.001f);

            scripted.RequestJump();
            Assert.IsTrue(scripted.ConsumeJump());
            Assert.IsFalse(scripted.ConsumeJump(), "El salto debe consumirse una sola vez.");
        }
    }
}
