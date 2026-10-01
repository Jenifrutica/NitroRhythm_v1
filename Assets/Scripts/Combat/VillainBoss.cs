using UnityEngine;
using NitroRhythm.Core;
using NitroRhythm.Player;

namespace NitroRhythm.Combat
{
    /// <summary>
    /// The Red Villain. In AI mode it continuously races forward along the track
    /// ahead of the players, firing BadReward parabolic projectiles backwards at
    /// the closest chasing player. In showdown mode it is player-controlled
    /// (IJKL to drive, Enter to fire). Falls into the void or losing all HP
    /// instantly respawns it at the current level checkpoint.
    /// </summary>
    public class VillainBoss : MonoBehaviour, IDamageable
    {
        [Header("Mode")]
        [SerializeField] private bool _playerControlled;

        [Header("AI Racing")]
        [SerializeField] private float _aiSpeed = 26f;
        [SerializeField] private float _levelEndBuffer = 6f;

        [Header("Combat")]
        [SerializeField] private float _detectionRange = 45f;
        [SerializeField] private float _fireInterval = 1.5f;
        [SerializeField] private float _damage = 15f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Audio-Reactive Cadence")]
        [SerializeField] private bool _useAudioCadence = true;
        [SerializeField] private float _arcHeight = 6f;

        private float _health;
        private float _fireTimer;
        private Rigidbody _rb;
        private LevelManager _levelManager;

        public bool IsPlayerControlled => _playerControlled;
        public float Health => _health;

        private void Start()
        {
            _health = _maxHealth;
            _rb = GetComponent<Rigidbody>();
            _levelManager = FindObjectOfType<LevelManager>();
            _fireTimer = _fireInterval; // warm-up so players are not shot instantly
        }

        private void Update()
        {
            if (_playerControlled)
            {
                HandlePlayerControl();
                return;
            }

            HandleAiDriving();
        }

        private void HandlePlayerControl()
        {
            PlayerKartController kart = GetComponent<PlayerKartController>();
            bool wantsToFire = kart != null ? kart.ConsumeFireInput() : Input.GetKeyDown(KeyCode.Return);

            if (wantsToFire)
            {
                PlayerKartController target = FindClosestPlayer();
                if (target != null)
                {
                    FireProjectile(target);
                }
            }
        }

        private void HandleAiDriving()
        {
            if (_levelManager == null)
            {
                _levelManager = FindObjectOfType<LevelManager>();
            }

            if (transform.position.y < -10f)
            {
                RespawnAtCheckpoint();
                return;
            }

            // Continuously race forward along the track (physics keeps gravity/falls).
            if (_rb != null)
            {
                _rb.linearVelocity = new Vector3(_aiSpeed, _rb.linearVelocity.y, _rb.linearVelocity.z);
            }
            else
            {
                transform.position += Vector3.right * (_aiSpeed * Time.deltaTime);
            }

            // Wait near the level-end platform so players can catch up.
            if (_levelManager != null)
            {
                float levelEnd = _levelManager.GetLevelEndX(_levelManager.CurrentLevel) - _levelEndBuffer;
                if (transform.position.x > levelEnd)
                {
                    transform.position = new Vector3(levelEnd, transform.position.y, transform.position.z);
                }
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                PlayerKartController target = FindClosestPlayer();
                if (target != null && Vector3.Distance(transform.position, target.transform.position) <= _detectionRange)
                {
                    FireProjectile(target);
                }
                _fireTimer = CurrentFireInterval();
            }
        }

        /// <summary>
        /// Fire cadence driven by the music: two shots per beat at the analysed
        /// BPM, or faster live when the high band is loud.
        /// </summary>
        private float CurrentFireInterval()
        {
            if (_useAudioCadence && Audio.AudioReactiveMusicController.Instance != null)
            {
                Audio.AudioReactiveMusicController audio = Audio.AudioReactiveMusicController.Instance;
                if (audio.IsReady && audio.Analysis != null && audio.Analysis.IsValid)
                {
                    return Mathf.Clamp(audio.Analysis.SecondsPerBeat * 2f, 0.5f, 3f);
                }
                return Mathf.Lerp(_fireInterval, _fireInterval * 0.5f, audio.Treble);
            }
            return _fireInterval;
        }

        public void SetPlayerControlled(bool playerControlled)
        {
            _playerControlled = playerControlled;
        }

        private PlayerKartController FindClosestPlayer()
        {
            PlayerKartController[] players = FindObjectsOfType<PlayerKartController>();
            PlayerKartController closest = null;
            float minDistance = float.MaxValue;

            foreach (PlayerKartController player in players)
            {
                if (player == GetComponent<PlayerKartController>()) continue; // never target self

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = player;
                }
            }

            return closest;
        }

        private void FireProjectile(PlayerKartController target)
        {
            Vector3 launchPosition = GetFiringPosition();

            GameObject projectile = SpawnProjectile(launchPosition);

            BadReward reward = projectile.GetComponent<BadReward>();
            if (reward == null)
            {
                reward = projectile.AddComponent<BadReward>();
            }
            reward.Damage = _damage;
            reward.Owner = gameObject;
            reward.Launch(launchPosition, target.transform.position + Vector3.up * 0.5f, _arcHeight);

            Debug.Log($"[{name}] Firing BadReward at {target.name} ({_damage} dmg)");
        }

        private Vector3 GetFiringPosition()
        {
            KartAnchors anchors = GetComponent<KartAnchors>();
            if (anchors != null && anchors.FiringPoint != null)
            {
                return anchors.FiringPoint.position;
            }

            return transform.position + Vector3.up * 0.8f + transform.forward * 2f;
        }

        private GameObject SpawnProjectile(Vector3 position)
        {
            if (_projectilePrefab != null)
            {
                GameObject instance = Instantiate(_projectilePrefab, position, Quaternion.identity);
                Collider collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
                if (instance.GetComponent<Rigidbody>() == null)
                {
                    Rigidbody prefabBody = instance.AddComponent<Rigidbody>();
                    prefabBody.isKinematic = true;
                    prefabBody.useGravity = false;
                }
                return instance;
            }

            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectile.name = "BadRewardProjectile";
            projectile.transform.position = position;
            projectile.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
            {
                projectileCollider.isTrigger = true;
            }

            Rigidbody projectileBody = projectile.AddComponent<Rigidbody>();
            projectileBody.isKinematic = true;
            projectileBody.useGravity = false;

            Renderer renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(new Color(1f, 0.3f, 0.1f));
            }

            return projectile;
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
                _levelManager = FindObjectOfType<LevelManager>();
            }

            Vector3 checkpoint = _levelManager != null ? _levelManager.GetPlayerSpawnPoint() : Vector3.zero;
            checkpoint.y = 1.2f;

            transform.position = checkpoint;
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            if (_rb != null)
            {
                _rb.linearVelocity = new Vector3(_aiSpeed, 0f, 0f);
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
                _rb.linearVelocity = new Vector3(_aiSpeed, 0f, 0f);
            }

            _health = _maxHealth;
            Debug.Log($"[{name}] Villain repositioned to Level {levelManager.CurrentLevel} start +{leadDistance} ({(startX + leadDistance):F1}, 1.2, 0)");
        }
    }
}
