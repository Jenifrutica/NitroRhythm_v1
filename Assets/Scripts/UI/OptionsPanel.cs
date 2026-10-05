using System.Collections;
using NitroRhythm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Options panel: music / SFX volume sliders and a rhythm-calibration slider
    /// (matching the SENA UI specification). Persists values into the GameSession and fades in/out.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private GameObject _root;
        private CanvasGroup _group;
        private Coroutine _fade;

        public void Build(Transform parent)
        {
            Image dim = UIFactory.CreatePanel(parent, "OptionsDim", new Color(0.01f, 0.01f, 0.04f, 0.82f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root = dim.gameObject;
            _group = _root.AddComponent<CanvasGroup>();

            Image panel = UIFactory.CreateCard(dim.transform, "OptionsPanel", new Color(0.04f, 0.05f, 0.12f, 0.96f), UIFactory.NeonCyan,
                new Vector2(820f, 600f), Vector2.zero);

            TextMeshProUGUI icon = UIFactory.CreateIcon(panel.transform, UIIcons.Settings, 46f, UIFactory.NeonCyan);
            icon.rectTransform.anchoredPosition = new Vector2(-170f, 232f);
            TextMeshProUGUI title = UIFactory.CreateTitle(panel.transform, "Title", "OPCIONES", 46f, Color.white, UIFactory.NeonCyan);
            title.rectTransform.anchoredPosition = new Vector2(30f, 232f);

            GameSession session = GameSession.EnsureExists();

            CreateLabeledSlider(panel.transform, UIIcons.Music, "VOLUMEN DE MÚSICA", 120f, session.MusicVolume, v =>
            {
                session.MusicVolume = v;
                if (Audio.AudioReactiveMusicController.Instance != null)
                {
                    Audio.AudioReactiveMusicController.Instance.SetVolume(v);
                }
            });

            CreateLabeledSlider(panel.transform, UIIcons.Volume, "VOLUMEN DE EFECTOS", 15f, session.SfxVolume, v => session.SfxVolume = v);
            CreateLabeledSlider(panel.transform, UIIcons.Tune, "CALIBRACIÓN DE AUDIO", -90f, 0.5f, v => session.AudioCalibrationMs = (v - 0.5f) * 200f);

            UIFactory.CreateButton(panel.transform, "REGRESAR", new Vector2(0f, -225f), new Vector2(380f, 76f), UIFactory.NeonCyan, Hide, UIIcons.Arrow);
        }

        private void CreateLabeledSlider(Transform parent, string glyph, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            TextMeshProUGUI icon = UIFactory.CreateIcon(parent, glyph, 34f, UIFactory.NeonGold);
            icon.rectTransform.anchoredPosition = new Vector2(-330f, y + 8f);

            TextMeshProUGUI text = UIFactory.CreateLabel(parent, $"Lbl_{label}", label, 26f, Color.white, TextAlignmentOptions.Left);
            text.rectTransform.anchoredPosition = new Vector2(-70f, y + 36f);
            text.rectTransform.sizeDelta = new Vector2(500f, 32f);

            TextMeshProUGUI percent = UIFactory.CreateTitle(parent, $"Pct_{label}", $"{Mathf.RoundToInt(value * 100f)}%", 24f, UIFactory.NeonCyan, UIFactory.NeonCyan, TextAlignmentOptions.Right);
            percent.rectTransform.anchoredPosition = new Vector2(310f, y + 36f);
            percent.rectTransform.sizeDelta = new Vector2(140f, 32f);

            UIFactory.CreateSlider(parent, $"Sld_{label}", new Vector2(10f, y - 6f), new Vector2(620f, 34f), value, v =>
            {
                percent.text = $"{Mathf.RoundToInt(v * 100f)}%";
                onChanged?.Invoke(v);
            });
        }

        public void Show()
        {
            _root.SetActive(true);
            StartFade(1f);
        }

        public void Hide()
        {
            if (!_root.activeSelf) return;
            StartFade(0f);
        }

        private void StartFade(float target)
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeRoutine(target));
        }

        private IEnumerator FadeRoutine(float target)
        {
            float start = _group.alpha;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, t / 0.18f);
                yield return null;
            }
            _group.alpha = target;
            if (target <= 0f) _root.SetActive(false);
        }
    }
}
