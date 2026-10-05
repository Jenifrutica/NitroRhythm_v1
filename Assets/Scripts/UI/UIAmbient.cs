using NitroRhythm.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>Drifting neon dots behind a menu (cheap UI "particles" with parallax).</summary>
    public class UIAmbient : MonoBehaviour
    {
        private struct Dot
        {
            public RectTransform Rect;
            public Image Image;
            public float Speed;
            public float Sway;
            public float Phase;
            public float BaseAlpha;
        }

        private Dot[] _dots;
        private RectTransform _area;

        public static UIAmbient Create(Transform parent, Color colorA, Color colorB, int count = 46)
        {
            GameObject go = new GameObject("UIAmbient", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.transform.SetAsFirstSibling();

            UIAmbient ambient = go.AddComponent<UIAmbient>();
            ambient.Build(rect, colorA, colorB, count);
            return ambient;
        }

        private void Build(RectTransform area, Color a, Color b, int count)
        {
            _area = area;
            _dots = new Dot[count];
            for (int i = 0; i < count; i++)
            {
                GameObject d = new GameObject($"Dot{i}", typeof(Image));
                d.transform.SetParent(area, false);
                Image img = d.GetComponent<Image>();
                img.sprite = UIFactory.CircleSprite;
                img.raycastTarget = false;
                float depth = Random.value;
                float size = Mathf.Lerp(6f, 26f, depth * depth);
                Color c = Color.Lerp(a, b, Random.value);
                float alpha = Mathf.Lerp(0.15f, 0.65f, depth);
                img.color = new Color(c.r, c.g, c.b, alpha);

                RectTransform r = d.GetComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
                r.sizeDelta = new Vector2(size, size);
                r.anchoredPosition = new Vector2(Random.value * 1920f, Random.value * 1080f);

                _dots[i] = new Dot { Rect = r, Image = img, Speed = Mathf.Lerp(8f, 46f, depth), Sway = Random.Range(8f, 30f), Phase = Random.value * 6.28f, BaseAlpha = alpha };
            }
        }

        private void Update()
        {
            if (_dots == null) return;
            float bass = AudioReactiveMusicController.Instance != null ? AudioReactiveMusicController.Instance.Bass : 0f;
            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < _dots.Length; i++)
            {
                Vector2 p = _dots[i].Rect.anchoredPosition;
                p.y += _dots[i].Speed * dt * (1f + bass);
                p.x += Mathf.Sin(Time.unscaledTime * 0.6f + _dots[i].Phase) * _dots[i].Sway * dt;
                if (p.y > 1100f) { p.y = -20f; p.x = Random.value * 1920f; }
                _dots[i].Rect.anchoredPosition = p;

                Color c = _dots[i].Image.color;
                c.a = _dots[i].BaseAlpha * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 1.7f + _dots[i].Phase));
                _dots[i].Image.color = c;
            }
        }
    }

    /// <summary>Row of bars that dance to the spectrum of an AudioSource (or to noise when silent).</summary>
    public class UIEqualizer : MonoBehaviour
    {
        private RectTransform[] _bars;
        private float[] _levels;
        private readonly float[] _spectrum = new float[256];
        private AudioSource _source;

        public static UIEqualizer Create(Transform parent, AudioSource source, Color color, int count, Vector2 size, Vector2 anchoredPosition, Vector2 anchor)
        {
            GameObject go = new GameObject("UIEqualizer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            UIEqualizer eq = go.AddComponent<UIEqualizer>();
            eq._source = source;
            eq._bars = new RectTransform[count];
            eq._levels = new float[count];
            float barWidth = size.x / count;
            for (int i = 0; i < count; i++)
            {
                GameObject b = new GameObject($"Bar{i}", typeof(Image));
                b.transform.SetParent(go.transform, false);
                Image img = b.GetComponent<Image>();
                img.sprite = UIFactory.BarSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.35f, 0.9f, i / (float)count));
                img.raycastTarget = false;
                RectTransform r = b.GetComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
                r.pivot = new Vector2(0f, 0f);
                r.sizeDelta = new Vector2(barWidth * 0.62f, 6f);
                r.anchoredPosition = new Vector2(i * barWidth, 0f);
                eq._bars[i] = r;
            }
            return eq;
        }

        private void Update()
        {
            bool live = _source != null && _source.isPlaying;
            if (live) _source.GetSpectrumData(_spectrum, 0, FFTWindow.Blackman);

            float maxHeight = ((RectTransform)transform).sizeDelta.y;
            for (int i = 0; i < _bars.Length; i++)
            {
                float target;
                if (live)
                {
                    int bin = Mathf.Min(_spectrum.Length - 1, Mathf.RoundToInt(Mathf.Pow(i / (float)_bars.Length, 1.7f) * 90f));
                    target = Mathf.Clamp01(_spectrum[bin] * (8f + i * 0.9f));
                }
                else
                {
                    target = Mathf.PerlinNoise(i * 0.33f, Time.unscaledTime * 1.3f) * 0.55f + 0.08f;
                }

                _levels[i] = Mathf.Lerp(_levels[i], target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                _bars[i].sizeDelta = new Vector2(_bars[i].sizeDelta.x, 6f + _levels[i] * maxHeight);
            }
        }
    }
}
