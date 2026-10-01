using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Runtime factory for the Canvas + TextMeshPro interface required by the
    /// SENA UI specification. Creates Screenspace canvases, translucent panels,
    /// neon buttons, sliders and text without depending on authored prefabs, so
    /// the same code drives the menu, character select, HUD and results scenes.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color NeonCyan = new Color(0.20f, 0.80f, 1f);
        public static readonly Color NeonBlue = new Color(0.0f, 0.13f, 0.70f);
        public static readonly Color NeonRed = new Color(1f, 0f, 0f);
        public static readonly Color NeonGold = new Color(1f, 0.78f, 0.15f);
        public static readonly Color PanelDark = new Color(0.04f, 0.04f, 0.10f, 0.92f);
        public static readonly Color PanelTranslucent = new Color(0.06f, 0.07f, 0.14f, 0.82f);

        private static TMP_FontAsset _font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;

                _font = TMP_Settings.defaultFontAsset;
                if (_font == null)
                {
                    Font osFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (osFont == null) osFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (osFont != null)
                    {
                        _font = TMP_FontAsset.CreateFontAsset(osFont);
                    }
                }

                return _font;
            }
        }

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

            // Allow this overlay to render over each split-screen camera viewport.
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystemObject);
        }

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

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject textObject = new GameObject(name, typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            if (Font != null) text.font = Font;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 100f);

            return text;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color accent, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject($"Btn_{label}", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 0.92f);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            // Accent border.
            GameObject border = new GameObject("Border", typeof(Image));
            border.transform.SetParent(buttonObject.transform, false);
            Image borderImage = border.GetComponent<Image>();
            borderImage.color = accent;
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3f, -3f);
            borderRect.offsetMax = new Vector2(3f, 3f);
            border.transform.SetAsFirstSibling();

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 26f, Color.white);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            if (onClick != null) button.onClick.AddListener(onClick);
            buttonObject.AddComponent<UIHoverEffect>();
            return button;
        }

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
            backgroundImage.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            StretchRect(background.GetComponent<RectTransform>(), 0f, 6f);

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            StretchRect(fillArea.GetComponent<RectTransform>(), 0f, 0f);

            GameObject fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = NeonCyan;
            StretchRect(fill.GetComponent<RectTransform>(), 0f, 0f);

            GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            StretchRect(handleArea.GetComponent<RectTransform>(), 0f, 0f);

            GameObject handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = Color.white;
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 34f);

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

        private static void StretchRect(RectTransform rect, float padding, float fixedHeight)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static Sprite _circleSprite;
        private static Sprite _squareSprite;
        private static Sprite _roundedSprite;
        private static Sprite _gradientSprite;

        /// <summary>Rounded-rectangle sprite used for cards and buttons (9-sliced).</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite != null) return _roundedSprite;

                const int size = 64;
                const float radius = 18f;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                        float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                _roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(22f, 22f, 22f, 22f));
                return _roundedSprite;
            }
        }

        /// <summary>Vertical gradient sprite (white → 35% grey) used for backdrops.</summary>
        public static Sprite GradientSprite
        {
            get
            {
                if (_gradientSprite != null) return _gradientSprite;

                const int height = 64;
                Texture2D texture = new Texture2D(1, height, TextureFormat.RGBA32, false);
                for (int y = 0; y < height; y++)
                {
                    float t = y / (float)(height - 1);
                    float value = Mathf.Lerp(0.32f, 1f, t);
                    texture.SetPixel(0, y, new Color(value, value, value, 1f));
                }
                texture.Apply();
                _gradientSprite = Sprite.Create(texture, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f));
                return _gradientSprite;
            }
        }

        /// <summary>Full-screen themed gradient backdrop.</summary>
        public static Image CreateBackdrop(Transform parent, Color top, Color bottom)
        {
            Image image = CreatePanel(parent, "Backdrop", top, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            image.sprite = GradientSprite;
            image.type = Image.Type.Simple;
            // Two overlapping images fake a two-colour gradient: keep it simple and
            // tint the gradient with the average colour.
            image.color = Color.Lerp(bottom, top, 0.5f);
            return image;
        }

        /// <summary>Rounded translucent card.</summary>
        public static Image CreateCard(Transform parent, string name, Color color, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject cardObject = new GameObject(name, typeof(Image));
            cardObject.transform.SetParent(parent, false);

            Image image = cardObject.GetComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            return image;
        }

        /// <summary>Soft-edged circle sprite used by the radial speed gauge.</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null) return _circleSprite;

                const int size = 128;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
                float radius = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        float alpha = Mathf.Clamp01((radius - distance) / 2f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                _circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _circleSprite;
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

        /// <summary>Factory for a radial-filled gauge image (velocímetro).</summary>
        public static Image CreateRadialGauge(Transform parent, string name, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject gaugeObject = new GameObject(name, typeof(Image));
            gaugeObject.transform.SetParent(parent, false);

            Image image = gaugeObject.GetComponent<Image>();
            image.sprite = CircleSprite;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillAmount = 0f;

            RectTransform rect = gaugeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            return image;
        }
    }
}
