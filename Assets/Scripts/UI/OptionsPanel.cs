using NitroRhythm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Options panel: music / SFX volume sliders and a rhythm-calibration slider
    /// (matching the SENA UI specification). Persists values into the GameSession.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private GameObject _root;

        public void Build(Transform parent)
        {
            Image dim = UIFactory.CreatePanel(parent, "OptionsDim", new Color(0f, 0f, 0f, 0.75f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root = dim.gameObject;

            Image panel = UIFactory.CreatePanel(dim.transform, "OptionsPanel", UIFactory.PanelDark,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.rectTransform.sizeDelta = new Vector2(720f, 520f);

            TextMeshProUGUI title = UIFactory.CreateText(panel.transform, "Title", "OPCIONES", 40f, UIFactory.NeonCyan);
            title.rectTransform.anchoredPosition = new Vector2(0f, 190f);

            GameSession session = GameSession.EnsureExists();

            CreateLabeledSlider(panel.transform, "Volumen música", 90f, session.MusicVolume, v =>
            {
                session.MusicVolume = v;
                if (Audio.AudioReactiveMusicController.Instance != null)
                {
                    Audio.AudioReactiveMusicController.Instance.SetVolume(v);
                }
            });

            CreateLabeledSlider(panel.transform, "Volumen efectos (SFX)", 0f, session.SfxVolume, v => session.SfxVolume = v);
            CreateLabeledSlider(panel.transform, "Calibración de audio", -90f, 0.5f, v => session.AudioCalibrationMs = (v - 0.5f) * 200f);

            UIFactory.CreateButton(panel.transform, "REGRESAR", new Vector2(0f, -180f), new Vector2(360f, 70f), UIFactory.NeonCyan, Hide);
        }

        private void CreateLabeledSlider(Transform parent, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            TextMeshProUGUI text = UIFactory.CreateText(parent, $"Lbl_{label}", label, 24f, Color.white, TextAlignmentOptions.Left);
            text.rectTransform.anchoredPosition = new Vector2(-120f, y + 40f);
            text.rectTransform.sizeDelta = new Vector2(420f, 30f);

            UIFactory.CreateSlider(parent, $"Sld_{label}", new Vector2(0f, y), new Vector2(420f, 24f), value, onChanged);
        }

        public void Show() => _root.SetActive(true);
        public void Hide() => _root.SetActive(false);
    }
}
