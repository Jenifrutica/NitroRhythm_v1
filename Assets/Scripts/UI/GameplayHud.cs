using System.Collections.Generic;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Data;
using NitroRhythm.Player;
using NitroRhythm.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Neon gameplay HUD built at runtime: domain + score card, race position, timer, health bars,
    /// ring speedometer with a beat pulse, progress bar with checkpoint markers, active power-up chips
    /// and checkpoint / impact toasts.
    /// </summary>
    public class GameplayHud : MonoBehaviour
    {
        private static readonly Color CardFill = new Color(0.03f, 0.04f, 0.1f, 0.74f);

        private Canvas _canvas;
        private Color _accent = UIFactory.NeonCyan;

        private TextMeshProUGUI _scoreText, _domainText, _domainName, _timerText, _positionText, _gapText, _speedText;
        private TextMeshProUGUI _waitingText, _checkpointToast, _impactToast, _impactSub;
        private TextMeshProUGUI _reviveP1, _reviveP2;
        private Image _speedRing, _beatRing, _beatLine;
        private NeonBar _p1Health, _p2Health, _progress, _turboBar, _slowBar;
        private TextMeshProUGUI _p1HealthText, _p2HealthText;
        private GameObject _p2Row, _turboChip, _slowChip;
        private RectTransform _progressTrack, _p1Marker, _p2Marker;
        private readonly List<Image> _checkpointTicks = new List<Image>();
        private bool _ticksBuilt;
        private float _turboMax = 1f;
        private const float SlowMax = 3f;

        private float _score, _pulse, _elapsed;
        private float _checkpointToastTimer, _impactToastTimer, _healthFlashTimer;
        private LevelManager _subscribedLevelManager;

        private const float CheckpointToastDuration = 1.8f;
        private const float ImpactToastDuration = 1f;
        private const float ProgressWidth = 980f;

        private void Start()
        {
            GameSession carried = GameSession.Instance;
            if (carried != null)
            {
                _score = carried.LastScore;
                _elapsed = carried.LastTime;
            }

            if (DomainTheme.Current != null) _accent = DomainTheme.Current.accent;
            Build();
            KartHealthSpeed.Hit += OnKartHit;
            SubscribeToLevelManager();
        }

        private void OnDestroy()
        {
            KartHealthSpeed.Hit -= OnKartHit;
            if (_subscribedLevelManager != null) _subscribedLevelManager.CheckpointReached -= OnCheckpointReached;
        }

        private void SubscribeToLevelManager()
        {
            if (_subscribedLevelManager != null || LevelManager.Instance == null) return;
            _subscribedLevelManager = LevelManager.Instance;
            _subscribedLevelManager.CheckpointReached += OnCheckpointReached;
        }

        private void OnCheckpointReached(Vector3 position)
        {
            _checkpointToast.text = "CHECKPOINT";
            _checkpointToastTimer = CheckpointToastDuration;
        }

        private void OnKartHit(KartHealthSpeed victim, float damage)
        {
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            bool isPlayer = boot != null && (victim.gameObject == boot.Player1 || victim.gameObject == boot.Player2);
            if (!isPlayer) return;

            string who = boot.Player2 != null ? (victim.gameObject == boot.Player1 ? "P1  " : "P2  ") : string.Empty;
            _impactToast.text = "IMPACTO!";
            _impactSub.text = $"{who}-{damage:0} VIDA";
            _impactToastTimer = ImpactToastDuration;
            _healthFlashTimer = 0.8f;
        }

        /// <summary>mm:ss.cc formatting used by the race timer and results.</summary>
        public static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float rest = seconds - minutes * 60f;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:D2}:{1:00.00}", minutes, rest);
        }

        /// <summary>1-based race position: 1 + how many opponents are further down the track.</summary>
        public static int ComputeRank(float playerX, params float[] otherXs)
        {
            int rank = 1;
            foreach (float x in otherXs)
            {
                if (x > playerX) rank++;
            }
            return rank;
        }

        // ------------------------------------------------------------------ build

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private Image Card(string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image card = UIFactory.CreateCard(_canvas.transform, name, CardFill, Color.Lerp(_accent, Color.white, 0.25f), size, Vector2.zero);
            Place(card.rectTransform, anchor, position, size);
            card.raycastTarget = false;
            return card;
        }

        private static TextMeshProUGUI Text(Transform parent, string content, float size, Color color, Vector2 pos, Vector2 box, TextAlignmentOptions align, bool title = false)
        {
            TextMeshProUGUI t = title
                ? UIFactory.CreateNumber(parent, "T", content, size * 1.18f, color, color, align)
                : UIFactory.CreateLabel(parent, "T", content, size, color, align);
            t.rectTransform.anchoredPosition = pos;
            t.rectTransform.sizeDelta = box;
            return t;
        }

        private static TextMeshProUGUI Icon(Transform parent, string glyph, float size, Color color, Vector2 pos)
        {
            TextMeshProUGUI icon = UIFactory.CreateIcon(parent, glyph, size, color);
            icon.rectTransform.anchoredPosition = pos;
            return icon;
        }

        private void Build()
        {
            _canvas = UIFactory.CreateCanvas("HudCanvas", 10);
            _canvas.transform.SetParent(transform, false);

            // Pulsing line along the very top edge (reacts to the bass).
            _beatLine = UIFactory.CreatePanel(_canvas.transform, "BeatLine", new Color(_accent.r, _accent.g, _accent.b, 0.2f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), Vector2.zero);
            _beatLine.raycastTarget = false;

            BuildTopLeft();
            BuildTopCenter();
            BuildTopRight();
            BuildSpeedometer();
            BuildProgress();
            BuildChips();
            BuildToasts();
        }

        private void BuildTopLeft()
        {
            Image domain = Card("DomainCard", new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(500f, 132f));
            _domainText = Text(domain.transform, "DOMINIO", 22f, UIFactory.NeonGold, new Vector2(0f, 42f), new Vector2(450f, 26f), TextAlignmentOptions.Left);
            _domainName = Text(domain.transform, "", 30f, Color.white, new Vector2(0f, 12f), new Vector2(450f, 36f), TextAlignmentOptions.Left, true);
            Icon(domain.transform, UIIcons.Star, 30f, UIFactory.NeonCyan, new Vector2(-214f, -34f));
            _scoreText = Text(domain.transform, "000000", 34f, UIFactory.NeonCyan, new Vector2(18f, -34f), new Vector2(380f, 40f), TextAlignmentOptions.Left, true);

            Image position = Card("PositionCard", new Vector2(0f, 1f), new Vector2(28f, -164f), new Vector2(500f, 76f));
            Icon(position.transform, UIIcons.Flag, 34f, UIFactory.NeonGold, new Vector2(-212f, 0f));
            _positionText = Text(position.transform, "POS 1/1", 32f, Color.white, new Vector2(-62f, 0f), new Vector2(170f, 40f), TextAlignmentOptions.Left, true);
            _gapText = Text(position.transform, "", 25f, UIFactory.NeonGold, new Vector2(112f, -2f), new Vector2(240f, 34f), TextAlignmentOptions.Left);
        }

        private void BuildTopCenter()
        {
            Image timer = Card("TimerCard", new Vector2(0.5f, 1f), new Vector2(-170f, -22f), new Vector2(340f, 82f));
            Icon(timer.transform, UIIcons.Timer, 34f, Color.white, new Vector2(-132f, 0f));
            _timerText = Text(timer.transform, "00:00.00", 40f, Color.white, new Vector2(20f, 0f), new Vector2(240f, 50f), TextAlignmentOptions.Center, true);

            _waitingText = UIFactory.CreateLabel(_canvas.transform, "Waiting", "", 30f, UIFactory.NeonGold);
            Place(_waitingText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(900f, 40f));
            _waitingText.rectTransform.pivot = new Vector2(0.5f, 1f);
        }

        private void BuildTopRight()
        {
            Image health = Card("HealthCard", new Vector2(1f, 1f), new Vector2(-28f, -22f), new Vector2(430f, 140f));
            BuildHealthRow(health.transform, "P1", 30f, out _p1Health, out _p1HealthText, UIFactory.NeonCyan, out _);
            BuildHealthRow(health.transform, "P2", -30f, out _p2Health, out _p2HealthText, UIFactory.NeonBlue, out _p2Row);
        }

        private void BuildHealthRow(Transform parent, string label, float y, out NeonBar bar, out TextMeshProUGUI value, Color tag, out GameObject row)
        {
            GameObject rowObject = new GameObject($"Row_{label}", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            RectTransform rect = rowObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400f, 50f);
            rect.anchoredPosition = new Vector2(0f, y);
            row = rowObject;

            Text(rowObject.transform, label, 24f, tag, new Vector2(-176f, 0f), new Vector2(50f, 30f), TextAlignmentOptions.Left, true);
            Icon(rowObject.transform, UIIcons.Heart, 28f, UIFactory.NeonRed, new Vector2(-124f, 0f));
            bar = UIFactory.CreateNeonBar(rowObject.transform, "Bar", UIFactory.NeonGreen, new Vector2(190f, 20f), new Vector2(20f, 0f));
            value = Text(rowObject.transform, "100", 26f, Color.white, new Vector2(158f, 0f), new Vector2(70f, 30f), TextAlignmentOptions.Right);
        }

        private void BuildSpeedometer()
        {
            GameObject beat = new GameObject("BeatRing", typeof(Image));
            beat.transform.SetParent(_canvas.transform, false);
            _beatRing = beat.GetComponent<Image>();
            _beatRing.sprite = UIFactory.RingSprite;
            _beatRing.color = new Color(_accent.r, _accent.g, _accent.b, 0.25f);
            _beatRing.raycastTarget = false;
            Place(_beatRing.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(330f, 330f));
            _beatRing.rectTransform.pivot = new Vector2(0f, 0f);

            Image disc = UIFactory.CreatePanel(_canvas.transform, "SpeedDisc", new Color(0.03f, 0.04f, 0.1f, 0.72f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            disc.sprite = UIFactory.CircleSprite;
            disc.raycastTarget = false;
            Place(disc.rectTransform, new Vector2(0f, 0f), new Vector2(60f, 60f), new Vector2(290f, 290f));

            Image back = UIFactory.CreateRadialGauge(_canvas.transform, "SpeedRingBack", new Color(0.12f, 0.16f, 0.3f, 0.9f), new Vector2(205f, 205f), new Vector2(290f, 290f));
            back.fillAmount = 1f;
            _speedRing = UIFactory.CreateRadialGauge(_canvas.transform, "SpeedRing", UIFactory.NeonCyan, new Vector2(205f, 205f), new Vector2(290f, 290f));

            _speedText = UIFactory.CreateNumber(_canvas.transform, "Speed", "0", 84f, Color.white, UIFactory.NeonCyan);
            Place(_speedText.rectTransform, new Vector2(0f, 0f), new Vector2(205f, 215f), new Vector2(240f, 80f));
            _speedText.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            TextMeshProUGUI unit = UIFactory.CreateLabel(_canvas.transform, "Unit", "KM/H", 24f, UIFactory.TextSoft);
            Place(unit.rectTransform, new Vector2(0f, 0f), new Vector2(205f, 160f), new Vector2(200f, 30f));
            unit.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private void BuildProgress()
        {
            NeonBar track = UIFactory.CreateNeonBar(_canvas.transform, "Progress", _accent, new Vector2(ProgressWidth, 22f), Vector2.zero);
            RectTransform rect = track.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 62f);
            _progress = track;
            _progressTrack = rect;

            _p2Marker = CreateMarker("P2Marker", UIFactory.NeonBlue, 18f);
            _p1Marker = CreateMarker("P1Marker", Color.white, 26f);

            TextMeshProUGUI flag = UIFactory.CreateIcon(_canvas.transform, UIIcons.Flag, 36f, UIFactory.NeonGold);
            flag.rectTransform.anchorMin = flag.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            flag.rectTransform.anchoredPosition = new Vector2(ProgressWidth * 0.5f + 44f, 62f);
        }

        private RectTransform CreateMarker(string name, Color color, float size)
        {
            GameObject go = new GameObject(name, typeof(Image));
            go.transform.SetParent(_progressTrack, false);
            Image img = go.GetComponent<Image>();
            img.sprite = UIFactory.CircleSprite;
            img.color = color;
            img.raycastTarget = false;
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(size, size);
            return r;
        }

        private void BuildChips()
        {
            _turboChip = BuildChip("TURBO", UIIcons.Bolt, UIFactory.NeonGold, 150f, out _turboBar);
            _slowChip = BuildChip("RALENTIZADO", UIIcons.Slow, UIFactory.NeonRed, 80f, out _slowBar);
        }

        private GameObject BuildChip(string label, string glyph, Color color, float y, out NeonBar bar)
        {
            Image chip = Card($"Chip_{label}", new Vector2(1f, 0f), new Vector2(-28f, y), new Vector2(360f, 62f));
            Icon(chip.transform, glyph, 32f, color, new Vector2(-148f, 0f));
            Text(chip.transform, label, 24f, color, new Vector2(10f, 11f), new Vector2(250f, 28f), TextAlignmentOptions.Left);
            bar = UIFactory.CreateNeonBar(chip.transform, "Bar", color, new Vector2(250f, 12f), new Vector2(10f, -14f));
            chip.gameObject.SetActive(false);
            return chip.gameObject;
        }

        private void BuildToasts()
        {
            _checkpointToast = UIFactory.CreateTitle(_canvas.transform, "CheckpointToast", "", 40f, UIFactory.NeonGreen, UIFactory.NeonGreen);
            Place(_checkpointToast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(700f, 56f));
            _checkpointToast.rectTransform.pivot = new Vector2(0.5f, 1f);
            _checkpointToast.alpha = 0f;

            _impactToast = UIFactory.CreateTitle(_canvas.transform, "ImpactToast", "", 78f, UIFactory.NeonRed, UIFactory.NeonRed);
            Place(_impactToast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1100f, 96f));
            _impactToast.alpha = 0f;

            _impactSub = UIFactory.CreateLabel(_canvas.transform, "ImpactSub", "", 42f, Color.white);
            Place(_impactSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(800f, 50f));
            _impactSub.alpha = 0f;

            // K.O. countdown, centred over each player's half of the screen.
            _reviveP1 = BuildReviveText("ReviveP1");
            _reviveP2 = BuildReviveText("ReviveP2");
        }

        private TextMeshProUGUI BuildReviveText(string name)
        {
            TextMeshProUGUI text = UIFactory.CreateTitle(_canvas.transform, name, "", 110f, Color.white, UIFactory.NeonRed);
            Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(760f, 260f));
            text.gameObject.SetActive(false);
            return text;
        }

        private void UpdateRevive(TextMeshProUGUI label, GameObject kart, float anchorX)
        {
            KartHealthSpeed health = kart != null ? kart.GetComponent<KartHealthSpeed>() : null;
            bool down = health != null && health.IsDown;
            if (label.gameObject.activeSelf != down) label.gameObject.SetActive(down);
            if (!down) return;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            label.text = $"<size=34%>REVIVIENDO EN</size>\n{Mathf.CeilToInt(health.ReviveTimeLeft)}";
        }

        // ----------------------------------------------------------------- update

        private void Update()
        {
            if (_canvas == null) return;

            float speed = 0f;
            const float maxSpeed = 44f;

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot != null && boot.Player1 != null)
            {
                PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
                if (kart != null) speed = Mathf.Abs(kart.CurrentSpeed);
            }

            _score += Time.deltaTime * (10f + speed);
            _scoreText.text = Mathf.FloorToInt(_score).ToString("D6");
            if (GameSession.Instance != null) GameSession.Instance.LastScore = Mathf.FloorToInt(_score);

            _speedRing.fillAmount = Mathf.Lerp(_speedRing.fillAmount, Mathf.Clamp01(speed / maxSpeed), 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            _speedText.text = Mathf.RoundToInt(speed * 3.6f).ToString();
            _speedRing.color = Color.Lerp(UIFactory.NeonCyan, UIFactory.NeonGold, Mathf.Clamp01((speed / maxSpeed - 0.6f) * 2.5f));

            _elapsed += Time.deltaTime;
            _timerText.text = FormatTime(_elapsed);
            if (GameSession.Instance != null) GameSession.Instance.LastTime = _elapsed;

            _waitingText.text = LevelGoalTrigger.CurrentWaitingMessage;

            SubscribeToLevelManager();
            UpdateLevelAndProgress();
            UpdateHealth();
            UpdatePosition();
            UpdatePowerUps();
            UpdateToasts();

            // Beat pulse from live bass.
            if (AudioReactiveMusicController.Instance != null)
            {
                float bass = AudioReactiveMusicController.Instance.Bass;
                _pulse = Mathf.Lerp(_pulse, bass, Time.deltaTime * 10f);
                _beatRing.rectTransform.localScale = Vector3.one * (1f + _pulse * 0.1f);
                _beatRing.color = new Color(_accent.r, _accent.g, _accent.b, 0.15f + _pulse * 0.5f);
                _beatLine.color = new Color(_accent.r, _accent.g, _accent.b, 0.15f + _pulse * 0.6f);
            }
        }

        private void UpdateLevelAndProgress()
        {
            LevelManager lm = LevelManager.Instance;
            if (lm == null) return;

            LevelDefinition[] playable = PrototypeData.Instance.PlayableLevels;
            int idx = Mathf.Clamp(lm.CurrentLevel - 1, 0, Mathf.Max(0, playable.Length - 1));
            _domainText.text = $"DOMINIO {lm.CurrentLevel} / {lm.TotalLevels}";
            _domainName.text = playable.Length > 0 ? playable[idx].displayName.ToUpperInvariant() : string.Empty;

            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot == null || boot.Player1 == null) return;

            float start = lm.GetLevelStartX(lm.CurrentLevel);
            float end = lm.GetLevelEndX(lm.CurrentLevel);
            float t = Mathf.Clamp01(Mathf.InverseLerp(start, end, boot.Player1.transform.position.x));
            _progress.SetValue(t);
            _p1Marker.anchoredPosition = new Vector2(Mathf.Lerp(8f, ProgressWidth - 8f, t), 0f);

            if (boot.Player2 != null)
            {
                float t2 = Mathf.Clamp01(Mathf.InverseLerp(start, end, boot.Player2.transform.position.x));
                _p2Marker.gameObject.SetActive(true);
                _p2Marker.anchoredPosition = new Vector2(Mathf.Lerp(8f, ProgressWidth - 8f, t2), 0f);
            }
            else
            {
                _p2Marker.gameObject.SetActive(false);
            }

            BuildOrRefreshCheckpointTicks(lm, start, end);
        }

        private void BuildOrRefreshCheckpointTicks(LevelManager lm, float start, float end)
        {
            if (!_ticksBuilt && lm.CheckpointXs.Count > 0)
            {
                foreach (float x in lm.CheckpointXs)
                {
                    GameObject tick = new GameObject("CheckpointTick", typeof(Image));
                    tick.transform.SetParent(_progressTrack, false);
                    tick.transform.SetSiblingIndex(2);
                    Image img = tick.GetComponent<Image>();
                    img.color = UIFactory.NeonGold;
                    img.raycastTarget = false;
                    RectTransform r = tick.GetComponent<RectTransform>();
                    r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
                    r.sizeDelta = new Vector2(5f, 36f);
                    float t = Mathf.Clamp01(Mathf.InverseLerp(start, end, x));
                    r.anchoredPosition = new Vector2(Mathf.Lerp(8f, ProgressWidth - 8f, t), 0f);
                    _checkpointTicks.Add(img);
                }
                _ticksBuilt = true;
            }

            for (int i = 0; i < _checkpointTicks.Count && i < lm.CheckpointXs.Count; i++)
            {
                bool reached = lm.CurrentCheckpoint.x >= lm.CheckpointXs[i] - 0.5f;
                _checkpointTicks[i].color = reached ? UIFactory.NeonGreen : UIFactory.NeonGold;
            }
        }

        private void UpdateHealth()
        {
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot == null || boot.Player1 == null) return;

            ApplyHealth(boot.Player1, _p1Health, _p1HealthText, true);

            _p2Row.SetActive(boot.Player2 != null);
            if (boot.Player2 != null) ApplyHealth(boot.Player2, _p2Health, _p2HealthText, false);

            _healthFlashTimer = Mathf.Max(0f, _healthFlashTimer - Time.deltaTime);
        }

        private void ApplyHealth(GameObject kart, NeonBar bar, TextMeshProUGUI text, bool flash)
        {
            KartHealthSpeed health = kart.GetComponent<KartHealthSpeed>();
            if (health == null) return;

            float fraction = health.Health / Mathf.Max(1f, health.MaxHealth);
            bar.SetValue(fraction);
            Color healthy = fraction > 0.5f ? UIFactory.NeonGreen : (fraction > 0.25f ? UIFactory.NeonGold : UIFactory.NeonRed);
            if (flash && _healthFlashTimer > 0f) healthy = Color.Lerp(healthy, Color.white, Mathf.Clamp01(_healthFlashTimer / 0.8f));
            bar.SetColor(healthy);
            text.text = Mathf.CeilToInt(health.Health).ToString();
        }

        private void UpdatePosition()
        {
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot == null || boot.Player1 == null) return;

            // Opponents on the track: the other hero and the villain (AI or human).
            List<float> others = new List<float>();
            GameObject villain = boot.Villain != null ? boot.Villain : boot.Player3;
            if (boot.Player2 != null) others.Add(boot.Player2.transform.position.x);
            if (villain != null) others.Add(villain.transform.position.x);

            if (others.Count == 0)
            {
                _positionText.text = string.Empty;
                _gapText.text = string.Empty;
                return;
            }

            float myX = boot.Player1.transform.position.x;
            int rank = ComputeRank(myX, others.ToArray());
            _positionText.text = $"POS {rank}/{others.Count + 1}";

            if (villain != null)
            {
                float gap = villain.transform.position.x - myX;
                _gapText.text = gap >= 0f ? $"VOX  +{gap:0} m" : $"VOX  -{-gap:0} m";
            }
        }

        private void UpdatePowerUps()
        {
            TestSceneBootstrapper boot = TestSceneBootstrapper.Instance;
            if (boot == null || boot.Player1 == null) return;

            PlayerKartController kart = boot.Player1.GetComponent<PlayerKartController>();
            bool boosting = kart != null && kart.IsBoosting;
            _turboChip.SetActive(boosting);
            if (boosting)
            {
                _turboMax = Mathf.Max(_turboMax, kart.BoostTimeLeft);
                _turboBar.SetValue(kart.BoostTimeLeft / _turboMax);
            }
            else
            {
                _turboMax = 1f;
            }

            KartHealthSpeed health = boot.Player1.GetComponent<KartHealthSpeed>();
            bool slowed = health != null && health.SlowTimeLeft > 0f;
            _slowChip.SetActive(slowed);
            if (slowed) _slowBar.SetValue(health.SlowTimeLeft / SlowMax);
        }

        private void UpdateToasts()
        {
            _checkpointToastTimer = Mathf.Max(0f, _checkpointToastTimer - Time.deltaTime);
            ApplyToast(_checkpointToast, _checkpointToastTimer, CheckpointToastDuration, 1.25f);

            TestSceneBootstrapper reviveBoot = TestSceneBootstrapper.Instance;
            if (reviveBoot != null)
            {
                bool split = reviveBoot.Player2 != null;
                UpdateRevive(_reviveP1, reviveBoot.Player1, split ? 0.25f : 0.5f);
                UpdateRevive(_reviveP2, reviveBoot.Player2, 0.75f);
            }

            _impactToastTimer = Mathf.Max(0f, _impactToastTimer - Time.deltaTime);
            ApplyToast(_impactToast, _impactToastTimer, ImpactToastDuration, 1.5f);
            ApplyToast(_impactSub, _impactToastTimer, ImpactToastDuration, 1.2f);
        }

        /// <summary>Pop-in (scale from popScale to 1 in 0.15 s) then fade out over the last 0.35 s.</summary>
        private static void ApplyToast(TextMeshProUGUI text, float remaining, float duration, float popScale)
        {
            if (remaining <= 0f)
            {
                text.alpha = 0f;
                return;
            }

            float age = duration - remaining;
            float pop = Mathf.Clamp01(age / 0.15f);
            text.rectTransform.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, pop);
            text.alpha = Mathf.Clamp01(remaining / 0.35f);
        }
    }
}
