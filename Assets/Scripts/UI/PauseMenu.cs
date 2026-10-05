using System.Collections;
using NitroRhythm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>Pause pop-up (Escape) with Resume / Restart / Main Menu actions; fades in over the frozen game.</summary>
    public class PauseMenu : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private CanvasGroup _group;
        private bool _paused;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            _canvas = UIFactory.CreateCanvas("PauseCanvas", 100);
            _canvas.transform.SetParent(transform, false);

            Image dim = UIFactory.CreatePanel(_canvas.transform, "Dim", new Color(0.01f, 0.01f, 0.04f, 0.72f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _panel = dim.gameObject;
            _group = _panel.AddComponent<CanvasGroup>();

            GameMode mode = GameSession.EnsureExists().Mode;

            TextMeshProUGUI icon = UIFactory.CreateIcon(dim.transform, UIIcons.Pause, 60f, UIFactory.NeonGold);
            Place(icon.rectTransform, new Vector2(-170f, 470f), new Vector2(70f, 70f));
            TextMeshProUGUI title = UIFactory.CreateTitle(dim.transform, "Title", "PAUSA", 66f, Color.white, UIFactory.NeonGold);
            Place(title.rectTransform, new Vector2(30f, 470f), new Vector2(500f, 90f));

            TextMeshProUGUI modeLabel = UIFactory.CreateLabel(dim.transform, "Mode", $"CONTROLES  ·  {ControlsGuide.ModeTitle(mode)}", 32f, UIFactory.NeonGold);
            modeLabel.characterSpacing = 6f;
            Place(modeLabel.rectTransform, new Vector2(0f, 385f), new Vector2(1200f, 44f));

            RectTransform guide = ControlsGuide.Build(dim.transform, mode);
            guide.anchoredPosition = new Vector2(0f, 20f);

            UIFactory.CreateButton(dim.transform, "REANUDAR", new Vector2(-500f, -440f), new Vector2(440f, 80f), UIFactory.NeonCyan, Resume, UIIcons.Play);
            UIFactory.CreateButton(dim.transform, "REINICIAR", new Vector2(0f, -440f), new Vector2(440f, 80f), UIFactory.NeonGold, Restart, UIIcons.Replay);
            UIFactory.CreateButton(dim.transform, "MENÚ PRINCIPAL", new Vector2(500f, -440f), new Vector2(440f, 80f), UIFactory.NeonRed, GoToMenu, UIIcons.Home);

            TextMeshProUGUI hint = UIFactory.CreateLabel(dim.transform, "Hint", "ESC para continuar", 22f, UIFactory.TextSoft);
            Place(hint.rectTransform, new Vector2(0f, -510f), new Vector2(600f, 30f));

            _panel.SetActive(false);
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
            if (Input.GetKeyDown(KeyCode.Escape) && !ScreenFader.IsFading && !ControlsOverlay.IsOpen && !LevelIntroCard.AnyShowing)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            _paused = !_paused;
            if (_paused)
            {
                _panel.SetActive(true);
                StartCoroutine(FadeIn());
            }
            else
            {
                _panel.SetActive(false);
            }
            Time.timeScale = _paused ? 0f : 1f;
        }

        private IEnumerator FadeIn()
        {
            float t = 0f;
            _group.alpha = 0f;
            while (t < 0.15f)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = t / 0.15f;
                yield return null;
            }
            _group.alpha = 1f;
        }

        private void Resume()
        {
            _paused = false;
            _panel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneFlow.RestartLevel();
        }

        private void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneFlow.LoadMainMenu();
        }
    }
}
