using System.Collections.Generic;
using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// 3D character-select garage. The real karts and pilots show a steady 3/4 view on neon pedestals,
    /// each with a card (concept-art portrait, stats, P1/P2 assignment) and a segmented game-mode row.
    /// </summary>
    public class CharacterSelectScreen : MonoBehaviour
    {
        private class CardVisuals
        {
            public Image Glow;
            public Image Frame;
            public Image P1Button;
            public Image P2Button;
            public Color Accent;
            public RectTransform Root;
            public CanvasGroup Group;
            public GameObject P1Badge;
            public GameObject P2Badge;
        }

        private Canvas _canvas;
        private GameSession _session;

        private string _p1 = "lyra";
        private string _p2 = "karel";
        private GameMode _mode = GameMode.TwoPlayerCoop;

        private readonly List<CharacterDisplay> _displays = new List<CharacterDisplay>();
        private readonly Dictionary<string, CardVisuals> _cards = new Dictionary<string, CardVisuals>();
        private readonly Dictionary<GameMode, Image> _modeButtons = new Dictionary<GameMode, Image>();

        private TextMeshProUGUI _p1Label, _p2Label;

        private static readonly string[] ModeLabels = { "1 JUGADOR", "JUGADOR + BOT", "COOPERATIVO", "SHOWDOWN 3P" };
        private static readonly string[] ModeIcons = { UIIcons.Person, UIIcons.Car, UIIcons.Group, UIIcons.Fire };

        private static readonly float[] CardAnchors = { 0.214f, 0.5f, 0.786f };
        private const float CardWidthUnits = 460f;   // card width in canvas units
        private int _layoutWidth, _layoutHeight;

        private void Start()
        {
            _session = GameSession.EnsureExists();
            NeonPostFx.Ensure();
            BuildStage();
            BuildUI();
            Refresh();
            LayoutStage();
        }

        private void Update()
        {
            if (Screen.width != _layoutWidth || Screen.height != _layoutHeight) LayoutStage();
        }

        /// <summary>
        /// Puts each 3D kart over the centre of its card and sizes it from the card width, so the showcase
        /// stays proportioned whatever the window shape (the canvas scales with the screen, the 3D scene does not).
        /// </summary>
        private void LayoutStage()
        {
            Camera camera = Camera.main;
            if (camera == null || _canvas == null || Screen.width <= 0 || Screen.height <= 0) return;
            _layoutWidth = Screen.width;
            _layoutHeight = Screen.height;

            Plane stage = new Plane(Vector3.back, Vector3.zero);   // the z = 0 plane the pedestals stand on
            float WorldX(float viewportX)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(viewportX, 0.62f, 0f));
                return stage.Raycast(ray, out float enter) ? ray.GetPoint(enter).x : 0f;
            }

            float worldPerScreen = WorldX(1f) - WorldX(0f);
            float canvasWidth = ((RectTransform)_canvas.transform).rect.width;   // in canvas units, whatever the window size
            float cardFraction = CardWidthUnits / Mathf.Max(1f, canvasWidth);
            float cardWorld = cardFraction * worldPerScreen;
            float scale = Mathf.Clamp(cardWorld * 0.78f / 4.2f, 0.4f, 1.3f);

            for (int i = 0; i < _displays.Count && i < CardAnchors.Length; i++)
            {
                Vector3 p = _displays[i].transform.position;
                _displays[i].transform.position = new Vector3(WorldX(CardAnchors[i]), p.y, p.z);
                _displays[i].LayoutScale = scale;
            }
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

            // Karts sit in the upper half of the screen; the cards live below them.
            camera.transform.position = new Vector3(0f, 2.2f, -11.8f);
            camera.transform.LookAt(new Vector3(0f, -0.35f, 0f));
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.01f, 0.05f);

            StageFactory.CreateFloor(new Color(0.35f, 0.25f, 0.85f), new Color(0.03f, 0.01f, 0.08f), 70f);
            StageFactory.AddRimLight(transform, new Vector3(-7f, 6f, -6f), new Color(0.3f, 0.6f, 1f), 1.2f, 30f);
            StageFactory.AddRimLight(transform, new Vector3(7f, 6f, -6f), new Color(1f, 0.3f, 0.7f), 1.2f, 30f);
            StageFactory.AddKeyLight(transform);

            PrototypeData data = PrototypeData.Instance;
            float[] positions = { -4.7f, 0f, 4.7f };
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

        /// <summary>A filled pill sitting on the card's top edge: tells which player has this pilot.</summary>
        private static GameObject MakeBadge(Transform card, string text, Color fill, Color textColor)
        {
            GameObject badge = new GameObject($"Badge_{text}", typeof(Image));
            badge.transform.SetParent(card, false);
            Image image = badge.GetComponent<Image>();
            image.sprite = UIFactory.RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;
            image.raycastTarget = false;
            RectTransform rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(240f, 54f);
            rect.anchoredPosition = new Vector2(0f, 14f);

            TextMeshProUGUI label = UIFactory.CreateLabel(badge.transform, "Label", "▼  " + text, 30f, textColor);
            label.characterSpacing = 4f;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            badge.SetActive(false);
            return badge;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Picks a pilot for a slot; if the other slot already has it, the two swap so they always differ.</summary>
        private void Assign(int slot, string id)
        {
            if (slot == 1) { if (_p2 == id) _p2 = _p1; _p1 = id; }
            else { if (_p1 == id) _p1 = _p2; _p2 = id; }
            Refresh();
        }

        private void OnDisplayClicked(CharacterDisplay display)
        {
            if (!display.Definition.IsVillain) _p1 = display.Definition.id == _p2 ? _p1 : display.Definition.id;
            Refresh();
        }

        // ------------------------------------------------------------------- UI

        private void BuildUI()
        {
            _canvas = UIFactory.CreateCanvas("SelectCanvas", 0);
            _canvas.transform.SetParent(transform, false);

            UIAmbient.Create(_canvas.transform, UIFactory.NeonPink, UIFactory.NeonCyan, 34);

            TextMeshProUGUI title = UIFactory.CreateTitle(_canvas.transform, "Title", "SELECCIONA TU PILOTO", 58f, Color.white, UIFactory.NeonCyan);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(1400f, 76f), new Vector2(0.5f, 1f));

            _p1Label = UIFactory.CreateLabel(_canvas.transform, "P1Label", "", 32f, UIFactory.NeonCyan, TextAlignmentOptions.Right);
            Anchor(_p1Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(-30f, -116f), new Vector2(700f, 42f), new Vector2(1f, 1f));
            _p2Label = UIFactory.CreateLabel(_canvas.transform, "P2Label", "", 32f, new Color(0.45f, 0.6f, 1f), TextAlignmentOptions.Left);
            Anchor(_p2Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(30f, -116f), new Vector2(700f, 42f), new Vector2(0f, 1f));

            float[] anchors = { 0.214f, 0.5f, 0.786f };
            int index = 0;
            foreach (CharacterDefinition def in PrototypeData.Instance.characters)
            {
                if (def == null || index >= anchors.Length) continue;
                BuildCard(def, anchors[index]);
                index++;
            }

            // Game mode: segmented row.
            for (int i = 0; i < 4; i++)
            {
                GameMode mode = (GameMode)i;
                Button button = UIFactory.CreateButton(_canvas.transform, ModeLabels[i],
                    new Vector2(-690f + i * 460f, -385f), new Vector2(440f, 64f), UIFactory.NeonGold,
                    () => { _mode = mode; Refresh(); }, ModeIcons[i]);
                _modeButtons[mode] = button.GetComponent<Image>();
            }

            UIFactory.CreateButton(_canvas.transform, "INICIAR CARRERA", new Vector2(0f, -475f), new Vector2(500f, 80f), UIFactory.NeonCyan, Play, UIIcons.Rocket);
            UIFactory.CreateButton(_canvas.transform, "CONTROLES", new Vector2(760f, -475f), new Vector2(300f, 64f), UIFactory.NeonGold,
                () => ControlsOverlay.Show(_mode, false, null, "ESPACIO PARA CERRAR"), UIIcons.Gamepad);
            UIFactory.CreateButton(_canvas.transform, "MENÚ", new Vector2(-760f, -475f), new Vector2(260f, 64f), UIFactory.NeonBlue, () => SceneFlow.LoadMainMenu(), UIIcons.Home);
        }

        private void BuildCard(CharacterDefinition def, float anchorX)
        {
            Color accent = def.IsVillain ? UIFactory.NeonRed : Color.Lerp(def.Color, Color.white, 0.25f);
            Image card = UIFactory.CreateCard(_canvas.transform, $"Card_{def.id}", new Color(0.04f, 0.05f, 0.12f, 0.9f), accent,
                new Vector2(460f, 340f), Vector2.zero);
            RectTransform cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(anchorX, 0.5f);
            cardRect.anchorMax = new Vector2(anchorX, 0.5f);
            cardRect.anchoredPosition = new Vector2(0f, -140f);

            CardVisuals visuals = new CardVisuals
            {
                Glow = card.transform.Find("Glow").GetComponent<Image>(),
                Frame = card.transform.Find("Frame").GetComponent<Image>(),
                Accent = accent
            };
            visuals.Root = cardRect;
            visuals.Group = card.gameObject.AddComponent<CanvasGroup>();
            if (!def.IsVillain)
            {
                visuals.P1Badge = MakeBadge(card.transform, "JUGADOR 1", UIFactory.NeonCyan, new Color(0.02f, 0.1f, 0.16f));
                visuals.P2Badge = MakeBadge(card.transform, "JUGADOR 2", new Color(0.35f, 0.5f, 1f), Color.white);
            }
            _cards[def.id] = visuals;

            // Portrait (concept art), framed.
            Sprite portrait = UIArt.Portrait(def.id);
            if (portrait != null)
            {
                GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform));
                portraitObject.transform.SetParent(card.transform, false);
                RectTransform pr = portraitObject.GetComponent<RectTransform>();
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.sizeDelta = new Vector2(150f, 200f);
                pr.anchoredPosition = new Vector2(-142f, 45f);

                // Rounded corners are baked into the sprite (no stencil mask).
                GameObject artObject = new GameObject("PortraitArt", typeof(Image));
                artObject.transform.SetParent(portraitObject.transform, false);
                Image portraitImage = artObject.GetComponent<Image>();
                portraitImage.sprite = UIArt.RoundedPortrait(def.id) ?? portrait;
                portraitImage.raycastTarget = false;
                Stretch(artObject.GetComponent<RectTransform>(), 0f);

                GameObject frame = new GameObject("PortraitFrame", typeof(Image));
                frame.transform.SetParent(portraitObject.transform, false);
                Image frameImage = frame.GetComponent<Image>();
                frameImage.sprite = UIFactory.FrameSprite;
                frameImage.type = Image.Type.Sliced;
                frameImage.color = accent;
                frameImage.raycastTarget = false;
                Stretch(frame.GetComponent<RectTransform>(), 0f);
            }

            TextMeshProUGUI name = UIFactory.CreateTitle(card.transform, "Name", def.displayName.ToUpperInvariant(), 27f, Color.white, accent, TextAlignmentOptions.Left);
            name.rectTransform.sizeDelta = new Vector2(250f, 40f);
            name.rectTransform.anchoredPosition = new Vector2(88f, 128f);

            TextMeshProUGUI role = UIFactory.CreateLabel(card.transform, "Role", def.IsVillain ? "VILLANO  ·  IA / P3" : "HÉROE", 22f, def.IsVillain ? UIFactory.NeonRed : UIFactory.NeonGold, TextAlignmentOptions.Left);
            role.rectTransform.sizeDelta = new Vector2(250f, 28f);
            role.rectTransform.anchoredPosition = new Vector2(88f, 98f);

            BuildStat(card.transform, "VEL", def.StatSpeed01, 56f, UIFactory.NeonCyan);
            BuildStat(card.transform, "AGARRE", def.StatGrip01, 18f, UIFactory.NeonGold);
            BuildStat(card.transform, "PODER", def.StatPower01, -20f, UIFactory.NeonRed);

            TextMeshProUGUI description = UIFactory.CreateText(card.transform, "Description", def.description, 19f, UIFactory.TextSoft, TextAlignmentOptions.TopLeft);
            description.enableWordWrapping = true;
            description.rectTransform.sizeDelta = new Vector2(410f, 58f);
            description.rectTransform.anchoredPosition = new Vector2(0f, -83f);

            if (!def.IsVillain)
            {
                Button p1 = UIFactory.CreateButton(card.transform, "P1", new Vector2(-100f, -140f), new Vector2(180f, 46f), UIFactory.NeonCyan,
                    () => Assign(1, def.id));
                visuals.P1Button = p1.GetComponent<Image>();

                Button p2 = UIFactory.CreateButton(card.transform, "P2", new Vector2(100f, -140f), new Vector2(180f, 46f), UIFactory.NeonBlue,
                    () => Assign(2, def.id));
                visuals.P2Button = p2.GetComponent<Image>();
            }
            else
            {
                TextMeshProUGUI tag = UIFactory.CreateLabel(card.transform, "VillainTag", "ES EL RIVAL DE LA CARRERA", 20f, UIFactory.NeonRed);
                tag.rectTransform.anchoredPosition = new Vector2(0f, -140f);
            }
        }

        private void BuildStat(Transform parent, string label, float value01, float y, Color color)
        {
            TextMeshProUGUI text = UIFactory.CreateLabel(parent, $"Stat_{label}", label, 19f, UIFactory.TextSoft, TextAlignmentOptions.Left);
            text.rectTransform.sizeDelta = new Vector2(90f, 22f);
            text.rectTransform.anchoredPosition = new Vector2(30f, y);

            NeonBar bar = UIFactory.CreateNeonBar(parent, $"StatBar_{label}", color, new Vector2(140f, 14f), new Vector2(150f, y));
            bar.SetValue(value01, true);
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void Refresh()
        {
            foreach (var pair in _cards)
            {
                bool isP1 = pair.Key == _p1;
                bool isP2 = pair.Key == _p2;
                CardVisuals v = pair.Value;
                bool chosen = isP1 || isP2;
                v.Glow.color = new Color(v.Accent.r, v.Accent.g, v.Accent.b, chosen ? 0.9f : 0.1f);
                v.Frame.color = new Color(v.Accent.r, v.Accent.g, v.Accent.b, chosen ? 1f : 0.45f);
                // The chosen pilots stand out: full brightness, a bit larger, with a player badge; the rest recede.
                bool villainCard = v.P1Badge == null;
                if (v.Group != null) v.Group.alpha = chosen ? 1f : (villainCard ? 0.8f : 0.55f);
                if (v.Root != null) v.Root.localScale = Vector3.one * (chosen ? 1.06f : 0.96f);
                if (v.P1Badge != null) v.P1Badge.SetActive(isP1);
                if (v.P2Badge != null) v.P2Badge.SetActive(isP2);

                if (v.P1Button != null) v.P1Button.color = isP1 ? new Color(0.1f, 0.4f, 0.55f, 1f) : new Color(0.05f, 0.1f, 0.14f, 0.95f);
                if (v.P2Button != null) v.P2Button.color = isP2 ? new Color(0.06f, 0.12f, 0.45f, 1f) : new Color(0.04f, 0.05f, 0.12f, 0.95f);
            }

            foreach (var pair in _modeButtons)
            {
                pair.Value.color = pair.Key == _mode ? new Color(0.45f, 0.34f, 0.04f, 1f) : new Color(0.1f, 0.09f, 0.04f, 0.95f);
            }
            foreach (CharacterDisplay display in _displays)
            {
                display.SetSelected(display.Definition.id == _p1 || display.Definition.id == _p2);
            }

            CharacterDefinition p1 = PrototypeData.Instance.GetCharacter(_p1);
            CharacterDefinition p2 = PrototypeData.Instance.GetCharacter(_p2);
            _p1Label.text = $"P1  {(p1 != null ? p1.displayName.ToUpperInvariant() : "-")}";
            _p2Label.text = $"P2  {(p2 != null ? p2.displayName.ToUpperInvariant() : "-")}";
        }

        private void Play()
        {
            _session.Mode = _mode;
            _session.P1CharacterId = _p1;
            _session.P2CharacterId = _p2;
            _session.P3CharacterId = "vox";
            SceneFlow.StartNewRun();
        }
    }
}
