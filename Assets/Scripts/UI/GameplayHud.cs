using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Data;
using NitroRhythm.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Neon gameplay HUD built at runtime with Canvas + TextMeshPro:
    /// radial speedometer, level progress bar, pulsing score, player health and
    /// the co-op "waiting for the other player" message.
    /// </summary>
    public class GameplayHud : MonoBehaviour
    {
        private Canvas _canvas;
        private TextMeshProUGUI _scoreText;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _speedText;
        private TextMeshProUGUI _waitingText;
        private TextMeshProUGUI _healthText;
        private Image _speedGauge;
        private Image _progressFill;
        private Image _beatPulse;

        private float _score;
        private float _pulse;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            _canvas = UIFactory.CreateCanvas("HudCanvas", 10);
            _canvas.transform.SetParent(transform, false);

            // Top-left: score + level.
            _scoreText = UIFactory.CreateText(_canvas.transform, "Score", "PUNTUACIÓN 000000", 30f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(_scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(500f, 40f));

            _levelText = UIFactory.CreateText(_canvas.transform, "Level", "", 24f, UIFactory.NeonGold, TextAlignmentOptions.TopLeft);
            Anchor(_levelText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -64f), new Vector2(600f, 34f));

            // Top-center: waiting message.
            _waitingText = UIFactory.CreateText(_canvas.transform, "Waiting", "", 28f, UIFactory.NeonGold);
            Anchor(_waitingText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(700f, 40f));

            // Top-right: health.
            _healthText = UIFactory.CreateText(_canvas.transform, "Health", "", 22f, UIFactory.NeonCyan, TextAlignmentOptions.TopRight);
            Anchor(_healthText.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(400f, 100f));

            // Bottom-left: radial speedometer.
            _speedGauge = UIFactory.CreateRadialGauge(_canvas.transform, "SpeedGauge", UIFactory.NeonCyan, new Vector2(130f, 130f), new Vector2(220f, 220f));
            _speedText = UIFactory.CreateText(_canvas.transform, "Speed", "0 km/h", 26f, Color.white);
            Anchor(_speedText.rectTransform, new Vector2(0f, 0f), new Vector2(130f, 130f), new Vector2(200f, 40f));

            // Bottom-center: progress bar.
            Image progressBackground = UIFactory.CreatePanel(_canvas.transform, "ProgressBg", new Color(0.15f, 0.15f, 0.2f, 0.9f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            RectTransform progressRect = progressBackground.rectTransform;
            progressRect.sizeDelta = new Vector2(700f, 26f);
            progressRect.anchoredPosition = new Vector2(0f, 50f);

            _progressFill = UIFactory.CreatePanel(progressBackground.transform, "ProgressFill", UIFactory.NeonCyan,
                new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            RectTransform fillRect = _progressFill.rectTransform;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(0f, 0f);

            // Beat pulse marker reacting to live bass energy.
            _beatPulse = UIFactory.CreatePanel(_canvas.transform, "BeatPulse", new Color(1f, 1f, 1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            _beatPulse.rectTransform.sizeDelta = new Vector2(700f, 26f);
            _beatPulse.rectTransform.anchoredPosition = new Vector2(0f, 50f);
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void Update()
        {
            if (_canvas == null) return;

            float speed = 0f;
            float maxSpeed = 44f;

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot != null && boot.Player1 != null)
            {
                PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
                if (kart != null) speed = Mathf.Abs(kart.CurrentSpeed);
            }

            _score += Time.deltaTime * (10f + speed);
            _scoreText.text = $"PUNTUACIÓN {Mathf.FloorToInt(_score):D6}";

            if (GameSession.Instance != null) GameSession.Instance.LastScore = Mathf.FloorToInt(_score);

            _speedGauge.fillAmount = Mathf.Clamp01(speed / maxSpeed);
            _speedText.text = $"{Mathf.RoundToInt(speed * 3.6f)} km/h";

            UpdateLevelAndProgress();
            UpdateHealth();

            // Beat pulse from live bass.
            if (AudioReactiveMusicController.Instance != null)
            {
                float bass = AudioReactiveMusicController.Instance.Bass;
                _pulse = Mathf.Lerp(_pulse, bass, Time.deltaTime * 10f);
                _beatPulse.color = new Color(1f, 0.85f, 0.2f, _pulse * 0.35f);
            }
        }

        private void UpdateLevelAndProgress()
        {
            LevelManager lm = LevelManager.Instance;
            if (lm == null) return;

            LevelDefinition[] playable = PrototypeData.Instance.PlayableLevels;
            int idx = Mathf.Clamp(lm.CurrentLevel - 1, 0, Mathf.Max(0, playable.Length - 1));
            _levelText.text = playable.Length > 0
                ? $"DOMINIO {lm.CurrentLevel}/{lm.TotalLevels} — {playable[idx].displayName}"
                : $"DOMINIO {lm.CurrentLevel}/{lm.TotalLevels}";

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot != null && boot.Player1 != null)
            {
                float start = lm.GetLevelStartX(lm.CurrentLevel);
                float end = lm.GetLevelEndX(lm.CurrentLevel);
                float t = Mathf.InverseLerp(start, end, boot.Player1.transform.position.x);
                _progressFill.rectTransform.sizeDelta = new Vector2(700f * Mathf.Clamp01(t), 0f);
            }
        }

        private void UpdateHealth()
        {
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot == null || boot.Player1 == null) return;

            KartHealthSpeed health = boot.Player1.GetComponent<KartHealthSpeed>();
            if (health == null) return;

            _healthText.text = $"VIDA P1: {health.Health:0}/{health.MaxHealth:0}";
        }
    }
}
