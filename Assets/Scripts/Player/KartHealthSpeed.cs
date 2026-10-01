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

        public float Health => _health;
        public float MaxHealth => _maxHealth;

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
        }

        private void Update()
        {
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

        public void SetFlashRect(Rect normalizedRect)
        {
            _flashRect = normalizedRect;
        }

        /// <summary>Applies a villain hit: health loss, speed slow and flash feedback.</summary>
        public void ApplyVillainHit(float damage)
        {
            float amount = damage <= 0f ? _hitDamage : damage;
            _health = Mathf.Max(0f, _health - amount);

            if (_kart != null)
            {
                _kart.SpeedMultiplier = _slowFactor;
            }
            _slowTimer = _slowDuration;

            _flashTimer = _flashDuration;
            _screenFlashAlpha = 0.55f;

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
