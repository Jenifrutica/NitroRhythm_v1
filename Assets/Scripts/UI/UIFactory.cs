using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>Material Icons glyphs (Apache 2.0) used across the interface.</summary>
    public static class UIIcons
    {
        public const string Speed = "";
        public const string Timer = "";
        public const string Flag = "";
        public const string Heart = "";
        public const string Bolt = "";
        public const string Fire = "";
        public const string Trophy = "";
        public const string Music = "";
        public const string Settings = "";
        public const string Play = "";
        public const string Pause = "";
        public const string Replay = "";
        public const string Home = "";
        public const string Person = "";
        public const string Group = "";
        public const string Star = "";
        public const string Check = "";
        public const string Close = "";
        public const string Volume = "";
        public const string Tune = "";
        public const string Car = "";
        public const string Shield = "";
        public const string Arrow = "";
        public const string Score = "";
        public const string Podium = "";
        public const string Equalizer = "";
        public const string Medal = "";
        public const string Broken = "";
        public const string Rocket = "";
        public const string Gamepad = "";
        public const string Queue = "";
        public const string Slow = "";
        public const string Enter = "";
    }

    /// <summary>
    /// Runtime factory for the Canvas + TextMeshPro interface. Everything is built in code (no prefabs):
    /// neon rounded frames with glow, capsule bars, rings, icon font, Orbitron/Rajdhani typography.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color NeonCyan = new Color(0.20f, 0.80f, 1f);
        public static readonly Color NeonBlue = new Color(0.0f, 0.13f, 0.70f);
        public static readonly Color NeonRed = new Color(1f, 0.15f, 0.2f);
        public static readonly Color NeonGold = new Color(1f, 0.78f, 0.15f);
        public static readonly Color NeonGreen = new Color(0.25f, 1f, 0.55f);
        public static readonly Color NeonPink = new Color(1f, 0.3f, 0.75f);
        public static readonly Color PanelDark = new Color(0.04f, 0.04f, 0.10f, 0.92f);
        public static readonly Color PanelTranslucent = new Color(0.06f, 0.07f, 0.14f, 0.82f);
        public static readonly Color TextSoft = new Color(0.82f, 0.88f, 1f);

        // ------------------------------------------------------------------ fonts

        private static TMP_FontAsset _font, _fontBold, _titleFont, _iconFont;
        private static readonly Dictionary<string, Material> GlowMaterials = new Dictionary<string, Material>();

        private static TMP_FontAsset LoadFont(string name)
        {
            return Resources.Load<TMP_FontAsset>($"NitroRhythm/Fonts/{name}");
        }

        /// <summary>Body font (Rajdhani SemiBold).</summary>
        public static TMP_FontAsset Font => _font != null ? _font : (_font = LoadFont("Rajdhani SDF") ?? FallbackFont());
        public static TMP_FontAsset FontBold => _fontBold != null ? _fontBold : (_fontBold = LoadFont("Rajdhani Bold SDF") ?? Font);
        /// <summary>Display font for titles and big numbers (Orbitron).</summary>
        public static TMP_FontAsset TitleFont => _titleFont != null ? _titleFont : (_titleFont = LoadFont("Orbitron SDF") ?? FontBold);
        public static TMP_FontAsset IconFont => _iconFont != null ? _iconFont : (_iconFont = LoadFont("MaterialIcons SDF"));

        private static TMP_FontAsset FallbackFont()
        {
            TMP_FontAsset f = TMP_Settings.defaultFontAsset;
            if (f == null)
            {
                Font osFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (osFont != null) f = TMP_FontAsset.CreateFontAsset(osFont);
            }
            return f;
        }

        // ----------------------------------------------------------------- canvas

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystemObject);
        }

        // ------------------------------------------------------------------ text

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject textObject = new GameObject(name, typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (Font != null) text.font = Font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 100f);
            return text;
        }

        /// <summary>Display text (Orbitron) with a soft coloured glow.</summary>
        public static TextMeshProUGUI CreateTitle(Transform parent, string name, string content, float fontSize, Color color, Color glow, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI text = CreateText(parent, name, content, fontSize, color, alignment);
            if (TitleFont != null) text.font = TitleFont;
            text.fontStyle = FontStyles.Bold;
            ApplyGlow(text, glow, 0.5f, 0.15f);
            return text;
        }

        /// <summary>Big numbers (score, timer, speed): Rajdhani Bold with a glow. Orbitron's slashed zero reads badly.</summary>
        public static TextMeshProUGUI CreateNumber(Transform parent, string name, string content, float fontSize, Color color, Color glow, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI text = CreateText(parent, name, content, fontSize, color, alignment);
            if (FontBold != null) text.font = FontBold;
            ApplyGlow(text, glow, 0.35f, 0.1f);
            return text;
        }

        /// <summary>Horizontal gradient overlay: opaque colour on the left fading to transparent on the right.</summary>
        public static Image CreateSideShade(Transform parent, Color color, float leftAlpha)
        {
            const int width = 128;
            Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                texture.SetPixel(x, 0, new Color(color.r, color.g, color.b, leftAlpha * (1f - Mathf.SmoothStep(0f, 1f, t))));
            }
            texture.Apply();
            Image image = CreatePanel(parent, "SideShade", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Bold body text (Rajdhani Bold), optionally with a glow.</summary>
        public static TextMeshProUGUI CreateLabel(Transform parent, string name, string content, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI text = CreateText(parent, name, content, fontSize, color, alignment);
            if (FontBold != null) text.font = FontBold;
            return text;
        }

        /// <summary>Icon (Material Icons glyph) as text, so it scales and tints like any label.</summary>
        public static TextMeshProUGUI CreateIcon(Transform parent, string glyph, float size, Color color)
        {
            TextMeshProUGUI icon = CreateText(parent, "Icon", glyph, size, color);
            if (IconFont != null) icon.font = IconFont;
            icon.rectTransform.sizeDelta = new Vector2(size * 1.3f, size * 1.3f);
            return icon;
        }

        /// <summary>Applies the glow/outline material variant of the text's font with the given colour.</summary>
        public static void ApplyGlow(TextMeshProUGUI text, Color glow, float power = 0.45f, float outline = 0.12f)
        {
            if (text == null || text.font == null) return;

            string key = text.font.name + " Glow";
            if (!GlowMaterials.TryGetValue(key, out Material glowBase) || glowBase == null)
            {
                glowBase = Resources.Load<Material>($"NitroRhythm/Fonts/{key}");
                GlowMaterials[key] = glowBase;
            }
            if (glowBase == null) return;

            text.fontSharedMaterial = glowBase;
            Material instance = text.fontMaterial;   // per-text instance, keeps the keywords
            instance.SetColor("_GlowColor", new Color(glow.r, glow.g, glow.b, 0.7f));
            instance.SetFloat("_GlowPower", power);
            instance.SetFloat("_OutlineWidth", outline);
            instance.SetColor("_OutlineColor", new Color(glow.r * 0.15f, glow.g * 0.15f, glow.b * 0.15f, 0.9f));
        }

        // ---------------------------------------------------------------- panels

        public static Image CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject panel = new GameObject(name, typeof(Image));
            panel.transform.SetParent(parent, false);

            Image image = panel.GetComponent<Image>();
            image.color = color;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return image;
        }

        /// <summary>Adds a sliced child image (frame, glow, ...) that stretches over the parent.</summary>
        private static Image AddSliced(Transform parent, string name, Sprite sprite, Color color, float grow, bool toBack)
        {
            GameObject go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-grow, -grow);
            rect.offsetMax = new Vector2(grow, grow);
            if (toBack) go.transform.SetAsFirstSibling();
            return image;
        }

        /// <summary>Rounded translucent card with a neon frame and halo (frame colour derived from the fill).</summary>
        public static Image CreateCard(Transform parent, string name, Color color, Vector2 size, Vector2 anchoredPosition)
        {
            Color frame = Color.Lerp(new Color(color.r, color.g, color.b, 1f), Color.white, 0.55f);
            return CreateCard(parent, name, color, frame, size, anchoredPosition);
        }

        public static Image CreateCard(Transform parent, string name, Color fill, Color accent, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject cardObject = new GameObject(name, typeof(Image));
            cardObject.transform.SetParent(parent, false);

            Image image = cardObject.GetComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            AddSliced(cardObject.transform, "Glow", GlowSprite, new Color(accent.r, accent.g, accent.b, 0.35f), 26f, true);
            AddSliced(cardObject.transform, "Frame", FrameSprite, new Color(accent.r, accent.g, accent.b, 0.95f), 0f, false);
            return image;
        }

        /// <summary>A real top→bottom gradient backdrop covering the whole canvas.</summary>
        public static Image CreateBackdrop(Transform parent, Color top, Color bottom)
        {
            const int height = 128;
            Texture2D texture = new Texture2D(1, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                texture.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            texture.Apply();

            Image image = CreatePanel(parent, "Backdrop", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f));
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        // --------------------------------------------------------------- buttons

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color accent, UnityEngine.Events.UnityAction onClick)
        {
            return CreateButton(parent, label, anchoredPosition, size, accent, onClick, null);
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color accent, UnityEngine.Events.UnityAction onClick, string icon)
        {
            GameObject buttonObject = new GameObject($"Btn_{label}", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 0.94f);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;   // UIHoverEffect handles feedback
            button.targetGraphic = image;

            Image glow = AddSliced(buttonObject.transform, "Glow", GlowSprite, new Color(accent.r, accent.g, accent.b, 0.28f), 24f, true);
            AddSliced(buttonObject.transform, "Frame", FrameSprite, new Color(accent.r, accent.g, accent.b, 1f), 0f, false);

            float textInset = 0f;
            if (!string.IsNullOrEmpty(icon) && IconFont != null)
            {
                TextMeshProUGUI iconText = CreateIcon(buttonObject.transform, icon, size.y * 0.5f, accent);
                iconText.rectTransform.anchorMin = iconText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                iconText.rectTransform.anchoredPosition = new Vector2(size.y * 0.55f + 14f, 0f);
                textInset = size.y * 0.5f;
            }

            TextMeshProUGUI text = CreateLabel(buttonObject.transform, "Label", label, Mathf.Clamp(size.y * 0.42f, 22f, 40f), Color.white);
            text.characterSpacing = 3f;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(textInset, 0f);
            textRect.offsetMax = Vector2.zero;

            if (onClick != null) button.onClick.AddListener(onClick);
            UIHoverEffect hover = buttonObject.AddComponent<UIHoverEffect>();
            hover.SetGlow(glow, 0.28f, 0.7f);
            return button;
        }

        // ----------------------------------------------------------------- slider

        public static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            GameObject sliderObject = new GameObject(name, typeof(Slider));
            sliderObject.transform.SetParent(parent, false);

            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            GameObject background = new GameObject("Background", typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = BarSprite;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = new Color(0.12f, 0.14f, 0.26f, 0.95f);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, 14f);

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(0f, 14f);

            GameObject fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = BarSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = NeonCyan;
            StretchRect(fill.GetComponent<RectTransform>());

            GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            StretchRect(handleArea.GetComponent<RectTransform>());

            GameObject handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = CircleSprite;
            handleImage.color = Color.white;
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 34f);
            AddGlowRing(handle.transform, NeonCyan);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        private static void AddGlowRing(Transform parent, Color color)
        {
            GameObject go = new GameObject("Halo", typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            Image img = go.GetComponent<Image>();
            img.sprite = CircleSprite;
            img.color = new Color(color.r, color.g, color.b, 0.35f);
            img.raycastTarget = false;
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(52f, 52f);
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------- bars

        /// <summary>A capsule progress bar whose fill is driven with <see cref="NeonBar.SetValue"/>.</summary>
        public static NeonBar CreateNeonBar(Transform parent, string name, Color color, Vector2 size, Vector2 anchoredPosition, bool centered = true)
        {
            GameObject root = new GameObject(name, typeof(Image));
            root.transform.SetParent(parent, false);
            Image back = root.GetComponent<Image>();
            back.sprite = BarSprite;
            back.type = Image.Type.Sliced;
            back.color = new Color(0.08f, 0.1f, 0.2f, 0.9f);
            back.raycastTarget = false;

            RectTransform rect = root.GetComponent<RectTransform>();
            Vector2 anchor = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            GameObject fillObject = new GameObject("Fill", typeof(Image));
            fillObject.transform.SetParent(root.transform, false);
            Image fill = fillObject.GetComponent<Image>();
            fill.sprite = BarSprite;
            fill.type = Image.Type.Sliced;
            fill.color = color;
            fill.raycastTarget = false;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            AddSliced(root.transform, "Frame", BarFrameSprite, new Color(color.r, color.g, color.b, 0.9f), 0f, false);

            NeonBar bar = root.AddComponent<NeonBar>();
            bar.Setup(fillRect, fill);
            return bar;
        }

        // ---------------------------------------------------------------- sprites

        private static Sprite _circleSprite, _squareSprite, _roundedSprite, _frameSprite, _glowSprite, _barSprite, _barFrameSprite, _ringSprite;

        /// <summary>Signed distance (px) from a point to a rounded rectangle centred in a w×h texture.</summary>
        private static float RoundedRectSdf(float x, float y, float w, float h, float halfW, float halfH, float radius)
        {
            float dx = Mathf.Abs(x - w * 0.5f) - (halfW - radius);
            float dy = Mathf.Abs(y - h * 0.5f) - (halfH - radius);
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return outside + inside - radius;
        }

        private static Sprite MakeSliced(int size, float border, System.Func<int, int, float> alphaAt, string name)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alphaAt(x, y)));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        /// <summary>Solid rounded rectangle (9-sliced).</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite != null) return _roundedSprite;
                const int n = 96; const float r = 26f;
                _roundedSprite = MakeSliced(n, 30f, (x, y) => 0.5f - RoundedRectSdf(x, y, n, n, n * 0.5f, n * 0.5f, r), "UI_Rounded");
                return _roundedSprite;
            }
        }

        /// <summary>Rounded rectangle outline (≈3 px), 9-sliced, for neon frames.</summary>
        public static Sprite FrameSprite
        {
            get
            {
                if (_frameSprite != null) return _frameSprite;
                const int n = 96; const float r = 26f;
                _frameSprite = MakeSliced(n, 30f, (x, y) =>
                {
                    float d = RoundedRectSdf(x, y, n, n, n * 0.5f, n * 0.5f, r);
                    return Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + 3.5f);
                }, "UI_Frame");
                return _frameSprite;
            }
        }

        /// <summary>Soft halo around a rounded rectangle (used behind frames).</summary>
        public static Sprite GlowSprite
        {
            get
            {
                if (_glowSprite != null) return _glowSprite;
                const int n = 128; const float r = 26f; const float inset = 30f;
                _glowSprite = MakeSliced(n, 60f, (x, y) =>
                {
                    float d = RoundedRectSdf(x, y, n, n, n * 0.5f - inset, n * 0.5f - inset, r);
                    return d <= 0f ? 0f : Mathf.Exp(-d * 0.11f);
                }, "UI_Glow");
                return _glowSprite;
            }
        }

        /// <summary>Capsule (fully rounded ends) used by bars and slider tracks.</summary>
        public static Sprite BarSprite
        {
            get
            {
                if (_barSprite != null) return _barSprite;
                const int n = 64; const float r = 31f;
                _barSprite = MakeSliced(n, 30f, (x, y) => 0.5f - RoundedRectSdf(x, y, n, n, n * 0.5f, n * 0.5f, r), "UI_Bar");
                return _barSprite;
            }
        }

        public static Sprite BarFrameSprite
        {
            get
            {
                if (_barFrameSprite != null) return _barFrameSprite;
                const int n = 64; const float r = 31f;
                _barFrameSprite = MakeSliced(n, 30f, (x, y) =>
                {
                    float d = RoundedRectSdf(x, y, n, n, n * 0.5f, n * 0.5f, r);
                    return Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + 3f);
                }, "UI_BarFrame");
                return _barFrameSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null) return _circleSprite;
                const int size = 128;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
                float radius = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((radius - Vector2.Distance(new Vector2(x, y), center)) / 1.5f)));
                texture.Apply();
                _circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _circleSprite;
            }
        }

        /// <summary>Thin ring (outer 100 %, inner 84 %): background and radial-filled arc of the speedometer.</summary>
        public static Sprite RingSprite
        {
            get
            {
                if (_ringSprite != null) return _ringSprite;
                const int size = 256;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                float c = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                        float outer = Mathf.Clamp01((1f - d) * c / 1.5f);
                        float inner = Mathf.Clamp01((d - 0.82f) * c / 1.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, outer * inner));
                    }
                }
                texture.Apply();
                _ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _ringSprite;
            }
        }

        public static Sprite SquareSprite
        {
            get
            {
                if (_squareSprite != null) return _squareSprite;
                Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                _squareSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                return _squareSprite;
            }
        }

        /// <summary>Radial-filled arc gauge (speedometer): a ring sprite filled clockwise from the top.</summary>
        public static Image CreateRadialGauge(Transform parent, string name, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject gaugeObject = new GameObject(name, typeof(Image));
            gaugeObject.transform.SetParent(parent, false);

            Image image = gaugeObject.GetComponent<Image>();
            image.sprite = RingSprite;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Bottom;
            image.fillClockwise = true;
            image.fillAmount = 0f;
            image.raycastTarget = false;

            RectTransform rect = gaugeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return image;
        }
    }

    /// <summary>Progress bar handle: set 0..1 and the fill resizes smoothly.</summary>
    public class NeonBar : MonoBehaviour
    {
        private RectTransform _fill;
        private Image _fillImage;
        private float _value;
        private float _shown;

        public void Setup(RectTransform fill, Image fillImage)
        {
            _fill = fill;
            _fillImage = fillImage;
        }

        public Image FillImage => _fillImage;

        public void SetValue(float value, bool instant = false)
        {
            _value = Mathf.Clamp01(value);
            if (instant) { _shown = _value; Apply(); }
        }

        public void SetColor(Color color)
        {
            if (_fillImage != null) _fillImage.color = color;
        }

        private void Update()
        {
            _shown = Mathf.Lerp(_shown, _value, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            Apply();
        }

        private void Apply()
        {
            if (_fill == null) return;
            _fill.anchorMax = new Vector2(Mathf.Max(0.001f, _shown), 1f);
        }
    }
}
