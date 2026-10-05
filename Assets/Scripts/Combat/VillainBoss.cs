using System.Collections;
using UnityEngine;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Player;

namespace NitroRhythm.Combat
{
    /// <summary>
    /// The Red Villain. In AI mode it races just ahead of the lead player (a bounded "leash", so it is
    /// always within shooting range), jumps gaps like the bot, and attacks ON THE BEAT of the music
    /// with four attack types: direct shot, fan burst, mine and a dropped spinning barrier. Every
    /// shot aims at where the target WILL be and shows a marker on the ground first.
    /// In showdown mode it is player-controlled (IJKL to drive, Enter to fire).
    /// </summary>
    public class VillainBoss : MonoBehaviour, IDamageable
    {
        public enum AttackKind { Direct, Burst, Mine, Barrier }

        [Header("Mode")]
        [SerializeField] private bool _playerControlled;

        [Header("AI Racing (leash)")]
        [SerializeField] private float _gapTarget = 26f;
        [SerializeField] private float _leashMax = 38f;
        [SerializeField] private float _teleportDistance = 70f;
        [SerializeField] private float _minSpeed = 0f;
        [SerializeField] private float _maxSpeed = 46f;
        [SerializeField] private float _fallbackSpeed = 30f;
        [SerializeField] private float _levelEndBuffer = 6f;
        [SerializeField] private float _jumpSpeed = 12f;
        [SerializeField] private float _jumpCooldown = 0.35f;

        [Header("Combat")]
        [SerializeField] private float _detectionRange = 80f;
        [SerializeField] private float _fireInterval = 1.8f;
        [SerializeField] private int _beatsPerShot = 2;
        [SerializeField] private float _damage = 15f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _flightTime = 1.1f;
        [SerializeField] private float _arcHeight = 3.5f;
        [SerializeField] private GameObject _projectilePrefab;

        private const float WarmupSeconds = 2.5f;

        private float _health;
        private float _fireTimer;
        private float _warmup = WarmupSeconds;
        private int _lastBeat = -1;
        private float _desiredSpeed;
        private float _lastJumpTime;
        private Rigidbody _rb;
        private LevelManager _levelManager;
        private PlayerKartController[] _players = new PlayerKartController[0];
        private float _playersRefreshTimer;
        private float _stuckTimer;
        private bool _anyPlayerExists;
        private float _clock;
        private float _lastMusicTime = -1f;
        private float _musicStall;

        public bool IsPlayerControlled => _playerControlled;
        public float Health => _health;

        /// <summary>Fired with the chosen kind each time the villain attacks (HUD / tests).</summary>
        public event System.Action<AttackKind> Attacked;
        public int ShotsFired { get; private set; }
        public float DesiredSpeed => _desiredSpeed;

        /// <summary>Compact state for logs and tests.</summary>
        public string DebugState
        {
            get
            {
                AudioReactiveMusicController music = AudioReactiveMusicController.Instance;
                return $"warmup={_warmup:F1} lastBeat={_lastBeat} players={_players.Length} music={(music != null && music.IsReady && music.Source != null && music.Source.isPlaying)} t={(music != null && music.Source != null ? music.Source.time : -1f):F1}";
            }
        }

        private void Start()
        {
            _health = _maxHealth;
            _rb = GetComponent<Rigidbody>();
            _levelManager = FindFirstObjectByType<LevelManager>();
            _fireTimer = _fireInterval;
            _desiredSpeed = _fallbackSpeed;
        }

        private void Update()
        {
            if (_playerControlled)
            {
                HandlePlayerControl();
                return;
            }

            RefreshPlayers();
            if (transform.position.y < -10f)
            {
                RecoverAheadOfLead();
                return;
            }

            UpdateLeash();
            UpdateStuckRecovery();
            UpdateFiring();
        }

        private void FixedUpdate()
        {
            if (_playerControlled || _rb == null) return;

            // Same snappy gravity as the karts (the rigidbody keeps Unity's default as well).
            _rb.AddForce(Vector3.down * 15.2f, ForceMode.Acceleration);

            _rb.linearVelocity = new Vector3(_desiredSpeed, _rb.linearVelocity.y, 0f);

            if (Time.time - _lastJumpTime > _jumpCooldown && IsGrounded() && ShouldJump())
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpSpeed, _rb.linearVelocity.z);
                _lastJumpTime = Time.time;
            }
        }

        // ---------------------------------------------------------------- driving

        private void RefreshPlayers()
        {
            _playersRefreshTimer -= Time.deltaTime;
            if (_playersRefreshTimer > 0f) return;
            _playersRefreshTimer = 0.25f;

            PlayerKartController self = GetComponent<PlayerKartController>();
            System.Collections.Generic.List<PlayerKartController> list = new System.Collections.Generic.List<PlayerKartController>();
            _anyPlayerExists = false;
            foreach (PlayerKartController p in FindObjectsByType<PlayerKartController>(FindObjectsSortMode.None))
            {
                if (p == self || p == null) continue;
                _anyPlayerExists = true;
                if (p.transform.position.y < -5f) continue;   // falling: not a valid target / leader
                KartHealthSpeed down = p.GetComponent<KartHealthSpeed>();
                if (down != null && down.IsDown) continue;     // K.O.: wait for the revival
                list.Add(p);
            }
            _players = list.ToArray();
        }

        private PlayerKartController FindLead()
        {
            PlayerKartController lead = null;
            foreach (PlayerKartController p in _players)
            {
                if (p == null) continue;
                if (lead == null || p.transform.position.x > lead.transform.position.x) lead = p;
            }
            return lead;
        }

        private void UpdateLeash()
        {
            PlayerKartController lead = FindLead();
            if (lead == null)
            {
                // Players exist but are all falling: wait for them instead of running away.
                _desiredSpeed = _anyPlayerExists ? 0f : _fallbackSpeed;
                return;
            }

            float gap = transform.position.x - lead.transform.position.x;

            if (gap > _teleportDistance || gap < -10f)
            {
                RecoverAheadOfLead();
                return;
            }

            // Keep a bounded lead: slow down when too far, speed up when the players close in.
            float leadSpeed = 0f;
            Rigidbody leadBody = lead.GetComponent<Rigidbody>();
            if (leadBody != null) leadSpeed = Mathf.Max(0f, leadBody.linearVelocity.x);

            float speed = leadSpeed - 0.9f * (gap - _gapTarget);
            if (gap > _leashMax) speed = Mathf.Min(speed, leadSpeed * 0.6f);
            _desiredSpeed = Mathf.Clamp(speed, _minSpeed, _maxSpeed);

            // Never run past the end of the level.
            if (_levelManager != null)
            {
                float levelEnd = _levelManager.GetLevelEndX(_levelManager.CurrentLevel) - _levelEndBuffer;
                if (levelEnd > 0f && transform.position.x > levelEnd)
                {
                    transform.position = new Vector3(levelEnd, transform.position.y, transform.position.z);
                    _desiredSpeed = Mathf.Min(_desiredSpeed, 0f);
                }
            }
        }

        /// <summary>If the villain wants to move but is not moving (wedged on scenery), put it back on track.</summary>
        private void UpdateStuckRecovery()
        {
            float actual = _rb != null ? Mathf.Abs(_rb.linearVelocity.x) : _desiredSpeed;
            if (_desiredSpeed > 10f && actual < 2f) _stuckTimer += Time.deltaTime;
            else _stuckTimer = 0f;

            if (_stuckTimer > 1.5f)
            {
                _stuckTimer = 0f;
                RecoverAheadOfLead();
            }
        }

        private bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, 1.1f, ~0, QueryTriggerInteraction.Ignore);
        }

        private bool ShouldJump()
        {
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            float probe = Mathf.Clamp(Mathf.Abs(_desiredSpeed) * 0.16f, 2.5f, 12f);
            if (!Physics.Raycast(origin + Vector3.right * probe, Vector3.down, 3.5f, ~0, QueryTriggerInteraction.Ignore)) return true;
            return Physics.Raycast(origin, Vector3.right, 8f, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Puts the villain back on the track just ahead of the lead player.</summary>
        private void RecoverAheadOfLead()
        {
            PlayerKartController lead = FindLead();
            float x = lead != null ? lead.transform.position.x + _gapTarget : transform.position.x;

            for (int step = 0; step < 25; step++)
            {
                float tryX = x + step * 2f;
                if (Physics.Raycast(new Vector3(tryX, 25f, 0f), Vector3.down, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    transform.position = new Vector3(tryX, hit.point.y + 1.3f, 0f);
                    transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    if (_rb != null) _rb.linearVelocity = new Vector3(_desiredSpeed, 0f, 0f);
                    return;
                }
            }

            RespawnAtCheckpoint();
        }

        // ----------------------------------------------------------------- combat

        private void HandlePlayerControl()
        {
            PlayerKartController kart = GetComponent<PlayerKartController>();
            bool wantsToFire = kart != null ? kart.ConsumeFireInput() : Input.GetKeyDown(KeyCode.Return);

            if (wantsToFire)
            {
                PlayerKartController target = FindClosestPlayer();
                if (target != null) Attack(target, AttackKind.Direct);
            }
        }

        private void UpdateFiring()
        {
            if (_warmup > 0f)
            {
                _warmup -= Time.deltaTime;
                return;
            }

            if (!ShouldFireNow()) return;

            PlayerKartController target = FindClosestPlayer();
            if (target == null) return;
            if (Mathf.Abs(target.transform.position.x - transform.position.x) > _detectionRange) return;

            Attack(target, PickAttack());
        }

        /// <summary>True on the beat grid of the music (every N beats); falls back to a timer.</summary>
        private bool ShouldFireNow()
        {
            AudioReactiveMusicController music = AudioReactiveMusicController.Instance;
            if (music != null && music.IsReady && music.Analysis != null && music.Analysis.IsValid
                && music.Source != null && music.Source.isPlaying)
            {
                float secondsPerBeat = Mathf.Max(0.2f, music.Analysis.SecondsPerBeat);

                // Follow the song's clock; when the audio device is not advancing it (batch/headless runs)
                // keep the same beat grid with an internal clock so the attacks never stop.
                float sourceTime = music.Source.time;
                _clock += Time.deltaTime;
                if (Mathf.Abs(sourceTime - _lastMusicTime) > 0.0005f)
                {
                    _clock = sourceTime;
                    _musicStall = 0f;
                }
                else
                {
                    _musicStall += Time.deltaTime;
                }
                _lastMusicTime = sourceTime;

                int beat = Mathf.FloorToInt(_clock / secondsPerBeat);
                if (beat == _lastBeat) return false;
                _lastBeat = beat;
                return beat % Mathf.Max(1, _beatsPerShot) == 0;
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer > 0f) return false;
            _fireTimer = _fireInterval;
            return true;
        }

        private AttackKind PickAttack()
        {
            float roll = Random.value;
            if (roll < 0.45f) return AttackKind.Direct;
            if (roll < 0.65f) return AttackKind.Burst;
            if (roll < 0.85f) return AttackKind.Mine;
            return AttackKind.Barrier;
        }

        public void SetPlayerControlled(bool playerControlled)
        {
            _playerControlled = playerControlled;
        }

        private PlayerKartController FindClosestPlayer()
        {
            PlayerKartController closest = null;
            float minDistance = float.MaxValue;
            PlayerKartController self = GetComponent<PlayerKartController>();

            PlayerKartController[] candidates = _playerControlled
                ? FindObjectsByType<PlayerKartController>(FindObjectsSortMode.None)
                : _players;

            foreach (PlayerKartController player in candidates)
            {
                if (player == null || player == self) continue;
                float distance = Mathf.Abs(player.transform.position.x - transform.position.x)
                    + Mathf.Abs(player.transform.position.z - transform.position.z) * 0.5f;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = player;
                }
            }

            return closest;
        }

        /// <summary>Executes an attack on a target (public so tests can drive it deterministically).</summary>
        public void Attack(PlayerKartController target, AttackKind kind)
        {
            if (target == null) return;

            Vector3 predicted = PredictPosition(target, _flightTime);
            Vector3 launch = GetFiringPosition();

            switch (kind)
            {
                case AttackKind.Direct:
                    LaunchProjectile(launch, predicted, false);
                    break;
                case AttackKind.Burst:
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector3 p = predicted + new Vector3(0f, 0f, i * 2.8f);
                        LaunchProjectile(launch, p, false);
                    }
                    break;
                case AttackKind.Mine:
                    LaunchProjectile(launch, predicted + Vector3.right * 6f, true);
                    break;
                case AttackKind.Barrier:
                {
                    Vector3 barrierAt = PredictPosition(target, 1.6f) + Vector3.right * 10f;
                    if (barrierAt.x < transform.position.x - 6f)
                    {
                        StartCoroutine(DropBarrier(barrierAt));
                    }
                    else
                    {
                        // Would land on the villain's own path: throw a mine instead.
                        LaunchProjectile(launch, predicted + Vector3.right * 6f, true);
                    }
                    break;
                }
            }

            ShotsFired++;
            SfxPlayer.Play(SfxLibrary.Fire, 0.5f, Random.Range(0.9f, 1.1f));
            Attacked?.Invoke(kind);
            Debug.Log($"[{name}] Attack {kind} at {target.name}");
        }

        private Vector3 PredictPosition(PlayerKartController target, float seconds)
        {
            Vector3 p = target.transform.position;
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (body != null)
            {
                Vector3 v = body.linearVelocity;
                p += new Vector3(v.x, 0f, v.z) * seconds;
            }
            p.z = Mathf.Clamp(p.z, -5f, 5f);

            if (Physics.Raycast(new Vector3(p.x, p.y + 6f, p.z), Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                p.y = hit.point.y;
            }
            else
            {
                p.y = target.transform.position.y - 0.6f;
            }
            return p;
        }

        private void LaunchProjectile(Vector3 launch, Vector3 target, bool mine)
        {
            GameObject projectile = SpawnProjectile(launch);

            BadReward reward = projectile.GetComponent<BadReward>();
            if (reward == null) reward = projectile.AddComponent<BadReward>();
            reward.Damage = _damage;
            reward.Owner = gameObject;
            reward.IsMine = mine;
            reward.Launch(launch, target, _arcHeight, _flightTime);
        }

        private IEnumerator DropBarrier(Vector3 position)
        {
            // Marker first, then the barrier rises out of the track.
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "BarrierMarker";
            marker.transform.position = new Vector3(position.x, position.y + 0.06f, 0f);
            marker.transform.localScale = new Vector3(5f, 0.02f, 5f);
            Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(new Color(1f, 0.5f, 0.1f), 2.2f);

            yield return new WaitForSeconds(0.8f);
            Destroy(marker);

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "VillainBarrier";
            bar.transform.position = new Vector3(position.x, position.y + 0.9f, 0f);
            bar.transform.localScale = new Vector3(1f, 0.4f, 5f);
            bar.GetComponent<Renderer>().sharedMaterial = VisualEntityFactory.CreateNeonMaterial(new Color(0.4f, 0.05f, 0.05f), new Color(1f, 0.25f, 0.1f), 2f);
            bar.AddComponent<SpinningBarComponent>().SpinSpeed = 120f;
            Destroy(bar, 8f);
        }

        private Vector3 GetFiringPosition()
        {
            // Launch from above the kart. The KartAnchors.FiringPoint is expressed in the cube's
            // non-uniform local space and ends up ~5 m in front of the kart, so it is not used.
            return transform.position + Vector3.up * 1.8f;
        }

        private GameObject SpawnProjectile(Vector3 position)
        {
            if (_projectilePrefab != null)
            {
                GameObject instance = Instantiate(_projectilePrefab, position, Quaternion.identity);
                PrepareProjectile(instance);
                return instance;
            }

            GameObject model = Resources.Load<GameObject>("NitroRhythm/Props/PrizeBad") ?? Resources.Load<GameObject>("NitroRhythm/Models/PremioMalo");
            GameObject projectile;
            if (model != null)
            {
                projectile = NitroRhythm.Core.ModelSanitizer.Strip(Instantiate(model, position, Quaternion.identity));
                projectile.name = "BadRewardProjectile";
                ModelNormalizer normalizer = projectile.AddComponent<ModelNormalizer>();
                normalizer.TargetSize = 1.4f;
                Material glow = NitroRhythm.World.DomainMaterials.ModelMaterial("PrizeBad", Color.white, 1.2f);
                foreach (Renderer r in projectile.GetComponentsInChildren<Renderer>())
                {
                    Material[] shared = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                    for (int i = 0; i < shared.Length; i++) shared[i] = glow;
                    r.sharedMaterials = shared;
                }
                SphereCollider sphere = projectile.AddComponent<SphereCollider>();
                sphere.radius = 0.6f;
            }
            else
            {
                projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectile.name = "BadRewardProjectile";
                projectile.transform.position = position;
                projectile.transform.localScale = Vector3.one * 0.9f;
                projectile.GetComponent<Renderer>().sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(new Color(1f, 0.3f, 0.1f), 2.2f);
            }

            PrepareProjectile(projectile);

            Light light = projectile.AddComponent<Light>();

            light.shadows = LightShadows.None;
            light.color = new Color(1f, 0.35f, 0.15f);
            light.intensity = 2.2f;
            light.range = 7f;
            return projectile;
        }

        private static void PrepareProjectile(GameObject instance)
        {
            Collider collider = instance.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            Rigidbody body = instance.GetComponent<Rigidbody>();
            if (body == null) body = instance.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        public void TakeDamage(float amount)
        {
            _health = Mathf.Max(0f, _health - amount);
            Debug.Log($"[{name}] Villain took {amount:F0} damage — HP {_health:F0}/{_maxHealth:F0}");

            if (_health <= 0f)
            {
                RespawnAtCheckpoint();
            }
        }

        /// <summary>Instantly respawns the villain at the current level checkpoint (full HP).</summary>
        public void RespawnAtCheckpoint()
        {
            if (_levelManager == null)
            {
                _levelManager = FindFirstObjectByType<LevelManager>();
            }

            Vector3 checkpoint = _levelManager != null ? _levelManager.GetPlayerSpawnPoint() : Vector3.zero;
            checkpoint.y = 1.2f;

            transform.position = checkpoint;
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            _health = _maxHealth;
            Debug.Log($"[{name}] Villain respawned at checkpoint {checkpoint}");
        }

        /// <summary>Moves the villain to the active level's start with a racing lead.</summary>
        public void RepositionToLevel(LevelManager levelManager, float leadDistance = 60f)
        {
            if (levelManager == null) return;

            float startX = levelManager.GetLevelStartX(levelManager.CurrentLevel);
            transform.position = new Vector3(startX + leadDistance, 1.2f, 0f);
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
            }

            _health = _maxHealth;
            Debug.Log($"[{name}] Villain repositioned to Level {levelManager.CurrentLevel} start +{leadDistance} ({(startX + leadDistance):F1}, 1.2, 0)");
        }
    }
}
