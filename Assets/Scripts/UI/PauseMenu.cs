using NitroRhythm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>Pause pop-up (Escape) with Resume / Restart / Main Menu actions.</summary>
    public class PauseMenu : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private bool _paused;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            _canvas = UIFactory.CreateCanvas("PauseCanvas", 100);
            _canvas.transform.SetParent(transform, false);

            Image dim = UIFactory.CreatePanel(_canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.6f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image panel = UIFactory.CreatePanel(dim.transform, "Panel", UIFactory.PanelTranslucent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.rectTransform.sizeDelta = new Vector2(560f, 460f);
            _panel = dim.gameObject;

            TextMeshProUGUI title = UIFactory.CreateText(panel.transform, "Title", "PAUSA", 40f, UIFactory.NeonCyan);
            title.rectTransform.anchoredPosition = new Vector2(0f, 160f);

            UIFactory.CreateButton(panel.transform, "REANUDAR", new Vector2(0f, 60f), new Vector2(420f, 72f), UIFactory.NeonCyan, Resume);
            UIFactory.CreateButton(panel.transform, "REINICIAR", new Vector2(0f, -30f), new Vector2(420f, 72f), UIFactory.NeonGold, Restart);
            UIFactory.CreateButton(panel.transform, "MENÚ PRINCIPAL", new Vector2(0f, -120f), new Vector2(420f, 72f), UIFactory.NeonRed, GoToMenu);

            _panel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            _paused = !_paused;
            _panel.SetActive(_paused);
            Time.timeScale = _paused ? 0f : 1f;
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
            SceneFlow.LoadGameplay();
        }

        private void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneFlow.LoadMainMenu();
        }
    }
}
