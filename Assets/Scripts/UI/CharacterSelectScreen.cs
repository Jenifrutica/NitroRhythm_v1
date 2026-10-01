using System.Collections.Generic;
using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// 3D character-select garage. Real karts and pilots spin on neon pedestals
    /// (built at runtime) while a translucent HUD shows names, stats, the P1/P2
    /// assignment and the four game modes. Clicking a kart or pressing its P1
    /// button selects it.
    /// </summary>
    public class CharacterSelectScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private GameSession _session;

        private string _p1 = "lyra";
        private string _p2 = "karel";
        private GameMode _mode = GameMode.TwoPlayerCoop;

        private readonly List<CharacterDisplay> _displays = new List<CharacterDisplay>();
        private readonly Dictionary<string, Image> _cardBorders = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _p1Buttons = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _p2Buttons = new Dictionary<string, Image>();
        private readonly Dictionary<GameMode, Image> _modeButtons = new Dictionary<GameMode, Image>();

        private TextMeshProUGUI _assignment;

        private static readonly string[] ModeLabels =
        {
            "1 JUGADOR",
            "1 JUGADOR + BOT",
            "COOPERATIVO 2P",
            "SHOWDOWN 3P"
        };

        private void Start()
        {
            _session = GameSession.EnsureExists();
            NeonPostFx.Ensure();
            BuildStage();
            BuildUI();
            Refresh();
        }

        // ------------------------------------------------------------- 3D stage

        private void BuildStage()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 2.9f, -11.8f);
            camera.transform.LookAt(new Vector3(0f, 1.35f, 0f));
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.01f, 0.05f);

            StageFactory.CreateFloor(new Color(0.35f, 0.25f, 0.85f), new Color(0.03f, 0.01f, 0.08f), 70f);
            StageFactory.AddRimLight(transform, new Vector3(-7f, 6f, -6f), new Color(0.3f, 0.6f, 1f), 1.2f, 30f);
            StageFactory.AddRimLight(transform, new Vector3(7f, 6f, -6f), new Color(1f, 0.3f, 0.7f), 1.2f, 30f);
            StageFactory.AddKeyLight(transform);

            PrototypeData data = PrototypeData.Instance;
            float[] positions = { -4.6f, 0f, 4.6f };
            int index = 0;

            foreach (CharacterDefinition def in data.characters)
            {
                if (def == null || index >= positions.Length) continue;

                GameObject displayObject = new GameObject($"Display_{def.id}");
                CharacterDisplay display = displayObject.AddComponent<CharacterDisplay>();
                display.Build(def, new Vector3(positions[index], 0f, 0f));
                display.Clicked += OnDisplayClicked;
                _displays.Add(display);

                index++;
            }
        }

        private void OnDisplayClicked(CharacterDisplay display)
        {
            _p1 = display.Definition.id;
            Refresh();
        }

        // ------------------------------------------------------------ 3D -> UI

        private void BuildUI()
        {
            _canvas = UIFactory.CreateCanvas("SelectCanvas", 0);
            _canvas.transform.SetParent(transform, false);

            TextMeshProUGUI title = UIFactory.CreateText(_canvas.transform, "Title", "SELECCIONA TU PILOTO", 64f, Color.white);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 70f));

            TextMeshProUGUI subtitle = UIFactory.CreateText(_canvas.transform, "Subtitle",
                "Lyra Pulse · Karel Volt · Vox Null", 24f, UIFactory.NeonGold);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(1200f, 34f));

            _assignment = UIFactory.CreateText(_canvas.transform, "Assignment", "", 22f, Color.white);
            Anchor(_assignment.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(1200f, 30f));

            PrototypeData data = PrototypeData.Instance;
            float[] anchors = { 0.214f, 0.5f, 0.786f };
            int index = 0;

            foreach (CharacterDefinition def in data.characters)
            {
                if (def == null || index >= anchors.Length) continue;
                BuildCard(def, anchors[index]);
                index++;
            }

            // Mode row.
            float modeSpacing = 360f;
            float modeStart = -540f;
            for (int i = 0; i < 4; i++)
            {
                GameMode mode = (GameMode)i;
                Button button = UIFactory.CreateButton(_canvas.transform, ModeLabels[i],
                    new Vector2(modeStart + i * modeSpacing, -395f), new Vector2(330f, 62f), UIFactory.NeonGold,
                    () => { _mode = mode; Refresh(); });
                _modeButtons[mode] = button.GetComponent<Image>();
            }

            UIFactory.CreateButton(_canvas.transform, "INICIAR CARRERA", new Vector2(0f, -475f), new Vector2(420f, 74f), UIFactory.NeonCyan, Play);
            UIFactory.CreateButton(_canvas.transform, "MENÚ", new Vector2(-720f, -475f), new Vector2(200f, 60f), UIFactory.NeonBlue, () => SceneFlow.LoadMainMenu());
        }

        private void BuildCard(CharacterDefinition def, float anchorX)
        {
            Image card = UIFactory.CreateCard(_canvas.transform, $"Card_{def.id}", new Color(0.07f, 0.08f, 0.15f, 0.86f),
                new Vector2(400f, 210f), Vector2.zero);
            RectTransform cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(anchorX, 0.5f);
            cardRect.anchorMax = new Vector2(anchorX, 0.5f);
            cardRect.anchoredPosition = new Vector2(0f, -235f);

            Image border = UIFactory.CreatePanel(card.transform, "Border", def.Accent,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            border.transform.SetAsFirstSibling();
            border.rectTransform.offsetMin = new Vector2(-3f, -3f);
            border.rectTransform.offsetMax = new Vector2(3f, 3f);
            border.color = new Color(def.Accent.r, def.Accent.g, def.Accent.b, 0.25f);
            _cardBorders[def.id] = border;

            TextMeshProUGUI name = UIFactory.CreateText(card.transform, "Name", def.displayName, 30f, def.Color);
            name.rectTransform.sizeDelta = new Vector2(360f, 40f);
            name.rectTransform.anchoredPosition = new Vector2(0f, 72f);

            BuildStatBar(card.transform, "VEL", def.StatSpeed01, 30f, UIFactory.NeonCyan);
            BuildStatBar(card.transform, "AGARRE", def.StatGrip01, 0f, UIFactory.NeonGold);
            BuildStatBar(card.transform, "PODER", def.StatPower01, -30f, UIFactory.NeonRed);

            if (!def.IsVillain)
            {
                Button p1 = UIFactory.CreateButton(card.transform, "P1", new Vector2(-90f, -70f), new Vector2(150f, 44f), UIFactory.NeonCyan,
                    () => { _p1 = def.id; Refresh(); });
                _p1Buttons[def.id] = p1.GetComponent<Image>();

                Button p2 = UIFactory.CreateButton(card.transform, "P2", new Vector2(90f, -70f), new Vector2(150f, 44f), UIFactory.NeonBlue,
                    () => { _p2 = def.id; Refresh(); });
                _p2Buttons[def.id] = p2.GetComponent<Image>();
            }
            else
            {
                TextMeshProUGUI villainTag = UIFactory.CreateText(card.transform, "VillainTag", "VILLANO (IA / P3)", 16f, UIFactory.NeonRed);
                villainTag.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            }
        }

        private void BuildStatBar(Transform parent, string label, float value01, float y, Color color)
        {
            TextMeshProUGUI text = UIFactory.CreateText(parent, $"Stat_{label}", label, 15f, new Color(0.85f, 0.85f, 0.95f), TextAlignmentOptions.Left);
            text.rectTransform.sizeDelta = new Vector2(96f, 22f);
            text.rectTransform.anchoredPosition = new Vector2(-122f, y);

            Image bg = UIFactory.CreatePanel(parent, $"StatBg_{label}", new Color(0.13f, 0.13f, 0.2f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bg.rectTransform.sizeDelta = new Vector2(210f, 14f);
            bg.rectTransform.anchoredPosition = new Vector2(40f, y);

            Image fill = UIFactory.CreatePanel(bg.transform, "Fill", color,
                new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.sizeDelta = new Vector2(210f * Mathf.Clamp01(value01), 0f);
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void Refresh()
        {
            foreach (var pair in _cardBorders)
            {
                bool isP1 = pair.Key == _p1;
                bool isP2 = pair.Key == _p2;
                float alpha = isP1 ? 1f : (isP2 ? 0.6f : 0.22f);
                Color c = pair.Value.color;
                pair.Value.color = new Color(c.r, c.g, c.b, alpha);
            }

            foreach (var pair in _p1Buttons)
            {
                pair.Value.color = pair.Key == _p1 ? UIFactory.NeonCyan : new Color(0.16f, 0.16f, 0.24f, 0.95f);
            }
            foreach (var pair in _p2Buttons)
            {
                pair.Value.color = pair.Key == _p2 ? UIFactory.NeonBlue : new Color(0.16f, 0.16f, 0.24f, 0.95f);
            }
            foreach (var pair in _modeButtons)
            {
                pair.Value.color = pair.Key == _mode ? UIFactory.NeonCyan : new Color(0.16f, 0.16f, 0.26f, 0.95f);
            }
            foreach (CharacterDisplay display in _displays)
            {
                display.SetSelected(display.Definition.id == _p1);
            }

            CharacterDefinition p1 = PrototypeData.Instance.GetCharacter(_p1);
            CharacterDefinition p2 = PrototypeData.Instance.GetCharacter(_p2);
            _assignment.text = $"P1: {(p1 != null ? p1.displayName : "-")}     P2: {(p2 != null ? p2.displayName : "-")}";
        }

        private void Play()
        {
            _session.Mode = _mode;
            _session.P1CharacterId = _p1;
            _session.P2CharacterId = _p2;
            _session.P3CharacterId = "vox";
            SceneFlow.LoadGameplay();
        }
    }
}
