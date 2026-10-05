using NitroRhythm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Full-screen "how to play" panel for a game mode. Freezes the game while it is open
    /// (start of the first level) and closes with Space / Enter / Escape / click.
    /// </summary>
    public class ControlsOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private System.Action _onClose;
        private float _previousTimeScale = 1f;
        private float _openTime;
        private TextMeshProUGUI _hint;
        private bool _freezeTime;

        public static bool IsOpen { get; private set; }

        /// <summary>Opens the guide for <paramref name="mode"/>. With <paramref name="freezeTime"/> the game is paused meanwhile.</summary>
        public static ControlsOverlay Show(GameMode mode, bool freezeTime, System.Action onClose = null, string closeText = "ESPACIO PARA EMPEZAR")
        {
            GameObject go = new GameObject("ControlsOverlay");
            ControlsOverlay overlay = go.AddComponent<ControlsOverlay>();
            overlay.Open(mode, freezeTime, onClose, closeText);
            return overlay;
        }

        private void Open(GameMode mode, bool freezeTime, System.Action onClose, string closeText)
        {
            _onClose = onClose;
            _freezeTime = freezeTime;
            IsOpen = true;
            _openTime = Time.unscaledTime;

            _canvas = UIFactory.CreateCanvas("ControlsCanvas", 120);
            _canvas.transform.SetParent(transform, false);

            Image dim = UIFactory.CreatePanel(_canvas.transform, "Dim", new Color(0.01f, 0.01f, 0.04f, 0.94f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.raycastTarget = true;

            TextMeshProUGUI title = UIFactory.CreateTitle(_canvas.transform, "Title", "CÓMO JUGAR", 74f, Color.white, UIFactory.NeonCyan);
            Place(title.rectTransform, new Vector2(0f, 455f), new Vector2(1200f, 100f));

            TextMeshProUGUI mode1 = UIFactory.CreateLabel(_canvas.transform, "Mode", ControlsGuide.ModeTitle(mode), 36f, UIFactory.NeonGold);
            mode1.characterSpacing = 8f;
            Place(mode1.rectTransform, new Vector2(0f, 385f), new Vector2(1200f, 48f));
            TextMeshProUGUI desc = UIFactory.CreateLabel(_canvas.transform, "ModeDesc", ControlsGuide.ModeDescription(mode), 28f, UIFactory.TextSoft);
            Place(desc.rectTransform, new Vector2(0f, 340f), new Vector2(1500f, 40f));

            RectTransform guide = ControlsGuide.Build(_canvas.transform, mode);
            guide.anchoredPosition = new Vector2(0f, -40f);

            _hint = UIFactory.CreateLabel(_canvas.transform, "Close", closeText, 32f, Color.white);
            _hint.characterSpacing = 6f;
            Place(_hint.rectTransform, new Vector2(0f, -490f), new Vector2(1000f, 44f));

            if (freezeTime)
            {
                _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void Update()
        {
            if (_hint != null) _hint.alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.4f));

            bool close = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                         || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0);
            if (close && Time.unscaledTime - _openTime > 0.3f) Close();
        }

        public void Close()
        {
            if (_freezeTime && Time.timeScale == 0f) Time.timeScale = _previousTimeScale;
            IsOpen = false;
            System.Action callback = _onClose;
            _onClose = null;
            Destroy(gameObject);
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            IsOpen = false;
        }
    }
}
