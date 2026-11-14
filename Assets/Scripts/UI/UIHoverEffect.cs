using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Neon hover/press feedback for UI buttons: scale-up, brighter fill, glowing halo and a
    /// small press squash, all driven by unscaled time so it also works while paused.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _pressScale = 0.97f;
        [SerializeField] private float _speed = 12f;

        private RectTransform _rect;
        private Image _image;
        private Color _baseColor;
        private Color _lastApplied;
        private bool _hovered;

        /// <summary>Selected state (e.g. the chosen game mode): brighter halo and slightly larger.</summary>
        public bool Highlighted { get; set; }
        private bool _pressed;

        private Image _glow;
        private float _glowBase = 0.28f;
        private float _glowHover = 0.7f;
        private Color _glowColor;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            if (_image != null) _baseColor = _lastApplied = _image.color;
        }

        /// <summary>Registers the halo image whose alpha follows the hover state.</summary>
        public void SetGlow(Image glow, float baseAlpha, float hoverAlpha)
        {
            _glow = glow;
            _glowBase = baseAlpha;
            _glowHover = hoverAlpha;
            if (glow != null) _glowColor = glow.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            NitroRhythm.Audio.SfxPlayer.Play(NitroRhythm.Audio.SfxLibrary.UiHover, 0.6f);
        }

        public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => _pressed = true;
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;

        private void Update()
        {
            if (_rect == null) return;

            float targetScale = _pressed ? _pressScale : (_hovered ? _hoverScale : (Highlighted ? 1.03f : 1f));
            float t = 1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime);
            _rect.localScale = Vector3.Lerp(_rect.localScale, Vector3.one * targetScale, t);

            if (_image != null)
            {
                // Someone else (e.g. a selection Refresh) recoloured the button: that is the new base.
                if (_image.color != _lastApplied) _baseColor = _image.color;
                Color target = _hovered
                    ? new Color(Mathf.Min(1f, _baseColor.r * 1.6f), Mathf.Min(1f, _baseColor.g * 1.6f), Mathf.Min(1f, _baseColor.b * 1.6f), _baseColor.a)
                    : _baseColor;
                _image.color = Color.Lerp(_image.color, target, t);
                _lastApplied = _image.color;
            }

            if (_glow != null)
            {
                float pulse = _hovered ? 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
                float alpha = (_hovered || Highlighted ? _glowHover : _glowBase) * pulse;
                Color c = _glow.color;
                c.a = Mathf.Lerp(c.a, alpha, t);
                _glow.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, c.a);
            }
        }
    }
}
