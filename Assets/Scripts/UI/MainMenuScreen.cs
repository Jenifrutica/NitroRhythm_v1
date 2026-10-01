using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;

namespace NitroRhythm.UI
{
    /// <summary>
    /// Start screen with a live 3D stage: the hero kart spins on a neon grid
    /// floor behind a translucent neon menu. No flat backdrop — the 3D scene is
    /// the background.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private OptionsPanel _options;

        private void Start()
        {
            GameSession.EnsureExists();
            NeonPostFx.Ensure();
            BuildStage();
            BuildUI();
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
            display.Build(hero, new Vector3(3.7f, 0.4f, 0f), false, false);
            display.SetSelected(true);
        }

        private void BuildUI()
        {
            _canvas = UIFactory.CreateCanvas("MainMenuCanvas", 0);
            _canvas.transform.SetParent(transform, false);

            TextMeshProUGUI title = UIFactory.CreateText(_canvas.transform, "Title", "NITRO RHYTHM", 92f, UIFactory.NeonCyan);
            Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(70f, 250f), new Vector2(760f, 110f));

            TextMeshProUGUI tagline = UIFactory.CreateText(_canvas.transform, "Tagline",
                "Velocidad sintética · frecuencia neón\nvectores letales", 24f, Color.white);
            Anchor(tagline.rectTransform, new Vector2(0f, 0.5f), new Vector2(70f, 175f), new Vector2(760f, 70f));

            UIFactory.CreateButton(_canvas.transform, "JUGAR", new Vector2(150f, 60f), new Vector2(420f, 82f), UIFactory.NeonCyan, OnPlay);
            UIFactory.CreateButton(_canvas.transform, "OPCIONES", new Vector2(150f, -40f), new Vector2(420f, 82f), UIFactory.NeonGold, OnOptions);
            UIFactory.CreateButton(_canvas.transform, "SALIR", new Vector2(150f, -140f), new Vector2(420f, 82f), UIFactory.NeonRed, OnExit);

            TextMeshProUGUI credit = UIFactory.CreateText(_canvas.transform, "Credit",
                "SENA · GA5-220501087 · Prototipo funcional", 18f, new Color(0.7f, 0.7f, 0.8f));
            Anchor(credit.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(900f, 28f));

            _options = gameObject.AddComponent<OptionsPanel>();
            _options.Build(_canvas.transform);
            _options.Hide();
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
