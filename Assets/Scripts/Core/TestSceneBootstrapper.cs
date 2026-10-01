using System.Collections;
using NitroRhythm.Audio;
using NitroRhythm.Combat;
using NitroRhythm.Data;
using NitroRhythm.Narrative;
using NitroRhythm.Player;
using NitroRhythm.UI;
using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Gameplay scene bootstrapper. Reads the <see cref="GameSession"/> selected in
    /// the menu and builds the run: audio-reactive procedural track, players
    /// (coloured from their character definition), villain, split-screen cameras,
    /// environment theme, cutscene/dialogue systems and HUD.
    /// </summary>
    public class TestSceneBootstrapper : MonoBehaviour
    {
        public static TestSceneBootstrapper Instance { get; private set; }

        [Header("Track")]
        [SerializeField] private float _trackStartX = 0f;
        [SerializeField] private float _fallbackLevelLength = 140f;
        [SerializeField] private bool _useAudioReactive = true;

        [Header("Camera Setup")]
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 4.6f, -10.5f);
        [SerializeField] private float _cameraSmoothing = 6f;

        [Header("Debug")]
        [SerializeField] private bool _spawnOnStart = true;

        public GameMode CurrentMode { get; private set; } = GameMode.TwoPlayerCoop;
        public GameObject Player1 { get; private set; }
        public GameObject Player2 { get; private set; }
        public GameObject Player3 { get; private set; }
        public GameObject Villain { get; private set; }
        public LevelManager LevelManager { get; private set; }

        private Rect _p1Rect = new Rect(0f, 0f, 1f, 1f);
        private Rect _p2Rect = new Rect(0.5f, 0f, 0.5f, 1f);
        private Rect _p3Rect = new Rect(0f, 0f, 1f, 0.5f);
        private bool _spawned;
        private bool _trackGenerated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (!_spawnOnStart) return;

            GameSession session = GameSession.EnsureExists();
            StartGame(session.Mode);
        }

        public void StartGame(GameMode mode)
        {
            if (_spawned) return;
            _spawned = true;

            CurrentMode = mode;
            GetViewportRects(mode, out _p1Rect, out _p2Rect, out _p3Rect);

            InitializeTrackAndAudio();
            SpawnEntities(mode);
            SetupCameras(mode);
            SetupSystems();
        }

        // ---------------------------------------------------------------- track

        private void InitializeTrackAndAudio()
        {
            GameObject levelManagerObject = new GameObject("LevelManager");
            LevelManager = levelManagerObject.AddComponent<LevelManager>();
            LevelManager.SetAutoBuild(false);

            if (_useAudioReactive)
            {
                GameObject audioObject = new GameObject("Music");
                AudioReactiveMusicController music = audioObject.AddComponent<AudioReactiveMusicController>();
                music.AnalysisReady += OnAnalysisReady;
            }
            else
            {
                GenerateFallbackTrack();
            }
        }

        private void OnAnalysisReady(AudioAnalysisResult analysis)
        {
            if (_trackGenerated) return;

            int playable = PrototypeData.Instance.PlayableCount;
            LevelManager.GenerateFromAudio(analysis, Mathf.Max(1, playable), _trackStartX);
            _trackGenerated = true;
        }

        private void GenerateFallbackTrack()
        {
            int levels = Mathf.Max(1, PrototypeData.Instance.PlayableCount);
            LevelManager.Configure(levels, _fallbackLevelLength, _trackStartX);
            LevelManager.GenerateTrack();
            _trackGenerated = true;
        }

        // ------------------------------------------------------------- entities

        private void SpawnEntities(GameMode mode)
        {
            Vector3 kartScale = new Vector3(2.2f, 1.2f, 3.8f);
            Quaternion facingTrack = Quaternion.Euler(0f, 90f, 0f);
            Vector3 baseSpawn = LevelManager.GetPlayerSpawnPoint();
            baseSpawn.y = 1.2f;

            LevelManager.ClearPlayers();

            GameSession session = GameSession.EnsureExists();

            // Player 1 — always playable.
            CharacterDefinition p1Def = session.GetCharacter(1);
            Player1 = CreatePlayerKart("Player1", p1Def, baseSpawn + new Vector3(0f, 0f, -1.6f), facingTrack, kartScale,
                PlayerKartController.InputScheme.PlayerOne);
            Player1.AddComponent<KartHealthSpeed>().SetFlashRect(_p1Rect);

            if (mode != GameMode.SinglePlayer)
            {
                CharacterDefinition p2Def = session.GetCharacter(2);
                bool isBot = mode == GameMode.SinglePlayerAndBot;
                Player2 = CreatePlayerKart("Player2", isBot ? null : p2Def,
                    baseSpawn + new Vector3(0f, 0f, 1.6f), facingTrack, kartScale,
                    isBot ? PlayerKartController.InputScheme.Bot : PlayerKartController.InputScheme.PlayerTwo);

                if (isBot) Player2.AddComponent<BotKartAI>();
                Player2.AddComponent<KartHealthSpeed>().SetFlashRect(_p2Rect);
            }

            if (mode == GameMode.ThreePlayerShowdown)
            {
                CharacterDefinition villainDef = session.GetVillain();
                Player3 = CreatePlayerKart("Player3", villainDef, baseSpawn, facingTrack, kartScale,
                    PlayerKartController.InputScheme.PlayerThree);

                VillainBoss villainBoss = Player3.AddComponent<VillainBoss>();
                villainBoss.SetPlayerControlled(true);
                Player3.AddComponent<KartHealthSpeed>().SetFlashRect(_p3Rect);
            }
            else
            {
                CharacterDefinition villainDef = session.GetVillain();
                Villain = CreateVillainKart(villainDef, kartScale, facingTrack);
                VillainBoss villainBoss = Villain.AddComponent<VillainBoss>();
                villainBoss.SetPlayerControlled(false);
                villainBoss.RepositionToLevel(LevelManager, 60f);
            }
        }

        private GameObject CreatePlayerKart(string name, CharacterDefinition def, Vector3 position, Quaternion rotation, Vector3 scale, PlayerKartController.InputScheme scheme)
        {
            Color color = def != null ? def.Color : VisualEntityFactory.Player1Color;
            GameObject kart = VisualEntityFactory.CreateKartEntity(name, color, null, def != null ? def.kartModel : null, def != null ? def.texture : null);
            kart.transform.localScale = scale;
            kart.transform.rotation = rotation;
            kart.transform.position = position;

            PlayerKartController controller = kart.AddComponent<PlayerKartController>();
            controller.SetInputScheme(scheme);
            if (def != null) controller.ApplyCharacterStats(def.speed, def.grip, def.power);

            LevelManager.RegisterPlayer(controller);
            return kart;
        }

        private GameObject CreateVillainKart(CharacterDefinition def, Vector3 scale, Quaternion rotation)
        {
            Color color = def != null ? def.Color : VisualEntityFactory.VillainColor;
            GameObject villain = VisualEntityFactory.CreateKartEntity(VisualEntityFactory.VillainName, color, null, def != null ? def.kartModel : "Kart_Neon_03", def != null ? def.texture : "Vox");
            villain.transform.localScale = scale;
            villain.transform.rotation = rotation;

            Rigidbody body = villain.AddComponent<Rigidbody>();
            body.mass = 50f;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            return villain;
        }

        // -------------------------------------------------------------- cameras

        private static void GetViewportRects(GameMode mode, out Rect p1, out Rect p2, out Rect p3)
        {
            p1 = new Rect(0f, 0f, 1f, 1f);
            p2 = new Rect(0.5f, 0f, 0.5f, 1f);
            p3 = new Rect(0f, 0f, 1f, 0.5f);

            switch (mode)
            {
                case GameMode.SinglePlayer:
                    p1 = new Rect(0f, 0f, 1f, 1f);
                    break;
                case GameMode.SinglePlayerAndBot:
                case GameMode.TwoPlayerCoop:
                    p1 = new Rect(0f, 0f, 0.5f, 1f);
                    p2 = new Rect(0.5f, 0f, 0.5f, 1f);
                    break;
                case GameMode.ThreePlayerShowdown:
                    p1 = new Rect(0f, 0.5f, 0.5f, 0.5f);
                    p2 = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    p3 = new Rect(0f, 0f, 1f, 0.5f);
                    break;
            }
        }

        private void SetupCameras(GameMode mode)
        {
            Camera p1Camera = Camera.main;
            if (p1Camera == null)
            {
                GameObject cameraObject = new GameObject("MainCamera") { tag = "MainCamera" };
                p1Camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            p1Camera.rect = _p1Rect;
            ConfigureFollowCamera(p1Camera, Player1.transform);

            if (mode == GameMode.SinglePlayer) return;

            GameObject p2CameraObject = new GameObject("CameraP2");
            Camera p2Camera = p2CameraObject.AddComponent<Camera>();
            p2Camera.rect = _p2Rect;
            ConfigureFollowCamera(p2Camera, Player2.transform);

            if (mode == GameMode.ThreePlayerShowdown && Player3 != null)
            {
                GameObject p3CameraObject = new GameObject("CameraP3");
                Camera p3Camera = p3CameraObject.AddComponent<Camera>();
                p3Camera.rect = _p3Rect;
                ConfigureFollowCamera(p3Camera, Player3.transform);
            }
        }

        private void ConfigureFollowCamera(Camera camera, Transform target)
        {
            camera.transform.position = target.position + target.rotation * _cameraOffset;
            camera.transform.LookAt(target.position + Vector3.up * 1f);

            CameraFollow follow = camera.gameObject.GetComponent<CameraFollow>();
            if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Target = target;
            follow.Offset = _cameraOffset;
            follow.Smoothing = _cameraSmoothing;
        }

        // --------------------------------------------------------------- systems

        private void SetupSystems()
        {
            NeonPostFx.Ensure();

            GameObject systems = new GameObject("Systems");

            systems.AddComponent<EnvironmentManager>();

            GameObject cutscene = new GameObject("CutsceneController");
            cutscene.transform.SetParent(systems.transform, false);
            cutscene.AddComponent<CutsceneController>();
            cutscene.AddComponent<DialogueSystem>();

            GameObject hud = new GameObject("GameplayHud");
            hud.transform.SetParent(systems.transform, false);
            hud.AddComponent<GameplayHud>();
            hud.AddComponent<PauseMenu>();

            GameObject respawn = new GameObject("RespawnManager");
            respawn.transform.SetParent(systems.transform, false);
            respawn.AddComponent<RespawnOnFall>();
        }
    }
}
