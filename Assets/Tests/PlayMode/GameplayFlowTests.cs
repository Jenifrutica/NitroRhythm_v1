using System.Collections;
using NUnit.Framework;
using NitroRhythm.Controls;
using NitroRhythm.Core;
using NitroRhythm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>Plays the real gameplay scene: the kart must be drivable and the camera must stay on it.</summary>
    public class GameplayFlowTests
    {
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator RealScene_KartDrivesAndCameraFollowsIt()
        {
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.SinglePlayer;
            session.BeginRun();
            session.CurrentLevelIndex = 1;

            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(0.5f);

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            Assert.IsNotNull(boot, "Debe existir el bootstrapper.");

            // Wait for the level intro card to finish (it freezes time for ~3 s).
            float guard = 0f;
            while (Time.timeScale == 0f && guard < 8f)
            {
                yield return null;
                guard += Time.unscaledDeltaTime;
            }
            Assert.Greater(Time.timeScale, 0.5f, "El juego no debe quedarse congelado tras la tarjeta de nivel.");

            PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
            kart.SetInputProvider(new ScriptedInputProvider());   // stands in for the keyboard
            ScriptedInputProvider input = (ScriptedInputProvider)typeof(PlayerKartController)
                .GetField("_input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(kart);
            input.SetDrive(1f, 0f);

            // Imported FBX files can carry Blender cameras/lights that would draw the scene from a prize.
            Camera[] cameras = Camera.allCameras;
            foreach (Camera c in cameras) Debug.Log($"[FlowTest] camera '{c.name}' depth={c.depth} parent={(c.transform.parent != null ? c.transform.parent.name : "-")} enabled={c.enabled}");
            Assert.AreEqual(1, cameras.Length, "En 1 jugador solo debe haber una cámara de juego (los FBX no deben traer cámaras).");
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, "Un solo AudioListener.");

            Camera cam = Camera.main;
            Assert.IsNotNull(cam, "Debe haber cámara principal.");
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            Assert.IsNotNull(follow);

            Vector3 start = boot.Player1.transform.position;
            float worst = 0f;
            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(boot.Player1.transform, follow.Target, "La cámara debe seguir al kart del jugador 1.");
                float distance = Vector3.Distance(cam.transform.position, boot.Player1.transform.position);
                worst = Mathf.Max(worst, distance);
                Debug.Log($"[FlowTest] t={i * 0.5f:F1} kart=({boot.Player1.transform.position.x:F1},{boot.Player1.transform.position.y:F1}) speed={kart.CurrentSpeed:F1} grounded={kart.IsGrounded} cam={distance:F1} m timeScale={Time.timeScale}");
            }

            Assert.Greater(boot.Player1.transform.position.x, start.x + 15f, "El kart debe avanzar al acelerar.");
            Assert.Less(worst, 25f, "La cámara debe permanecer cerca del kart.");
        }

        [Test]
        public void PlayerTwo_IsNeverTheSamePilotAsPlayerOne()
        {
            GameSession session = GameSession.EnsureExists();
            session.P1CharacterId = "karel";
            session.P2CharacterId = "karel";
            Assert.AreNotEqual(session.GetCharacter(1).id, session.GetCharacter(2).id, "P1 y P2 deben ser personajes distintos.");
            Assert.IsFalse(session.GetCharacter(2).IsVillain, "P2 no puede ser el villano.");
            session.P1CharacterId = "lyra";
            session.P2CharacterId = "karel";
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator KnockedOutKart_WaitsFiveSecondsThenRevivesAtCheckpoint()
        {
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.SinglePlayer;
            session.BeginRun();
            session.CurrentLevelIndex = 1;
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(0.5f);
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            float guard = 0f;
            while (Time.timeScale == 0f && guard < 8f) { yield return null; guard += Time.unscaledDeltaTime; }

            PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
            KartHealthSpeed health = boot.Player1.GetComponent<KartHealthSpeed>();

            // Drive away from the start, activate a checkpoint behind the kart, then die further ahead.
            ScriptedInputProvider drive = new ScriptedInputProvider();
            kart.SetInputProvider(drive);
            drive.SetDrive(1f, 0f);
            yield return new WaitForSeconds(2f);
            Vector3 checkpoint = boot.Player1.transform.position - Vector3.right * 15f;
            Assert.IsTrue(LevelManager.Instance.ActivateCheckpoint(checkpoint), "El checkpoint debe activarse.");
            yield return new WaitForSeconds(1.5f);
            drive.SetDrive(0f, 0f);
            float deathX = boot.Player1.transform.position.x;
            health.ApplyVillainHit(500f);

            Assert.IsTrue(health.IsDown, "Tras quedar sin vida el kart queda K.O.");
            Assert.IsTrue(kart.ControlLocked, "Un kart K.O. no se puede manejar.");
            Assert.That(health.ReviveTimeLeft, Is.InRange(4.5f, 5.01f), "La cuenta regresiva empieza en 5 s.");

            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(health.IsDown, "A los 2,5 s sigue K.O.");
            Assert.That(health.ReviveTimeLeft, Is.InRange(1.5f, 2.9f));

            yield return new WaitForSeconds(3f);
            Assert.IsFalse(health.IsDown, "A los 5 s revive.");
            Assert.IsFalse(kart.ControlLocked, "Al revivir se puede volver a manejar.");
            Assert.AreEqual(health.MaxHealth, health.Health, 0.01f, "Revive con la vida completa.");
            yield return new WaitForFixedUpdate();
            float reviveX = boot.Player1.transform.position.x;
            Debug.Log($"[Revive] checkpoint x={checkpoint.x:F1} deathX={deathX:F1} reviveX={reviveX:F1}");
            Assert.AreEqual(checkpoint.x, reviveX, 3f, "Debe revivir en el checkpoint, no donde murió.");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Cooperative_PlayersShowTheirOwnDistinctKartsAndCameras()
        {
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.TwoPlayerCoop;
            session.P1CharacterId = "lyra";
            session.P2CharacterId = "karel";
            session.BeginRun();
            session.CurrentLevelIndex = 1;
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(0.5f);
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            float guard = 0f;
            while (Time.timeScale == 0f && guard < 8f) { yield return null; guard += Time.unscaledDeltaTime; }

            Assert.IsNotNull(boot.Player2, "En cooperativo debe existir el jugador 2.");
            Assert.IsNotNull(boot.Player1.transform.Find("Kart_Neon_01_Visual"), "P1 debe usar el kart de Lyra.");
            Assert.IsNotNull(boot.Player2.transform.Find("Kart_Neon_02_Visual"), "P2 debe usar el kart de Karel.");
            Assert.IsNull(boot.Player2.transform.Find("Kart_Neon_01_Visual"), "P2 no puede llevar el kart de Lyra.");

            Assert.Greater(Vector3.Distance(boot.Player1.transform.position, boot.Player2.transform.position), 2.5f, "Los karts de P1 y P2 no pueden ir uno encima del otro.");

            CameraFollow[] follows = Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);
            Assert.AreEqual(2, follows.Length);
            bool p1 = false, p2 = false;
            foreach (CameraFollow f in follows) { if (f.Target == boot.Player1.transform) p1 = true; if (f.Target == boot.Player2.transform) p2 = true; }
            Assert.IsTrue(p1 && p2, "Cada cámara debe seguir a su propio kart.");
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator ReachingTheGoal_AdvancesToTheNextLevel()
        {
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.SinglePlayer;
            session.BeginRun();
            session.CurrentLevelIndex = 1;
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(0.5f);
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            float guard = 0f;
            while (Time.timeScale == 0f && guard < 8f) { yield return null; guard += Time.unscaledDeltaTime; }

            LevelGoalTrigger goal = Object.FindFirstObjectByType<LevelGoalTrigger>();
            Assert.IsNotNull(goal, "El nivel debe tener meta.");

            // Arrive at the goal the way a kart does: drive onto it from behind.
            Vector3 g = goal.transform.position;
            boot.Player1.transform.position = g + new Vector3(-12f, 0f, 0f);
            Rigidbody body = boot.Player1.GetComponent<Rigidbody>();
            body.position = boot.Player1.transform.position;
            ScriptedInputProvider drive = new ScriptedInputProvider();
            boot.Player1.GetComponent<PlayerKartController>().SetInputProvider(drive);
            drive.SetDrive(1f, 0f);

            float waited = 0f;
            while (session.CurrentLevelIndex == 1 && waited < 40f)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;
            }
            Debug.Log($"[Goal] waited={waited:F1}s level={session.CurrentLevelIndex} scene={SceneManager.GetActiveScene().name} timeScale={Time.timeScale} cutscenePlaying={(NitroRhythm.Narrative.CutsceneController.Instance != null && NitroRhythm.Narrative.CutsceneController.Instance.IsPlaying)}");
            Assert.AreEqual(2, session.CurrentLevelIndex, "Al llegar a la meta del nivel 1 debe pasar al nivel 2.");
            yield return new WaitForSecondsRealtime(2.5f);   // let the fade + scene load finish before the next test starts
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Cooperative_GoalAdvancesEvenIfThePartnerStaysBehind()
        {
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.TwoPlayerCoop;
            session.P1CharacterId = "lyra";
            session.P2CharacterId = "karel";
            session.BeginRun();
            session.CurrentLevelIndex = 1;
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(0.5f);
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            float guard = 0f;
            while (Time.timeScale == 0f && guard < 8f) { yield return null; guard += Time.unscaledDeltaTime; }

            LevelGoalTrigger goal = Object.FindFirstObjectByType<LevelGoalTrigger>();
            Vector3 g = goal.transform.position;
            boot.Player1.transform.position = g + new Vector3(-12f, 0f, 0f);
            boot.Player1.GetComponent<Rigidbody>().position = boot.Player1.transform.position;
            ScriptedInputProvider drive = new ScriptedInputProvider();
            boot.Player1.GetComponent<PlayerKartController>().SetInputProvider(drive);
            drive.SetDrive(1f, 0f);

            float waited = 0f;
            while (session.CurrentLevelIndex == 1 && waited < 60f)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;
            }
            Debug.Log($"[GoalCoop] waited={waited:F1}s level={session.CurrentLevelIndex}");
            Assert.AreEqual(2, session.CurrentLevelIndex, "Con el compañero atrás, al pasar el tiempo de gracia debe avanzar al nivel 2.");
            yield return new WaitForSecondsRealtime(2.5f);   // let the fade + scene load finish before the next test starts
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator FirstLevel_ShowsTheControlsGuide_AndRunsOnceClosed()
        {
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.TwoPlayerCoop;
            session.BeginRun();   // new run: the guide has not been shown yet
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(0.5f);

            float guard = 0f;
            while (!NitroRhythm.UI.ControlsOverlay.IsOpen && guard < 30f) { yield return null; guard += Time.unscaledDeltaTime; }
            Assert.IsTrue(NitroRhythm.UI.ControlsOverlay.IsOpen, "Al inicio del primer nivel debe salir la guía de controles.");
            Assert.AreEqual(0f, Time.timeScale, "El juego espera mientras se lee la guía.");
            Assert.IsTrue(session.ControlsGuideShown);

            Object.FindFirstObjectByType<NitroRhythm.UI.ControlsOverlay>().Close();
            yield return null;
            Assert.IsFalse(NitroRhythm.UI.ControlsOverlay.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "Al cerrar la guía el juego corre.");

            // The second level of the same run goes straight to the race (no guide again).
            session.CurrentLevelIndex = 2;
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(0.5f);
            guard = 0f;
            while (Time.timeScale == 0f && guard < 10f) { yield return null; guard += Time.unscaledDeltaTime; }
            Assert.IsFalse(NitroRhythm.UI.ControlsOverlay.IsOpen, "La guía solo sale una vez por partida.");
        }

        /// <summary>Evidence: what the follow camera really sees while driving (scene only, no UI).</summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CaptureFollowCameraView()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("NITRO_SHOT_DIR no definido.");
            System.IO.Directory.CreateDirectory(dir);

            foreach (int level in new[] { 1, 4 })
            {
                GameSession session = GameSession.EnsureExists();
                session.Mode = GameMode.SinglePlayer;
                session.BeginRun();
                session.CurrentLevelIndex = level;
                SceneManager.LoadScene(SceneFlow.Gameplay);
                yield return new WaitForSecondsRealtime(0.5f);
                TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
                float guard = 0f;
                while (Time.timeScale == 0f && guard < 8f) { yield return null; guard += Time.unscaledDeltaTime; }

                PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
                ScriptedInputProvider input = new ScriptedInputProvider();
                kart.SetInputProvider(input);
                input.SetDrive(1f, 0f);

                for (int shot = 0; shot < 4; shot++)
                {
                    yield return new WaitForSeconds(shot == 0 ? 0.5f : 3f);
                    yield return null;
                    Camera cam = Camera.main;
                    RenderTexture rt = new RenderTexture(960, 540, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;
                    RenderTexture.active = rt;
                    Texture2D tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                    RenderTexture.active = null;
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"cam_nivel{level}_{shot}.png"), tex.EncodeToPNG());
                    Debug.Log($"[FlowTest] shot lvl{level}#{shot} cam={cam.transform.position} kart={boot.Player1.transform.position}");
                }
            }
        }
    }
}
