using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Title card shown at the start of every domain (each domain is its own screen):
    /// domain number, name, description and music hint over the domain's colours.
    /// The world is frozen while the card is visible, then it fades out.
    /// </summary>
    public class LevelIntroCard : MonoBehaviour
    {
        [SerializeField] private float _holdSeconds = 2.4f;
        [SerializeField] private float _fadeSeconds = 0.6f;

        private CanvasGroup _group;
        private float _timer;
        private bool _active;
        private System.Collections.Generic.List<RectTransform> _animated;
        private float _previousTimeScale = 1f;

        public bool IsShowing => _active;

        /// <summary>True while any level card is on screen (the pause menu stays closed meanwhile).</summary>
        public static bool AnyShowing { get; private set; }

        // First level of a run: after the card, the full controls guide is shown and the game waits for the player.
        private bool _guideAfterCard;

        private void Start()
        {
            GameSession session = GameSession.EnsureExists();
            LevelDefinition[] playable = PrototypeData.Instance.PlayableLevels;
            int index = Mathf.Clamp(session.CurrentLevelIndex - 1, 0, Mathf.Max(0, playable.Length - 1));
            LevelDefinition def = playable.Length > 0 ? playable[index] : null;
            Show(session.CurrentLevelIndex, playable.Length, def);
        }

        private RectTransform _countdownFill;

        public void Show(int number, int total, LevelDefinition def)
        {
            Canvas canvas = UIFactory.CreateCanvas("LevelIntroCanvas", 90);
            canvas.transform.SetParent(transform, false);
            _group = canvas.gameObject.AddComponent<CanvasGroup>();
            _animated = new System.Collections.Generic.List<RectTransform>();

            Color sky = def != null ? def.Sky : new Color(0.1f, 0.1f, 0.2f);
            Color accent = def != null ? def.Accent : UIFactory.NeonGold;

            Image backdrop = UIFactory.CreatePanel(canvas.transform, "Backdrop", Color.Lerp(new Color(0.008f, 0.008f, 0.03f), sky, 0.10f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = false;
            UIAmbient.Create(canvas.transform, accent, Color.Lerp(accent, Color.white, 0.5f), 26);

            // ---- right: the domain illustration in a rounded, framed card (aspect ratio kept)
            Sprite art = def != null ? (UIArt.RoundedDomain(def.id) ?? UIArt.Domain(def.id)) : null;
            // The frame follows the illustration's own proportions (no empty bands around it).
            float artAspect = art != null ? art.rect.width / art.rect.height : 1.6f;
            float cardHeight = Mathf.Clamp(820f / artAspect + 44f, 420f, 640f);
            Image card = UIFactory.CreateCard(canvas.transform, "ArtCard", new Color(0.03f, 0.04f, 0.1f, 0.95f), accent, new Vector2(864f, cardHeight), Vector2.zero);
            CenterPlace(card.rectTransform, new Vector2(400f, 30f), new Vector2(864f, cardHeight));
            card.raycastTarget = false;
            _animated.Add(card.rectTransform);
            if (art != null)
            {
                GameObject artObject = new GameObject("Art", typeof(Image));
                artObject.transform.SetParent(card.transform, false);
                Image artImage = artObject.GetComponent<Image>();
                artImage.sprite = art;
                artImage.preserveAspect = true;
                artImage.raycastTarget = false;
                RectTransform ar = artObject.GetComponent<RectTransform>();
                ar.anchorMin = Vector2.zero;
                ar.anchorMax = Vector2.one;
                ar.offsetMin = new Vector2(22f, 22f);
                ar.offsetMax = new Vector2(-22f, -22f);
            }

            // ---- left: number, name, rule, description, music chip
            TextMeshProUGUI label = UIFactory.CreateLabel(canvas.transform, "Number", $"DOMINIO {number} / {total}", 36f, accent, TextAlignmentOptions.Left);
            label.characterSpacing = 10f;
            CenterPlace(label.rectTransform, new Vector2(-910f, 205f), new Vector2(760f, 50f), 0f);
            _animated.Add(label.rectTransform);

            TextMeshProUGUI title = UIFactory.CreateTitle(canvas.transform, "Title", (def != null ? def.displayName : "DOMINIO").ToUpperInvariant(), 92f, Color.white, accent, TextAlignmentOptions.Left);
            title.enableWordWrapping = false;
            title.enableAutoSizing = true;
            title.fontSizeMin = 46f;
            title.fontSizeMax = 92f;
            CenterPlace(title.rectTransform, new Vector2(-910f, 118f), new Vector2(790f, 120f), 0f);
            _animated.Add(title.rectTransform);

            Image rule = UIFactory.CreatePanel(canvas.transform, "Rule", accent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            rule.sprite = UIFactory.BarSprite;
            rule.type = Image.Type.Sliced;
            rule.raycastTarget = false;
            CenterPlace(rule.rectTransform, new Vector2(-910f, 42f), new Vector2(240f, 8f), 0f);
            _animated.Add(rule.rectTransform);

            TextMeshProUGUI description = UIFactory.CreateText(canvas.transform, "Description", def != null ? def.description : string.Empty, 34f, new Color(0.9f, 0.93f, 1f), TextAlignmentOptions.TopLeft);
            description.enableWordWrapping = true;
            CenterPlace(description.rectTransform, new Vector2(-910f, -112f), new Vector2(780f, 190f), 0f);
            _animated.Add(description.rectTransform);

            if (def != null && !string.IsNullOrEmpty(def.musicHint))
            {
                Image chip = UIFactory.CreatePanel(canvas.transform, "MusicChip", new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.95f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                chip.sprite = UIFactory.RoundedSprite;
                chip.type = Image.Type.Sliced;
                chip.raycastTarget = false;
                CenterPlace(chip.rectTransform, new Vector2(-910f, -250f), new Vector2(740f, 70f), 0f);
                TextMeshProUGUI icon = UIFactory.CreateIcon(chip.transform, UIIcons.Music, 38f, accent);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(44f, 0f);
                TextMeshProUGUI hint = UIFactory.CreateLabel(chip.transform, "MusicHint", def.musicHint, 30f, Color.white, TextAlignmentOptions.Left);
                hint.rectTransform.anchorMin = Vector2.zero;
                hint.rectTransform.anchorMax = Vector2.one;
                hint.rectTransform.offsetMin = new Vector2(86f, 0f);
                hint.rectTransform.offsetMax = new Vector2(-16f, 0f);
                _animated.Add(chip.rectTransform);
            }

            // ---- bottom: progress segments, start prompt, controls, countdown
            float segment = 120f, gap = 16f;
            float startX = -(total * segment + (total - 1) * gap) * 0.5f;
            for (int i = 0; i < total; i++)
            {
                bool done = i < number - 1, current = i == number - 1;
                Image seg = UIFactory.CreatePanel(canvas.transform, $"Seg{i}", current ? accent : (done ? new Color(accent.r, accent.g, accent.b, 0.55f) : new Color(1f, 1f, 1f, 0.16f)),
                    Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                seg.sprite = UIFactory.BarSprite;
                seg.type = Image.Type.Sliced;
                seg.raycastTarget = false;
                CenterPlace(seg.rectTransform, new Vector2(startX + i * (segment + gap), -352f), new Vector2(segment, current ? 14f : 9f), 0f);
            }

            TextMeshProUGUI start = UIFactory.CreateLabel(canvas.transform, "Start", "ESPACIO PARA COMENZAR", 30f, Color.white, TextAlignmentOptions.Center);
            start.characterSpacing = 6f;
            CenterPlace(start.rectTransform, new Vector2(0f, -410f), new Vector2(900f, 44f), 0.5f);

            TextMeshProUGUI controls = UIFactory.CreateLabel(canvas.transform, "Controls",
                "P1  W A S D conducir  ·  ESPACIO saltar          P2  FLECHAS conducir  ·  SHIFT DER. saltar",
                22f, new Color(0.72f, 0.78f, 0.95f), TextAlignmentOptions.Center);
            controls.enableWordWrapping = false;
            CenterPlace(controls.rectTransform, new Vector2(0f, -462f), new Vector2(1500f, 32f), 0.5f);

            Image track = UIFactory.CreatePanel(canvas.transform, "CountdownTrack", new Color(1f, 1f, 1f, 0.12f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            track.sprite = UIFactory.BarSprite;
            track.type = Image.Type.Sliced;
            track.raycastTarget = false;
            CenterPlace(track.rectTransform, new Vector2(0f, -510f), new Vector2(520f, 6f), 0.5f);
            Image fill = UIFactory.CreatePanel(track.transform, "Fill", accent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.sprite = UIFactory.BarSprite;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            _countdownFill = fill.rectTransform;
            _countdownFill.pivot = new Vector2(0f, 0.5f);

            _guideAfterCard = number == 1 && !GameSession.EnsureExists().ControlsGuideShown;
            _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _timer = 0f;
            _active = true;
            AnyShowing = true;
            _group.alpha = 1f;
        }

        /// <summary>Places a rect relative to the screen centre; pivotX 0 = left-aligned, 0.5 = centred.</summary>
        private static void CenterPlace(RectTransform rect, Vector2 position, Vector2 size, float pivotX = 0.5f)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(pivotX, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void Update()
        {
            if (!_active) return;

            // A scene-load hitch (seconds long in the browser) must not use up the card's display time.
            _timer += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            bool skip = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
            if (skip && _timer > 0.4f) _timer = Mathf.Max(_timer, _holdSeconds);

            if (_timer >= _holdSeconds && Time.timeScale == 0f && !_guideAfterCard)
            {
                Time.timeScale = _previousTimeScale;
            }

            if (_animated != null)
            {
                for (int i = 0; i < _animated.Count; i++)
                {
                    if (_animated[i] == null) continue;
                    float k = Mathf.Clamp01((_timer - i * 0.08f) / 0.5f);
                    float eased = 1f - Mathf.Pow(1f - k, 3f);
                    Vector2 p = _animated[i].anchoredPosition;
                    _animated[i].anchoredPosition = new Vector2(Mathf.Lerp(p.x, p.x, 1f), p.y);
                    _animated[i].localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
                    CanvasGroup cg = _animated[i].GetComponent<CanvasGroup>();
                    if (cg == null) cg = _animated[i].gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = eased;
                }
            }

            if (_countdownFill != null) _countdownFill.anchorMax = new Vector2(Mathf.Clamp01(1f - _timer / _holdSeconds), 1f);

            float fade = Mathf.Clamp01((_timer - _holdSeconds) / Mathf.Max(0.01f, _fadeSeconds));
            _group.alpha = 1f - fade;

            if (fade >= 1f)
            {
                _active = false;
                AnyShowing = false;
                Destroy(_group.gameObject);
                if (_guideAfterCard)
                {
                    GameSession session = GameSession.EnsureExists();
                    session.ControlsGuideShown = true;
                    ControlsOverlay.Show(session.Mode, true);   // keeps the game frozen until the player closes it
                }
            }
        }

        private void OnDestroy()
        {
            AnyShowing = false;
            if (_active && Time.timeScale == 0f) Time.timeScale = _previousTimeScale;
        }
    }
}
