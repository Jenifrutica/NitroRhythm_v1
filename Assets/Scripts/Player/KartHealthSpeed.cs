using System;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.Player
{
    /// <summary>
    /// Health and debuff handler for player karts. When hit by the villain's
    /// BadReward projectile the kart loses 15% health, gets a -40% speed slow
    /// for 3 seconds and flashes white with a red screen flash over its camera
    /// viewport rect (split-screen aware).
    /// </summary>
    public class KartHealthSpeed : MonoBehaviour
    {
        [Header("Health & Debuff")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _hitDamage = 15f;
        [SerializeField] private float _slowFactor = 0.6f;  // -40% speed
        [SerializeField] private float _slowDuration = 3f;

        [Header("Feedback")]
        [SerializeField] private float _flashDuration = 0.35f;
        [SerializeField] private Rect _flashRect = new Rect(0f, 0f, 1f, 1f);

        private PlayerKartController _kart;
        private Material _material;
        private Color _baseColor;
        private float _health;
        private float _slowTimer;
        private float _flashTimer;
        private float _screenFlashAlpha;
        private Material[] _modelMaterials = new Material[0];
        private Color[] _modelEmission = new Color[0];

        /// <summary>Raised on every villain hit: (victim, damage dealt). Used by the HUD.</summary>
        public static event Action<KartHealthSpeed, float> Hit;

        public float Health => _health;
        public float SlowTimeLeft => _slowTimer;
        public float SlowFactor => _slowFactor;
        public float MaxHealth => _maxHealth;

        /// <summary>Seconds a K.O. kart waits before it is revived at the checkpoint.</summary>
        public const float ReviveDelay = 5f;
        private float _downTimer;
        private Renderer[] _blinkRenderers = new Renderer[0];

        /// <summary>True while the kart is K.O. and counting down to its revival.</summary>
        public bool IsDown => _downTimer > 0f;
        public float ReviveTimeLeft => Mathf.Max(0f, _downTimer);

        private void Awake()
        {
            _health = _maxHealth;
            _kart = GetComponent<PlayerKartController>();

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                _material = renderer.material;
                _baseColor = _material.color;
            }

            // The visible kart is the FBX child (the cube renderer is hidden): flash its materials too.
            System.Collections.Generic.List<Material> mats = new System.Collections.Generic.List<Material>();
            System.Collections.Generic.List<Color> emissions = new System.Collections.Generic.List<Color>();
            foreach (Renderer child in GetComponentsInChildren<Renderer>())
            {
                if (child == renderer) continue;
                foreach (Material m in child.materials)
                {
                    if (!m.HasProperty("_EmissionColor")) continue;
                    mats.Add(m);
                    emissions.Add(m.GetColor("_EmissionColor"));
                }
            }
            _modelMaterials = mats.ToArray();
            _modelEmission = emissions.ToArray();
        }

        private void SetModelFlash(float blend)
        {
            for (int i = 0; i < _modelMaterials.Length; i++)
            {
                if (_modelMaterials[i] == null) continue;
                _modelMaterials[i].EnableKeyword("_EMISSION");
                _modelMaterials[i].SetColor("_EmissionColor", Color.Lerp(_modelEmission[i], new Color(1.6f, 0.35f, 0.3f), blend));
            }
        }

        private void Update()
        {
            if (_downTimer > 0f) TickDown();

            if (_slowTimer > 0f)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0f && _kart != null)
                {
                    _kart.SpeedMultiplier = 1f;
                }
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                SetModelFlash(Mathf.Clamp01(_flashTimer / _flashDuration));
                if (_material != null)
                {
                    float blend = Mathf.Clamp01(_flashTimer / _flashDuration);
                    _material.color = Color.Lerp(_baseColor, Color.white, blend);
                }

                if (_flashTimer <= 0f && _material != null)
                {
                    _material.color = _baseColor;
                }
            }

            _screenFlashAlpha = Mathf.MoveTowards(_screenFlashAlpha, 0f, Time.deltaTime * 1.6f);
        }

        /// <summary>Raised when a kart runs out of health and is sent back to the last checkpoint.</summary>
        public static event Action<KartHealthSpeed> KnockedOut;

        /// <summary>K.O.: back to the last checkpoint with full health (keeps the slow-down as a penalty).</summary>
        private void KnockOut()
        {
            // Down for ReviveDelay seconds: the kart stops and blinks, then Revive() puts it back on track.
            System.Collections.Generic.List<Renderer> visible = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) if (r.enabled) visible.Add(r);
            _blinkRenderers = visible.ToArray();
            _downTimer = ReviveDelay;
            if (_kart != null)
            {
                _kart.ControlLocked = true;
                _kart.SpeedMultiplier = 1f;
            }
            _slowTimer = 0f;
            KnockedOut?.Invoke(this);
            Debug.Log($"[{name}] K.O. — reviving in {ReviveDelay:0} s.");
        }

        private void TickDown()
        {
            _downTimer -= Time.deltaTime;
            bool show = _downTimer <= 0f || Mathf.FloorToInt(_downTimer * 6f) % 2 == 0;
            foreach (Renderer r in _blinkRenderers) if (r != null) r.enabled = show;
            if (_downTimer <= 0f) Revive();
        }

        private void Revive()
        {
            _downTimer = 0f;
            foreach (Renderer r in _blinkRenderers) if (r != null) r.enabled = true;
            LevelManager lm = LevelManager.Instance;
            Vector3 spawn = lm != null ? lm.GetCheckpointSpawn(_kart) : transform.position;
            spawn.y = 1.2f;

            Quaternion facing = Quaternion.Euler(0f, 90f, 0f);
            transform.SetPositionAndRotation(spawn, facing);
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                // Move the rigidbody itself: with interpolation on, a bare transform move is overwritten by the next physics step.
                body.position = spawn;
                body.rotation = facing;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();

            _health = _maxHealth;
            if (_kart != null) _kart.ControlLocked = false;
            Debug.Log($"[{name}] Revived at the checkpoint {spawn} (lm={(lm != null)}, pos now {transform.position}).");
        }

        public void SetFlashRect(Rect normalizedRect)
        {
            _flashRect = normalizedRect;
        }

        /// <summary>Applies a villain hit: health loss, speed slow and flash feedback.</summary>
        public void ApplyVillainHit(float damage)
        {
            if (IsDown) return;   // already K.O.
            float amount = damage <= 0f ? _hitDamage : damage;
            _health = Mathf.Max(0f, _health - amount);

            if (_kart != null)
            {
                _kart.SpeedMultiplier = _slowFactor;
            }
            _slowTimer = _slowDuration;

            _flashTimer = _flashDuration;
            _screenFlashAlpha = 0.7f;

            SfxPlayer.Play(SfxLibrary.Impact, 1f, UnityEngine.Random.Range(0.92f, 1.05f));
            foreach (CameraFollow follow in FindObjectsByType<CameraFollow>(FindObjectsSortMode.None))
            {
                if (follow.Target == transform) follow.Shake(0.45f, 0.4f);
            }
            Hit?.Invoke(this, amount);

            if (_health <= 0f) KnockOut();

            Debug.Log($"[{name}] Villain hit: -{amount:F0} HP ({_health:F0}/{_maxHealth:F0}), slowed -40% for {_slowDuration}s");
        }

        private void OnGUI()
        {
            if (_screenFlashAlpha <= 0.01f) return;

            Rect flashRect = new Rect(
                _flashRect.x * Screen.width,
                _flashRect.y * Screen.height,
                _flashRect.width * Screen.width,
                _flashRect.height * Screen.height);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0f, 0f, _screenFlashAlpha);
            GUI.DrawTexture(flashRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }
    }
}
