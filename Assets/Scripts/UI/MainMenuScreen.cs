using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Start screen: live 3D stage (the hero kart on a neon grid) behind a neon menu with an animated
    /// title, drifting particles, a music-reactive equalizer and a looping synthesized menu track.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private OptionsPanel _options;
        private AudioSource _music;
        private RectTransform _titleRoot;

        private void Start()
        {
            GameSession.EnsureExists();
            NeonPostFx.Ensure();
            BuildStage();
            BuildMusic();
            BuildUI();
        }

        private void BuildMusic()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = MusicGenerator.CreateDemoLoop(112f, 8);
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.volume = 0.45f * GameSession.EnsureExists().MusicVolume;
            _music.Play();
        }

        private void BuildStage()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 2.5f, -10.5f);
            camera.transform.LookAt(new Vector3(0f, 1.2f, 0f));
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.01f, 0.06f);

            StageFactory.CreateFloor(new Color(0.35f, 0.15f, 0.75f), new Color(0.03f, 0.01f, 0.08f), 70f);
            StageFactory.AddRimLight(transform, new Vector3(-6f, 6f, -6f), new Color(0.2f, 0.6f, 1f), 1.4f, 28f);
            StageFactory.AddRimLight(transform, new Vector3(6f, 6f, -6f), new Color(1f, 0.3f, 0.7f), 1.4f, 28f);
            StageFactory.AddKeyLight(transform);

            CharacterDefinition hero = PrototypeData.Instance.GetCharacter("lyra");
            GameObject showcase = new GameObject("MenuShowcase");
            CharacterDisplay display = showcase.AddComponent<CharacterDisplay>();
            display.Build(hero, new Vector3(3.9f, 0.4f, 0f), false, false);
            display.SetSelected(true);
        }

        private void BuildUI()
        {
            _canvas = UIFactory.CreateCanvas("MainMenuCanvas", 0);
            _canvas.transform.SetParent(transform, false);

            UIAmbient.Create(_canvas.transform, UIFactory.NeonCyan, UIFactory.NeonPink);

            // Title block.
            GameObject titleObject = new GameObject("TitleRoot", typeof(RectTransform));
            titleObject.transform.SetParent(_canvas.transform, false);
            _titleRoot = titleObject.GetComponent<RectTransform>();
            Anchor(_titleRoot, new Vector2(0f, 0.5f), new Vector2(90f, 270f), new Vector2(900f, 300f));

            TextMeshProUGUI nitro = UIFactory.CreateTitle(_titleRoot, "Nitro", "NITRO", 124f, UIFactory.NeonCyan, UIFactory.NeonCyan, TextAlignmentOptions.Left);
            nitro.characterSpacing = 10f;
            Anchor(nitro.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(900f, 140f));

            TextMeshProUGUI rhythm = UIFactory.CreateTitle(_titleRoot, "Rhythm", "RHYTHM", 124f, UIFactory.NeonPink, UIFactory.NeonPink, TextAlignmentOptions.Left);
            rhythm.characterSpacing = 10f;
            Anchor(rhythm.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -122f), new Vector2(900f, 140f));

            TextMeshProUGUI tagline = UIFactory.CreateLabel(_canvas.transform, "Tagline",
                "Velocidad sintética  ·  frecuencia neón  ·  vectores letales", 28f, UIFactory.TextSoft, TextAlignmentOptions.Left);
            Anchor(tagline.rectTransform, new Vector2(0f, 0.5f), new Vector2(94f, 95f), new Vector2(900f, 38f));

            MenuButton("JUGAR", UIIcons.Play, UIFactory.NeonCyan, 20f, OnPlay);
            MenuButton("OPCIONES", UIIcons.Settings, UIFactory.NeonGold, -78f, OnOptions);
            MenuButton("SALIR", UIIcons.Close, UIFactory.NeonRed, -176f, OnExit);

            UIEqualizer.Create(_canvas.transform, _music, UIFactory.NeonCyan, 36, new Vector2(720f, 70f), new Vector2(94f, 40f), new Vector2(0f, 0f));

            TextMeshProUGUI credit = UIFactory.CreateLabel(_canvas.transform, "Credit",
                "SENA  ·  GA5-220501087  ·  Prototipo funcional", 22f, new Color(0.65f, 0.7f, 0.85f), TextAlignmentOptions.Right);
            Anchor(credit.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(900f, 30f));

            _options = gameObject.AddComponent<OptionsPanel>();
            _options.Build(_canvas.transform);
            _options.Hide();
        }

        private void MenuButton(string label, string icon, Color accent, float y, UnityEngine.Events.UnityAction action)
        {
            UnityEngine.UI.Button button = UIFactory.CreateButton(_canvas.transform, label, Vector2.zero, new Vector2(470f, 84f), accent, action, icon);
            RectTransform rect = button.GetComponent<RectTransform>();
            Anchor(rect, new Vector2(0f, 0.5f), new Vector2(94f, y - 30f), new Vector2(470f, 84f));
        }

        private void Update()
        {
            if (_titleRoot != null)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.012f;
                _titleRoot.localScale = new Vector3(s, s, 1f);
            }
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void OnPlay() => SceneFlow.LoadCharacterSelect();
        private void OnOptions() => _options.Show();

        private void OnExit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
