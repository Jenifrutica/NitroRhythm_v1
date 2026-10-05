using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// End-of-run screen with a live 3D podium: the winning kart celebrates on the neon stage while a
    /// rank badge (S/A/B/C), the score (counting up), time and pilots are shown on neon cards.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        private TextMeshProUGUI _scoreText;
        private int _finalScore;
        private float _shownScore;
        private float _clock;

        private void Start()
        {
            NeonPostFx.Ensure();
            BuildStage();
            Build();
        }

        /// <summary>Rank from the final score (S ≥ 12000, A ≥ 8500, B ≥ 5500, otherwise C).</summary>
        public static string RankFor(int score)
        {
            if (score >= 12000) return "S";
            if (score >= 8500) return "A";
            if (score >= 5500) return "B";
            return "C";
        }

        private static Color RankColor(string rank)
        {
            switch (rank)
            {
                case "S": return UIFactory.NeonGold;
                case "A": return UIFactory.NeonCyan;
                case "B": return UIFactory.NeonGreen;
                default: return UIFactory.NeonPink;
            }
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
            camera.backgroundColor = new Color(0.03f, 0.01f, 0.07f);

            StageFactory.CreateFloor(new Color(0.9f, 0.6f, 0.15f), new Color(0.04f, 0.01f, 0.07f), 70f);
            StageFactory.AddKeyLight(transform);

            GameSession session = GameSession.EnsureExists();
            CharacterDefinition winner = session.GetCharacter(1);
            GameObject podium = new GameObject("WinnerDisplay");
            CharacterDisplay display = podium.AddComponent<CharacterDisplay>();
            display.Build(winner, new Vector3(3.9f, 0.4f, 0f), true, false);
            display.SetSelected(true);
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("ResultsCanvas", 0);
            canvas.transform.SetParent(transform, false);
            UIAmbient.Create(canvas.transform, UIFactory.NeonGold, UIFactory.NeonPink, 50);

            GameSession session = GameSession.EnsureExists();
            _finalScore = session.LastScore;
            string rank = RankFor(_finalScore);
            Color rankColor = RankColor(rank);

            TextMeshProUGUI title = UIFactory.CreateTitle(canvas.transform, "Title", "HARMONÍA RESTAURADA", 62f, UIFactory.NeonGold, UIFactory.NeonGold, TextAlignmentOptions.Left);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(90f, -60f), new Vector2(1100f, 80f));

            TextMeshProUGUI subtitle = UIFactory.CreateLabel(canvas.transform, "Subtitle",
                "Renacimiento de Harmonya  —  fin del prototipo", 30f, UIFactory.TextSoft, TextAlignmentOptions.Left);
            Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(94f, -146f), new Vector2(1100f, 40f));

            // Rank badge.
            Image badge = UIFactory.CreateCard(canvas.transform, "RankCard", new Color(0.04f, 0.05f, 0.12f, 0.9f), rankColor, new Vector2(300f, 330f), Vector2.zero);
            Anchor(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(90f, 20f), new Vector2(300f, 330f));
            TextMeshProUGUI rankLabel = UIFactory.CreateLabel(badge.transform, "RankLabel", "RANGO", 28f, UIFactory.TextSoft);
            rankLabel.rectTransform.anchoredPosition = new Vector2(0f, 128f);
            TextMeshProUGUI rankLetter = UIFactory.CreateTitle(badge.transform, "Rank", rank, 210f, rankColor, rankColor);
            rankLetter.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            rankLetter.rectTransform.sizeDelta = new Vector2(280f, 250f);

            // Stats card.
            Image stats = UIFactory.CreateCard(canvas.transform, "StatsCard", new Color(0.04f, 0.05f, 0.12f, 0.9f), UIFactory.NeonCyan, new Vector2(760f, 330f), Vector2.zero);
            Anchor(stats.rectTransform, new Vector2(0f, 0.5f), new Vector2(420f, 20f), new Vector2(760f, 330f));

            CharacterDefinition p1 = session.GetCharacter(1);
            CharacterDefinition p2 = session.GetCharacter(2);

            _scoreText = StatRow(stats.transform, UIIcons.Star, "PUNTUACIÓN", "000000", 105f, UIFactory.NeonCyan);
            StatRow(stats.transform, UIIcons.Timer, "TIEMPO TOTAL", GameplayHud.FormatTime(session.LastTime), 35f, Color.white);
            StatRow(stats.transform, UIIcons.Gamepad, "MODO", ModeName(session.Mode), -35f, UIFactory.NeonGold);
            StatRow(stats.transform, UIIcons.Person, "PILOTOS", $"{(p1 != null ? p1.displayName : "-")}  &  {(p2 != null ? p2.displayName : "-")}", -105f, UIFactory.NeonPink);

            UIFactory.CreateButton(canvas.transform, "JUGAR DE NUEVO", new Vector2(0f, 0f), new Vector2(460f, 82f), UIFactory.NeonCyan, () => SceneFlow.StartNewRun(), UIIcons.Replay);
            Anchor(canvas.transform.GetChild(canvas.transform.childCount - 1).GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(90f, 150f), new Vector2(460f, 82f));
            UIFactory.CreateButton(canvas.transform, "MENÚ PRINCIPAL", new Vector2(0f, 0f), new Vector2(460f, 82f), UIFactory.NeonGold, () => SceneFlow.LoadMainMenu(), UIIcons.Home);
            Anchor(canvas.transform.GetChild(canvas.transform.childCount - 1).GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(580f, 150f), new Vector2(460f, 82f));
        }

        private static string ModeName(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.SinglePlayer: return "1 Jugador";
                case GameMode.SinglePlayerAndBot: return "Jugador + Bot";
                case GameMode.TwoPlayerCoop: return "Cooperativo 2P";
                case GameMode.ThreePlayerShowdown: return "Showdown 3P";
                default: return mode.ToString();
            }
        }

        private TextMeshProUGUI StatRow(Transform parent, string glyph, string label, string value, float y, Color color)
        {
            TextMeshProUGUI icon = UIFactory.CreateIcon(parent, glyph, 42f, color);
            icon.rectTransform.anchoredPosition = new Vector2(-325f, y);

            TextMeshProUGUI labelText = UIFactory.CreateLabel(parent, "Label", label, 26f, UIFactory.TextSoft, TextAlignmentOptions.Left);
            labelText.rectTransform.anchoredPosition = new Vector2(-120f, y);
            labelText.rectTransform.sizeDelta = new Vector2(330f, 34f);

            TextMeshProUGUI valueText = UIFactory.CreateNumber(parent, "Value", value, 42f, color, color, TextAlignmentOptions.Right);
            valueText.rectTransform.anchoredPosition = new Vector2(115f, y);
            valueText.rectTransform.sizeDelta = new Vector2(480f, 46f);
            return valueText;
        }

        private void Update()
        {
            if (_scoreText == null) return;
            _clock += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01((_clock - 0.4f) / 1.6f);
            _shownScore = Mathf.Lerp(0f, _finalScore, 1f - Mathf.Pow(1f - t, 3f));
            _scoreText.text = Mathf.RoundToInt(_shownScore).ToString("D6");
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, anchor.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
