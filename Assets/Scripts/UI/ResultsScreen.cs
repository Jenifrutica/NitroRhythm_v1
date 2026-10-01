using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;

namespace NitroRhythm.UI
{
    /// <summary>
    /// End-of-run screen with a live 3D podium: the winning kart celebrates on
    /// the neon stage while the final score and summary float above.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        private void Start()
        {
            NeonPostFx.Ensure();
            BuildStage();
            Build();
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
            display.Build(winner, new Vector3(3.6f, 0.4f, 0f), true, false);
            display.SetSelected(true);
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("ResultsCanvas", 0);
            canvas.transform.SetParent(transform, false);

            GameSession session = GameSession.EnsureExists();

            TextMeshProUGUI title = UIFactory.CreateText(canvas.transform, "Title", "HARMONÍA RESTAURADA", 56f, UIFactory.NeonGold, TextAlignmentOptions.TopLeft);
            Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 320f), new Vector2(900f, 70f));

            TextMeshProUGUI subtitle = UIFactory.CreateText(canvas.transform, "Subtitle",
                "Renacimiento de Harmonya — fin del prototipo", 24f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 265f), new Vector2(900f, 34f));

            TextMeshProUGUI score = UIFactory.CreateText(canvas.transform, "Score",
                $"PUNTUACIÓN FINAL: {session.LastScore:D6}", 40f, UIFactory.NeonCyan, TextAlignmentOptions.TopLeft);
            Anchor(score.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 170f), new Vector2(900f, 60f));

            CharacterDefinition p1 = session.GetCharacter(1);
            CharacterDefinition p2 = session.GetCharacter(2);
            TextMeshProUGUI summary = UIFactory.CreateText(canvas.transform, "Summary",
                $"Modo: {session.Mode}\nP1: {(p1 != null ? p1.displayName : "-")}    P2: {(p2 != null ? p2.displayName : "-")}",
                24f, new Color(0.85f, 0.85f, 0.95f), TextAlignmentOptions.TopLeft);
            Anchor(summary.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 60f), new Vector2(900f, 110f));

            UIFactory.CreateButton(canvas.transform, "JUGAR DE NUEVO", new Vector2(430f, -230f), new Vector2(420f, 76f), UIFactory.NeonCyan,
                () => SceneFlow.LoadGameplay());
            UIFactory.CreateButton(canvas.transform, "MENÚ PRINCIPAL", new Vector2(430f, -320f), new Vector2(420f, 72f), UIFactory.NeonGold,
                () => SceneFlow.LoadMainMenu());
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
