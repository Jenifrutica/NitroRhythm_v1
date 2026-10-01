using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Smooth neon hover/press feedback for UI buttons: gentle scale-up plus an
    /// accent glow pulse, driven by unscaled time so it works while paused.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float _hoverScale = 1.06f;
        [SerializeField] private float _speed = 10f;

        private RectTransform _rect;
        private Image _image;
        private Color _baseColor;
        private bool _hovered;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            if (_image != null) _baseColor = _image.color;
        }

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;
        public void OnPointerExit(PointerEventData eventData) => _hovered = false;

        private void Update()
        {
            if (_rect == null) return;

            float targetScale = _hovered ? _hoverScale : 1f;
            float t = 1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime);
            _rect.localScale = Vector3.Lerp(_rect.localScale, Vector3.one * targetScale, t);

            if (_image != null)
            {
                Color target = _hovered
                    ? new Color(Mathf.Min(1f, _baseColor.r * 1.5f), Mathf.Min(1f, _baseColor.g * 1.5f), Mathf.Min(1f, _baseColor.b * 1.5f), _baseColor.a)
                    : _baseColor;
                _image.color = Color.Lerp(_image.color, target, t);
            }
        }
    }
}
