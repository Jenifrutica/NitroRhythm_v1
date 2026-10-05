using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Persistent full-screen fade used between scenes (fade to black, run the action, fade back in).
    /// Created on first use and kept across scene loads.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader _instance;
        private Image _overlay;
        private float _alpha;

        public static bool IsFading { get; private set; }

        private static ScreenFader Instance
        {
            get
            {
                if (_instance != null) return _instance;
                GameObject go = new GameObject("ScreenFader");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ScreenFader>();
                _instance.Build();
                return _instance;
            }
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("FaderCanvas", 5000);
            canvas.transform.SetParent(transform, false);
            _overlay = UIFactory.CreatePanel(canvas.transform, "Fade", new Color(0.01f, 0.01f, 0.04f, 0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _overlay.raycastTarget = false;
            SceneManagerHook();
        }

        private void SceneManagerHook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                // Every freshly loaded scene fades in from black.
                if (_alpha > 0.01f) StartCoroutine(Fade(_alpha, 0f, 0.35f, null));
            };
        }

        /// <summary>Fades out, runs <paramref name="action"/> (usually a scene load) and lets the new scene fade in.</summary>
        public static void Transition(Action action, float duration = 0.28f)
        {
            if (!Application.isPlaying || IsFading) { action?.Invoke(); return; }
            Instance.StartCoroutine(Instance.Run(action, duration));
        }

        private IEnumerator Run(Action action, float duration)
        {
            IsFading = true;
            yield return Fade(_alpha, 1f, duration, null);
            IsFading = false;
            action?.Invoke();
        }

        private IEnumerator Fade(float from, float to, float duration, Action done)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                _overlay.color = new Color(0.01f, 0.01f, 0.04f, _alpha);
                yield return null;
            }
            _alpha = to;
            _overlay.color = new Color(0.01f, 0.01f, 0.04f, _alpha);
            done?.Invoke();
        }
    }
}
