using System.Collections.Generic;
using UnityEngine;
using NitroRhythm.Player;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Builds and manages the sequential 7-level procedural Obby track.
    /// Levels 1-7 scale in difficulty with narrower platforms, larger jump gaps,
    /// rotating bars, moving hazards, jump/speed pads and static barriers.
    /// The end of each level contains a goal trigger that advances the player
    /// and sets a checkpoint; falling off the track respawns at the checkpoint.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Configuration")]
        [SerializeField] private int _totalLevels = 7;
        [SerializeField] private float _levelLength = 160f;
        [SerializeField] private float _trackHalfWidth = 7f; // track width 14 (platforms are 8-15 wide)

        [Header("Spawning")]
        [SerializeField] private float _startX = 0f;
        [SerializeField] private float _platformHeight = 0f;
        [SerializeField] private float _firstPlatformOffset = 2f;

        [Header("Prefab Overrides (optional, procedural fallback applies when null)")]
        [SerializeField] private GameObject _spinningBarPrefab;
        [SerializeField] private GameObject _movingHazardPrefab;
        [SerializeField] private GameObject _jumpPadPrefab;
        [SerializeField] private GameObject _speedPadPrefab;
        [SerializeField] private GameObject _staticBarrierPrefab;

        private const int MinLevel = 1;
        private const float PlatformThickness = 1f;
        private const float GoalLength = 4f;

        private int _currentLevel = MinLevel;
        private Vector3 _currentCheckpoint;
        private Transform _levelParent;
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private bool _trackBuilt;

        [Header("Audio Reactive")]
        [SerializeField] private bool _useAudioReactive = true;

        [Header("Build")]
        [SerializeField] private bool _autoBuildOnStart = true;

        private readonly List<float> _levelStarts = new List<float>();
        private readonly List<float> _levelLengths = new List<float>();
        private readonly List<NitroRhythm.Data.LevelDefinition> _levelDefs = new List<NitroRhythm.Data.LevelDefinition>();

        public int CurrentLevel => _currentLevel;
        public int CurrentLevelIndex => _currentLevel - 1;
        public int TotalLevels => _totalLevels;
        public float StartX => _startX;
        public float PlayerSpawnX => _currentCheckpoint.x;
        public Vector3 CurrentCheckpoint => _currentCheckpoint;

        /// <summary>All active player karts registered by the bootstrapper for this run.</summary>
        public List<PlayerKartController> Players { get; } = new List<PlayerKartController>();

        public void RegisterPlayer(PlayerKartController player)
        {
            if (player != null && !Players.Contains(player))
            {
                Players.Add(player);
            }
        }

        public void ClearPlayers()
        {
            Players.Clear();
        }

        public static LevelManager Instance { get; private set; }

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
            if (_levelParent == null)
            {
                _levelParent = new GameObject("ProceduralTrack").transform;
            }

            if (_autoBuildOnStart && !_trackBuilt)
            {
                BuildTrack();
            }

            SetCheckpoint(GetLevelStartPosition(_currentLevel));
            Debug.Log($"LevelManager ready — track built for {_totalLevels} levels. Spawn: {_currentCheckpoint}");
        }

        /// <summary>
        /// When false, LevelManager waits for an external plan (e.g. the
        /// audio-reactive track) instead of building the fallback in Start().
        /// </summary>
        public void SetAutoBuild(bool autoBuild)
        {
            _autoBuildOnStart = autoBuild;
        }

        /// <summary>
        /// Configures the track before it is built. Only effective before the track is generated.
        /// </summary>
        public void Configure(int totalLevels, float levelLength, float startX = 0f, float trackHalfWidth = 7f)
        {
            if (_trackBuilt) return;

            _totalLevels = Mathf.Max(1, totalLevels);
            _levelLength = Mathf.Max(30f, levelLength);
            _startX = startX;
            _trackHalfWidth = Mathf.Max(4f, trackHalfWidth);
        }

        /// <summary>Generates the complete multi-level procedural track (idempotent).</summary>
        public void GenerateTrack()
        {
            BuildTrack();
        }

        /// <summary>Builds all level segments as one continuous course.</summary>
        public void BuildTrack()
        {
            if (_trackBuilt) return;

            if (_levelParent == null)
            {
                _levelParent = new GameObject("ProceduralTrack").transform;
            }

            PopulateLinearMetadata();

            for (int level = MinLevel; level <= _totalLevels; level++)
            {
                GenerateLevelSegment(level);
            }

            _trackBuilt = true;
        }

        /// <summary>
        /// Builds every playable domain from an analysed music clip: low
        /// frequencies carve gaps/jump pads, highs place hazards and the BPM
        /// scales spacing/speed. Themes and difficulty come from prototype_data.json.
        /// </summary>
        public void GenerateFromAudio(NitroRhythm.Audio.AudioAnalysisResult analysis, int levelCount, float startX)
        {
            if (_trackBuilt) return;

            if (_levelParent == null)
            {
                _levelParent = new GameObject("ProceduralTrack").transform;
            }

            _startX = startX;
            _totalLevels = Mathf.Max(1, levelCount);

            _levelStarts.Clear();
            _levelLengths.Clear();
            _levelDefs.Clear();

            NitroRhythm.Data.LevelDefinition[] playable = NitroRhythm.Data.PrototypeData.Instance.PlayableLevels;

            float cursor = _startX;
            for (int i = 0; i < _totalLevels; i++)
            {
                NitroRhythm.Data.LevelDefinition def = (playable != null && i < playable.Length) ? playable[i] : null;

                NitroRhythm.Audio.ProceduralTrackPlan plan =
                    NitroRhythm.Audio.ProceduralTrackBuilder.Build(analysis, def, cursor);

                _levelStarts.Add(cursor);
                _levelLengths.Add(plan.levelLength);
                _levelDefs.Add(def);

                GenerateLevelFromPlan(plan, i + MinLevel, def);

                cursor += plan.levelLength;
            }

            _trackBuilt = true;
            Debug.Log($"[LevelManager] Built {_totalLevels} audio-reactive domains.");
        }

        /// <summary>Populates the procedural fallback track metadata (uniform lengths).</summary>
        private void PopulateLinearMetadata()
        {
            _levelStarts.Clear();
            _levelLengths.Clear();
            _levelDefs.Clear();

            NitroRhythm.Data.LevelDefinition[] playable = NitroRhythm.Data.PrototypeData.Instance.PlayableLevels;

            for (int i = 0; i < _totalLevels; i++)
            {
                _levelStarts.Add(_startX + i * _levelLength);
                _levelLengths.Add(_levelLength);
                _levelDefs.Add(playable != null && i < playable.Length ? playable[i] : null);
            }
        }

        private void GenerateLevelFromPlan(NitroRhythm.Audio.ProceduralTrackPlan plan, int level, NitroRhythm.Data.LevelDefinition def)
        {
            int platformIndex = 0;

            foreach (NitroRhythm.Audio.TrackSegment segment in plan.segments)
            {
                SpawnPlatform(level, segment.startX, segment.length,
                    def != null ? def.Ground : (Color?)null,
                    def != null ? def.Accent : (Color?)null);

                if (segment.obstacleCount > 0)
                {
                    for (int i = 0; i < segment.obstacleCount; i++)
                    {
                        float t = (i + 1f) / (segment.obstacleCount + 1f);
                        float obstacleX = segment.startX + segment.length * (0.2f + t * 0.6f);
                        SpawnObstacleByKind(level, obstacleX, segment.length, segment.obstacleKind);
                    }
                }

                if (segment.hasJumpPad)
                {
                    SpawnJumpPad(segment.startX + segment.length * 0.75f, level);
                }

                if (segment.hasSpeedPad)
                {
                    SpawnSpeedPad(segment.startX + segment.length * 0.35f, level);
                }

                if (segment.hasRamp)
                {
                    SpawnRamp(segment.startX + segment.length * 0.85f, level);
                }

                // Intermediate checkpoints every third platform.
                if (platformIndex > 0 && platformIndex % 3 == 0)
                {
                    SpawnCheckpoint(segment.startX + segment.length * 0.5f, level);
                }

                platformIndex++;
            }

            float levelEnd = plan.segments.Count > 0
                ? plan.segments[plan.segments.Count - 1].startX + plan.segments[plan.segments.Count - 1].length
                : plan.levelLength;

            SpawnLevelGoal(levelEnd, level);
        }

        private void SpawnObstacleByKind(int level, float x, float platformWidth, NitroRhythm.Audio.ObstacleKind kind)
        {
            switch (kind)
            {
                case NitroRhythm.Audio.ObstacleKind.StaticBarrier:
                    SpawnStaticBarrier(x, level);
                    break;
                case NitroRhythm.Audio.ObstacleKind.MovingHazard:
                    SpawnMovingHazard(x, platformWidth, level);
                    break;
                default:
                    SpawnSpinningBar(x, level);
                    break;
            }
        }

        /// <summary>Sets the active level and its checkpoint (used for respawn and debug).</summary>
        public void LoadLevel(int levelNumber)
        {
            _currentLevel = Mathf.Clamp(levelNumber, MinLevel, _totalLevels);
            SetCheckpoint(GetLevelStartPosition(_currentLevel));
            Debug.Log($"Loaded Level {_currentLevel} — checkpoint set at {_currentCheckpoint}");
        }

        /// <summary>
        /// Called by a LevelGoalTrigger when a player reaches the end of a level.
        /// Advances to the next level and sets its checkpoint.
        /// </summary>
        public void CompleteLevel(int levelNumber)
        {
            if (levelNumber != _currentLevel) return;

            if (_currentLevel >= _totalLevels)
            {
                Debug.Log("🏆 NitroRhythm Obby complete — all 7 levels cleared!");
                return;
            }

            _currentLevel++;
            SetCheckpoint(GetLevelStartPosition(_currentLevel));

            // Change the environment theme/fog as soon as the next level unlocks.
            if (EnvironmentManager.Instance != null)
            {
                EnvironmentManager.Instance.ApplyLevelTheme(_currentLevel);
            }

            Debug.Log($"Level {levelNumber} complete → Level {_currentLevel}. Checkpoint: {_currentCheckpoint}");
        }

        /// <summary>Compatibility entry point for legacy goal-checkers.</summary>
        public void CheckLevelComplete()
        {
            CompleteLevel(_currentLevel);
        }

        /// <summary>Respawn position for the current level (its checkpoint).</summary>
        public Vector3 GetPlayerSpawnPoint()
        {
            return _currentCheckpoint;
        }

        /// <summary>
        /// Activates an intermediate checkpoint. Only advances forward so a
        /// player cannot "unlock" a checkpoint behind the current one.
        /// </summary>
        public void ActivateCheckpoint(Vector3 position)
        {
            if (position.x <= _currentCheckpoint.x) return;

            position.y = _platformHeight;
            _currentCheckpoint = position;
            Debug.Log($"[LevelManager] Checkpoint activated at {position}");
        }

        /// <summary>Teleports all active players to the current checkpoint, keeping forward speed.</summary>
        public void RepositionPlayersToCheckpoint()
        {
            foreach (PlayerKartController player in Players)
            {
                Rigidbody rb = player.GetComponent<Rigidbody>();
                Vector3 keptVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
                keptVelocity.y = 0f;

                Vector3 spawn = _currentCheckpoint;
                spawn.y = 1.2f;

                player.transform.position = spawn;
                player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(keptVelocity.x, 0f, keptVelocity.z);
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        /// <summary>World X of the goal platform (level end) for a given level.</summary>
        public float GetLevelEndX(int level)
        {
            Vector3 start = GetLevelStartPosition(level);
            return start.x + GetLevelLength(level);
        }

        /// <summary>World X at the start (checkpoint) of a given level.</summary>
        public float GetLevelStartX(int level)
        {
            return GetLevelStartPosition(level).x;
        }

        private float GetLevelLength(int level)
        {
            int index = Mathf.Clamp(level, MinLevel, _totalLevels) - MinLevel;
            if (index >= 0 && index < _levelLengths.Count) return _levelLengths[index];
            return _levelLength;
        }

        private void SetCheckpoint(Vector3 checkpoint)
        {
            _currentCheckpoint = checkpoint;
        }

        private Vector3 GetLevelStartPosition(int level)
        {
            int index = Mathf.Clamp(level, MinLevel, _totalLevels) - MinLevel;
            if (index >= 0 && index < _levelStarts.Count)
            {
                return new Vector3(_levelStarts[index], _platformHeight, 0f);
            }

            return new Vector3(_startX + (level - 1) * _levelLength, _platformHeight, 0f);
        }

        private void GenerateLevelSegment(int level)
        {
            float levelStart = GetLevelStartPosition(level).x;
            float levelLength = GetLevelLength(level);
            float levelEnd = levelStart + levelLength;

            float platformWidth = CalculatePlatformWidth(level);
            float gapSize = CalculateGapSize(level);

            float x = levelStart + _firstPlatformOffset;
            int platformIndex = 0;

            while (x < levelEnd - 10f)
            {
                float width = platformWidth;
                float remaining = levelEnd - 10f - x;
                if (width > remaining)
                {
                    width = Mathf.Max(3f, remaining);
                }

                SpawnPlatform(level, x, width);

                int obstacleCount = CalculateObstacleCount(level, platformIndex);
                for (int i = 0; i < obstacleCount; i++)
                {
                    float t = (i + 1f) / (obstacleCount + 1f);
                    float obstacleX = x + width * 0.15f + t * (width * 0.7f);
                    SpawnObstacle(level, obstacleX, width);
                }

                // A jump pad just before long gaps so they can be cleared.
                if (gapSize >= 8f && platformIndex % 2 == 0)
                {
                    SpawnJumpPad(x + width * 0.75f, level);
                }

                // Sporadic speed pads keep momentum up for the next gap,
                // placed right before a massive ramp for a big launch.
                if (level >= 3 && platformIndex % 3 == 1)
                {
                    SpawnSpeedPad(x + width * 0.4f, level);
                    SpawnRamp(x + width * 0.85f, level);
                }

                // Intermediate checkpoint every third platform.
                if (platformIndex > 0 && platformIndex % 3 == 0)
                {
                    SpawnCheckpoint(x + width * 0.5f, level);
                }

                x += width + gapSize;
                platformIndex++;
            }

            SpawnLevelGoal(levelEnd, level);
        }

        private void SpawnCheckpoint(float x, int level)
        {
            GameObject checkpoint = new GameObject($"Level{level}_Checkpoint");
            checkpoint.transform.SetParent(_levelParent, true);
            checkpoint.transform.position = new Vector3(x, _platformHeight + 1.2f, 0f);

            BoxCollider trigger = checkpoint.AddComponent<BoxCollider>();
            trigger.size = new Vector3(2.5f, 3f, _trackHalfWidth * 2f);
            trigger.isTrigger = true;

            checkpoint.AddComponent<CheckpointComponent>();
            _spawnedObjects.Add(checkpoint);
        }

        private float CalculatePlatformWidth(int level)
        {
            // Grand-scale platforms: 12 units on level 1 shrinking to ~7 by level 7.
            return Mathf.Max(4f, 12f - (level - 1) * 0.8f);
        }

        private float CalculateGapSize(int level)
        {
            // Grand-scale jump gaps grow with level difficulty (~4 to ~19 units).
            return Mathf.Max(3f, 4f + (level - 1) * 2.5f);
        }

        private int CalculateObstacleCount(int level, int platformIndex)
        {
            int count = level >= 6 ? 2 : (level >= 3 ? 1 : 0);
            if (platformIndex >= 3 && Random.value < (level - 1) * 0.1f)
            {
                count++;
            }
            return count;
        }

        private void SpawnObstacle(int level, float x, float platformWidth)
        {
            float roll = Random.value;

            if (level >= 5 && roll < 0.3f)
            {
                SpawnStaticBarrier(x, level);
            }
            else if (level >= 3 && roll < 0.6f)
            {
                SpawnMovingHazard(x, platformWidth, level);
            }
            else
            {
                SpawnSpinningBar(x, level);
            }
        }

        private void SpawnPlatform(int level, float startX, float width, Color? baseColor = null, Color? accentColor = null)
        {
            Vector3 center = new Vector3(startX + width * 0.5f, _platformHeight - PlatformThickness * 0.5f, 0f);
            Vector3 scale = new Vector3(width, PlatformThickness, _trackHalfWidth * 2f);
            GameObject platform = SpawnObstacleBox(null, $"Level{level}_Platform", center, scale, baseColor ?? new Color(0.42f, 0.42f, 0.5f));

            if (baseColor.HasValue && accentColor.HasValue)
            {
                Renderer renderer = platform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = VisualEntityFactory.CreateNeonMaterial(baseColor.Value, accentColor.Value, 0.4f);
                }
            }
        }

        private void SpawnSpinningBar(float x, int level)
        {
            GameObject bar = SpawnObstacleBox(
                _spinningBarPrefab, "SpinningBar",
                new Vector3(x, _platformHeight + 0.9f, 0f),
                new Vector3(1f, 0.3f, 4f),
                new Color(0.95f, 0.55f, 0.1f));

            SpinningBarComponent spinner = bar.GetComponent<SpinningBarComponent>();
            if (spinner == null)
            {
                spinner = bar.AddComponent<SpinningBarComponent>();
            }
            spinner.SpinSpeed = Random.Range(50f, 80f) + level * 10f;
        }

        private void SpawnMovingHazard(float x, float platformWidth, int level)
        {
            GameObject hazard = SpawnObstacleBox(
                _movingHazardPrefab, "MovingHazard",
                new Vector3(x, _platformHeight + 0.8f, 0f),
                new Vector3(1.2f, 0.5f, _trackHalfWidth * 1.6f),
                new Color(1f, 0.85f, 0.1f));

            MovingHazardComponent mover = hazard.GetComponent<MovingHazardComponent>();
            if (mover == null)
            {
                mover = hazard.AddComponent<MovingHazardComponent>();
            }
            mover.MoveSpeed = Random.Range(2f, 3.5f) + level * 0.2f;
            mover.MoveRange = Mathf.Clamp(platformWidth * 0.28f, 1f, 2.8f);

            // Some hazards sweep across the track (Z) to push players off the edge.
            mover.SweepAcrossTrack = Random.value < 0.5f || level >= 4;
            if (mover.SweepAcrossTrack)
            {
                hazard.transform.localScale = new Vector3(1.2f, 0.5f, 2.6f);
            }
        }

        private void SpawnStaticBarrier(float x, int level)
        {
            float height = 1.2f + level * 0.1f;
            SpawnObstacleBox(
                _staticBarrierPrefab, "StaticBarrier",
                new Vector3(x, _platformHeight + height * 0.5f, _trackHalfWidth * 0.35f),
                new Vector3(0.8f, height, 2.5f),
                new Color(0.35f, 0.35f, 0.42f));
        }

        private void SpawnJumpPad(float x, int level)
        {
            GameObject pad = SpawnObstacleBox(
                _jumpPadPrefab, "JumpPad",
                new Vector3(x, _platformHeight + 0.1f, 0f),
                new Vector3(2f, 0.2f, 2f),
                new Color(0.2f, 0.9f, 0.6f));

            SetTrigger(pad);

            BouncingPadComponent bumper = pad.GetComponent<BouncingPadComponent>();
            if (bumper == null)
            {
                bumper = pad.AddComponent<BouncingPadComponent>();
            }
            bumper.BounceForce = Random.Range(10f, 13f) + level * 0.5f;
            bumper.BounceCount = 0;
        }

        private void SpawnSpeedPad(float x, int level)
        {
            GameObject pad = SpawnObstacleBox(
                _speedPadPrefab, "SpeedPad",
                new Vector3(x, _platformHeight + 0.1f, 0f),
                new Vector3(2.4f, 0.15f, 2.4f),
                new Color(0.3f, 0.95f, 1f));

            SetTrigger(pad);

            SpeedPadComponent speedPad = pad.GetComponent<SpeedPadComponent>();
            if (speedPad == null)
            {
                speedPad = pad.AddComponent<SpeedPadComponent>();
            }
            speedPad.BoostMultiplier = 1.4f + level * 0.1f;
            speedPad.BoostDuration = 1.1f;
        }

        private void SpawnRamp(float x, int level)
        {
            float rampHeight = 1.6f + level * 0.15f;

            GameObject ramp = SpawnObstacleBox(
                null, "Ramp",
                new Vector3(x + 2f, _platformHeight + rampHeight * 0.45f, 0f),
                new Vector3(8f, rampHeight, _trackHalfWidth * 1.8f),
                new Color(0.45f, 0.5f, 0.62f));

            ramp.transform.rotation = Quaternion.Euler(0f, 0f, -14f);
        }

        private void SpawnLevelGoal(float levelEndX, int level)
        {
            float goalCenterX = levelEndX;

            // Solid landing platform that bridges into the next level.
            SpawnObstacleBox(
                null, $"Level{level}_GoalPlatform",
                new Vector3(goalCenterX, _platformHeight - PlatformThickness * 0.5f, 0f),
                new Vector3(GoalLength, PlatformThickness, _trackHalfWidth * 2f),
                new Color(0.25f, 0.9f, 0.35f));

            // Trigger volume floating above the goal platform.
            GameObject triggerObj = new GameObject($"Level{level}_GoalTrigger");
            triggerObj.transform.SetParent(_levelParent, true);
            triggerObj.transform.position = new Vector3(goalCenterX, _platformHeight + 1.5f, 0f);

            BoxCollider triggerCollider = triggerObj.AddComponent<BoxCollider>();
            // 10-unit reach so the cutscene triggers when players approach the goal.
            triggerCollider.size = new Vector3(10f, 3f, _trackHalfWidth * 2f);
            triggerCollider.isTrigger = true;

            LevelGoalTrigger goalTrigger = triggerObj.AddComponent<LevelGoalTrigger>();
            goalTrigger.LevelNumber = level;
            _spawnedObjects.Add(triggerObj);

            // Glowing checkpoint zone: emissive disc + point light hovering above the goal.
            GameObject glowDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glowDisc.name = $"Level{level}_CheckpointGlow";
            glowDisc.transform.SetParent(_levelParent, true);
            glowDisc.transform.position = new Vector3(goalCenterX, _platformHeight + 1.6f, 0f);
            glowDisc.transform.localScale = new Vector3(GoalLength * 1.2f, 0.06f, _trackHalfWidth * 1.7f);

            Collider discCollider = glowDisc.GetComponent<Collider>();
            if (discCollider != null)
            {
                Destroy(discCollider);
            }

            Renderer discRenderer = glowDisc.GetComponent<Renderer>();
            if (discRenderer != null)
            {
                discRenderer.sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(new Color(0.35f, 1f, 0.55f));
            }

            Light glowLight = glowDisc.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0.4f, 1f, 0.6f);
            glowLight.intensity = 2.5f;
            glowLight.range = 12f;

            _spawnedObjects.Add(glowDisc);
        }

        private GameObject SpawnObstacleBox(GameObject prefab, string fallbackName, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj;
            if (prefab != null)
            {
                obj = Instantiate(prefab, position, Quaternion.identity, _levelParent);
                obj.transform.localScale = scale;
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = fallbackName;
                obj.transform.SetParent(_levelParent, true);
                obj.transform.position = position;
                obj.transform.localScale = scale;
            }

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = VisualEntityFactory.CreateSolidMaterial(color);
            }

            _spawnedObjects.Add(obj);
            return obj;
        }

        private static void SetTrigger(GameObject obj)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
    }
}
