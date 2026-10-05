using System.Collections;
using System.IO;
using NUnit.Framework;
using NitroRhythm.Controls;
using NitroRhythm.Core;
using NitroRhythm.Player;
using NitroRhythm.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>
    /// Evidence generator (AA3/AA4). Only runs when NITRO_SHOT_DIR is set: plays the
    /// real gameplay scene, forces the HUD events and saves full-screen captures.
    /// </summary>
    public class GameplayEvidenceCapture
    {
        /// <summary>
        /// Renders every gameplay camera (split-screen viewports preserved) and then the
        /// UI canvases into one texture. Works in batch mode, unlike ScreenCapture.
        /// </summary>
        private static IEnumerator Shot(string dir, string file, int width = 1280, int height = 720)
        {
            yield return null;

            RenderTexture rt = new RenderTexture(width, height, 24);

            Camera[] cameras = Camera.allCameras;
            System.Array.Sort(cameras, (x, y) => x.depth.CompareTo(y.depth));
            for (int i = 0; i < cameras.Length; i++)
            {
                RenderTexture previous = cameras[i].targetTexture;
                cameras[i].targetTexture = rt;
                cameras[i].Render();
                cameras[i].targetTexture = previous;
            }

            // Scene pixels first.
            RenderTexture.active = rt;
            Texture2D scene = new Texture2D(width, height, TextureFormat.RGBA32, false);
            scene.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;

            // UI rendered separately over transparent black, then composited.
            RenderTexture uiRt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            GameObject uiCamObject = new GameObject("CaptureUiCam");
            Camera uiCam = uiCamObject.AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiCam.cullingMask = 1 << 31;
            uiCam.orthographic = true;
            uiCam.targetTexture = uiRt;

            // Move the UI hierarchy to a private layer so only it is drawn by the UI camera.
            System.Collections.Generic.List<(Transform t, int layer)> moved = new System.Collections.Generic.List<(Transform, int)>();
            foreach (Canvas canvas in canvases)
            {
                foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    moved.Add((t, t.gameObject.layer));
                    t.gameObject.layer = 31;
                }
            }

            RenderMode[] modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = uiCam;
                canvases[i].planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            uiCam.Render();
            for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
            foreach ((Transform t, int layer) in moved) if (t != null) t.gameObject.layer = layer;

            RenderTexture.active = uiRt;
            Texture2D ui = new Texture2D(width, height, TextureFormat.RGBA32, false);
            ui.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;

            Color32[] basePixels = scene.GetPixels32();
            Color32[] uiPixels = ui.GetPixels32();
            for (int i = 0; i < basePixels.Length; i++)
            {
                float a = uiPixels[i].a / 255f;
                basePixels[i] = new Color32(
                    (byte)Mathf.Min(255, uiPixels[i].r + basePixels[i].r * (1f - a)),
                    (byte)Mathf.Min(255, uiPixels[i].g + basePixels[i].g * (1f - a)),
                    (byte)Mathf.Min(255, uiPixels[i].b + basePixels[i].b * (1f - a)),
                    255);
            }
            scene.SetPixels32(basePixels);
            scene.Apply();
            File.WriteAllBytes(Path.Combine(dir, file), scene.EncodeToPNG());

            Object.Destroy(scene);
            Object.Destroy(ui);
            Object.Destroy(uiCamObject);
            rt.Release();
            uiRt.Release();
            Object.Destroy(rt);
            Object.Destroy(uiRt);
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator CaptureGameplayHud()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }

            Directory.CreateDirectory(dir);
            Screen.SetResolution(1280, 720, false);
            Time.captureDeltaTime = 1f / 30f; // deterministic frame time for batch captures

            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.SinglePlayerAndBot;

            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSeconds(3f);

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            Assert.IsNotNull(boot);
            PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
            ScriptedInputProvider input = new ScriptedInputProvider();
            kart.SetInputProvider(input);
            input.SetDrive(1f, 0f);

            yield return new WaitForSeconds(4f);
            yield return Shot(dir, "gameplay_conduciendo.png");

            kart.ApplySpeedBoost(1.6f, 3f);
            LevelManager.Instance.ActivateCheckpoint(boot.Player1.transform.position + Vector3.right * 5f);
            boot.Player1.GetComponent<KartHealthSpeed>().ApplyVillainHit(15f);
            yield return Shot(dir, "gameplay_impacto_checkpoint_turbo.png");

            // K.O.: the kart waits 5 s with an on-screen countdown before it revives.
            boot.Player1.GetComponent<KartHealthSpeed>().ApplyVillainHit(500f);
            yield return new WaitForSeconds(1.2f);
            yield return Shot(dir, "gameplay_ko_cuenta_regresiva.png");
            Time.captureDeltaTime = 0f;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator CaptureCooperativeSplitScreen()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }

            Directory.CreateDirectory(dir);
            Screen.SetResolution(1280, 720, false);
            Time.captureDeltaTime = 1f / 30f;

            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.TwoPlayerCoop;
            session.P1CharacterId = "lyra";
            session.P2CharacterId = "karel";
            session.BeginRun();

            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(3.4f);
            yield return Shot(dir, "coop_pantalla_dividida.png");
            Time.captureDeltaTime = 0f;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator CaptureLevelIntroCards()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }

            Directory.CreateDirectory(dir);
            Screen.SetResolution(1280, 720, false);
            foreach (int level in new[] { 1, 3, 7 })
            {
                GameSession session = GameSession.EnsureExists();
                session.Mode = GameMode.SinglePlayer;
                session.BeginRun();
                session.CurrentLevelIndex = level;
                SceneManager.LoadScene(SceneFlow.Gameplay);
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Shot(dir, $"tarjeta_nivel_{level}.png");
            }
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator CaptureControlsGuides()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }
            Directory.CreateDirectory(dir);

            // Start of the first level: card, then the guide waits for the player.
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.TwoPlayerCoop;
            session.BeginRun();
            SceneManager.LoadScene(SceneFlow.Gameplay);
            float guard = 0f;
            while (!ControlsOverlay.IsOpen && guard < 25f) { yield return null; guard += Time.unscaledDeltaTime; }
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(ControlsOverlay.IsOpen, "En el primer nivel debe salir la guía de controles al inicio.");
            Assert.AreEqual(0f, Time.timeScale, "El juego espera mientras se lee la guía.");
            yield return Shot(dir, "guia_inicio_cooperativo.png");

            foreach (GameMode mode in new[] { GameMode.SinglePlayer, GameMode.ThreePlayerShowdown })
            {
                ControlsOverlay old = Object.FindFirstObjectByType<ControlsOverlay>();
                if (old != null) old.Close();
                ControlsOverlay.Show(mode, false);
                yield return null;
                yield return Shot(dir, $"guia_{mode}.png");
            }
            foreach (ControlsOverlay open in Object.FindObjectsByType<ControlsOverlay>(FindObjectsSortMode.None)) open.Close();
            yield return null;
            Time.timeScale = 1f;

            // Pause screen with the guide of the current mode.
            PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();
            typeof(PauseMenu).GetMethod("Toggle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pause, null);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot(dir, "pausa_con_controles.png");
            typeof(PauseMenu).GetMethod("Toggle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pause, null);
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator CaptureCharacterSelectAndResults()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }

            Directory.CreateDirectory(dir);
            Time.captureDeltaTime = 1f / 30f;

            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.TwoPlayerCoop;
            session.LastScore = 18450;
            session.LastTime = 187.42f;

            SceneManager.LoadScene(SceneFlow.CharacterSelect);
            yield return new WaitForSeconds(2f);
            yield return Shot(dir, "seleccion_personajes.png");

            // The same screen at other window shapes (browser canvas 16:10, 4:3, ultra-wide).
            CharacterSelectScreen select = Object.FindFirstObjectByType<CharacterSelectScreen>();
            var layout = typeof(CharacterSelectScreen).GetMethod("LayoutStage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var size in new[] { (960, 600), (1024, 768), (1680, 720) })
            {
                Camera.main.aspect = (float)size.Item1 / size.Item2;
                layout.Invoke(select, null);
                yield return null;
                yield return Shot(dir, $"seleccion_{size.Item1}x{size.Item2}.png", size.Item1, size.Item2);
            }
            Camera.main.ResetAspect();
            layout.Invoke(select, null);

            SceneManager.LoadScene(SceneFlow.Results);
            yield return new WaitForSeconds(2f);
            yield return Shot(dir, "resultados_con_tiempo.png");

            Time.captureDeltaTime = 0f;
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator CaptureEveryDomain()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }

            Directory.CreateDirectory(dir);
            Time.captureDeltaTime = 1f / 30f;
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.SinglePlayer;

            int total = NitroRhythm.Data.PrototypeData.Instance.PlayableCount;
            for (int level = 1; level <= total; level++)
            {
                session.BeginRun();
                session.CurrentLevelIndex = level;
                SceneManager.LoadScene(SceneFlow.Gameplay);
                yield return new WaitForSecondsRealtime(1.2f);

                if (level == 3) yield return Shot(dir, "intro_dominio3.png");

                yield return new WaitForSecondsRealtime(3.2f);

                TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
                if (boot == null || boot.Player1 == null) continue;

                // Fixed fly-over camera behind the start of the track (independent of the kart).
                Camera[] gameplayCameras = Camera.allCameras;
                foreach (Camera c in gameplayCameras) c.enabled = false;
                GameObject camObject = new GameObject("OverviewCam");
                Camera overview = camObject.AddComponent<Camera>();
                overview.fieldOfView = 62f;
                overview.clearFlags = CameraClearFlags.Skybox;
                overview.farClipPlane = 800f;
                overview.transform.position = new Vector3(-13f, 5.2f, 0f);
                overview.transform.LookAt(new Vector3(34f, 2f, 0f));
                yield return Shot(dir, $"dominio_{level}.png");
                Object.Destroy(camObject);
                foreach (Camera c in gameplayCameras) if (c != null) c.enabled = true;
            }

            Time.captureDeltaTime = 0f;
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CaptureInterfaceScreens()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }

            Directory.CreateDirectory(dir);
            Time.captureDeltaTime = 1f / 30f;
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.TwoPlayerCoop;
            session.BeginRun();
            session.LastScore = 9240;
            session.LastTime = 412.37f;

            SceneManager.LoadScene(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot(dir, "ui_menu_principal.png");

            SceneManager.LoadScene(SceneFlow.CharacterSelect);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot(dir, "ui_seleccion.png");

            SceneManager.LoadScene(SceneFlow.Results);
            yield return new WaitForSecondsRealtime(3.5f);
            yield return Shot(dir, "ui_resultados.png");

            session.Mode = GameMode.SinglePlayerAndBot;
            session.CurrentLevelIndex = 5;
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Shot(dir, "ui_intro_dominio5.png");
            yield return new WaitForSecondsRealtime(3.5f);

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot != null && boot.Player1 != null)
            {
                PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
                kart.SetInputScheme(PlayerKartController.InputScheme.Bot);
                boot.Player1.AddComponent<BotKartAI>();
                yield return new WaitForSeconds(1.1f);
            }
            yield return Shot(dir, "ui_hud_dominio5.png");

            Time.captureDeltaTime = 0f;
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CaptureVillainAttacks()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura de evidencias omitida.");
            }

            Directory.CreateDirectory(dir);
            Time.captureDeltaTime = 1f / 30f;
            GameSession session = GameSession.EnsureExists();
            session.Mode = GameMode.SinglePlayer;
            session.BeginRun();
            session.CurrentLevelIndex = 7;
            SceneManager.LoadScene(SceneFlow.Gameplay);
            yield return new WaitForSecondsRealtime(3.8f);

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            Assert.IsNotNull(boot);
            NitroRhythm.Combat.VillainBoss villain = boot.Villain.GetComponent<NitroRhythm.Combat.VillainBoss>();
            // The player stays at the start so the camera frames the kart, the marker and the incoming shot.
            System.Collections.Generic.List<NitroRhythm.Combat.VillainBoss.AttackKind> kinds = new System.Collections.Generic.List<NitroRhythm.Combat.VillainBoss.AttackKind>();
            villain.Attacked += k => kinds.Add(k);

            float waited = 0f;
            while (kinds.Count < 1 && waited < 25f)
            {
                yield return null;
                waited += Time.deltaTime;
            }

            yield return new WaitForSeconds(0.55f);
            yield return Shot(dir, "villano_ataque.png");        // projectile in flight + impact marker
            yield return new WaitForSeconds(0.75f);
            yield return Shot(dir, "villano_impacto.png");       // after the explosion

            // Let it play a while and report how the villain behaved.
            yield return new WaitForSeconds(12f);
            Debug.Log($"[VillainReport] attacks in ~{waited + 13.5f:F0}s of play: {kinds.Count} -> {string.Join(",", kinds)}; gap to player {villain.transform.position.x - boot.Player1.transform.position.x:F0} m");

            Time.captureDeltaTime = 0f;
            Assert.GreaterOrEqual(kinds.Count, 3, "El villano debe atacar varias veces.");
        }
    }
}
