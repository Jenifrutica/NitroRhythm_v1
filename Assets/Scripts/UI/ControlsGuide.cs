using NitroRhythm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// "How to play" content for a game mode: one card per human player with its keys, plus the rules that
    /// matter (goal, K.O., prizes). Used by the pause menu, the first-level start screen and the select screen.
    /// </summary>
    public static class ControlsGuide
    {
        private struct Row
        {
            public string[] Keys;
            public string Action;
            public Row(string action, params string[] keys) { Keys = keys; Action = action; }
        }

        private struct PlayerCard
        {
            public string Title;
            public string Subtitle;
            public Color Color;
            public Row[] Rows;
        }

        public static string ModeTitle(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.SinglePlayer: return "1 JUGADOR";
                case GameMode.SinglePlayerAndBot: return "JUGADOR + BOT";
                case GameMode.TwoPlayerCoop: return "COOPERATIVO";
                default: return "SHOWDOWN 3P";
            }
        }

        public static string ModeDescription(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.SinglePlayer: return "Tú contra el villano, que juega la IA.";
                case GameMode.SinglePlayerAndBot: return "Te acompaña un bot (P2) controlado por la IA: no necesita teclas.";
                case GameMode.TwoPlayerCoop: return "Dos jugadores en el mismo teclado, pantalla dividida.";
                default: return "Dos héroes contra un villano humano que lanza obstáculos.";
            }
        }

        private static PlayerCard[] CardsFor(GameMode mode)
        {
            PlayerCard p1 = new PlayerCard
            {
                Title = "JUGADOR 1", Subtitle = "Lyra  ·  teclas W A S D", Color = UIFactory.NeonCyan,
                Rows = new[]
                {
                    new Row("Acelerar", "W"), new Row("Frenar / reversa", "S"),
                    new Row("Girar", "A", "D"), new Row("Saltar", "ESPACIO"),
                }
            };
            PlayerCard p2 = new PlayerCard
            {
                Title = "JUGADOR 2", Subtitle = "Karel  ·  flechas", Color = new Color(0.45f, 0.6f, 1f),
                Rows = new[]
                {
                    new Row("Acelerar", "ARRIBA"), new Row("Frenar / reversa", "ABAJO"),
                    new Row("Girar", "IZQ.", "DER."), new Row("Saltar", "SHIFT DER."),
                }
            };
            PlayerCard p3 = new PlayerCard
            {
                Title = "VILLANO (P3)", Subtitle = "Vox  ·  teclas I J K L", Color = UIFactory.NeonRed,
                Rows = new[]
                {
                    new Row("Avanzar", "I"), new Row("Retroceder", "K"),
                    new Row("Girar", "J", "L"), new Row("Saltar", "U"), new Row("Lanzar obstáculo", "ENTER"),
                }
            };

            switch (mode)
            {
                case GameMode.SinglePlayer:
                case GameMode.SinglePlayerAndBot: return new[] { p1 };
                case GameMode.TwoPlayerCoop: return new[] { p1, p2 };
                default: return new[] { p1, p2, p3 };
            }
        }

        private static readonly string[] Rules =
        {
            "META: el nivel avanza cuando llegan todos; al último se le esperan 6 s.",
            "K.O.: sin vida quedas fuera 5 s y revives en el último checkpoint.",
            "Esquiva los ataques del villano al compás y recoge premios para turbo.",
        };

        /// <summary>Builds the guide centred in <paramref name="parent"/>; returns its root.</summary>
        public static RectTransform Build(Transform parent, GameMode mode)
        {
            GameObject root = new GameObject("ControlsGuide", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);

            PlayerCard[] cards = CardsFor(mode);
            const float cardWidth = 520f, cardHeight = 412f, gap = 36f;
            float total = cards.Length * cardWidth + (cards.Length - 1) * gap;
            float startX = -total * 0.5f + cardWidth * 0.5f;

            for (int i = 0; i < cards.Length; i++)
                BuildCard(root.transform, cards[i], new Vector2(startX + i * (cardWidth + gap), 0f), new Vector2(cardWidth, cardHeight));

            // Rules strip under the cards.
            float rulesY = -cardHeight * 0.5f - 70f;
            for (int i = 0; i < Rules.Length; i++)
            {
                TextMeshProUGUI rule = UIFactory.CreateLabel(root.transform, $"Rule{i}", "•  " + Rules[i], 26f, UIFactory.TextSoft, TextAlignmentOptions.Left);
                rule.enableWordWrapping = false;
                rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rule.rectTransform.sizeDelta = new Vector2(Mathf.Max(total, 1100f), 34f);
                rule.rectTransform.anchoredPosition = new Vector2(0f, rulesY - i * 38f);
            }

            float width = Mathf.Max(total, 1100f);
            rootRect.sizeDelta = new Vector2(width, cardHeight + 70f + Rules.Length * 38f);
            return rootRect;
        }

        private static void BuildCard(Transform parent, PlayerCard data, Vector2 position, Vector2 size)
        {
            Image card = UIFactory.CreateCard(parent, $"Card_{data.Title}", new Color(0.04f, 0.05f, 0.12f, 0.96f), data.Color, size, position);
            card.raycastTarget = false;

            TextMeshProUGUI title = UIFactory.CreateTitle(card.transform, "Title", data.Title, 36f, Color.white, data.Color, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.sizeDelta = new Vector2(size.x - 40f, 50f);
            title.rectTransform.anchoredPosition = new Vector2(28f, -20f);

            TextMeshProUGUI sub = UIFactory.CreateLabel(card.transform, "Sub", data.Subtitle.ToUpperInvariant(), 24f, data.Color, TextAlignmentOptions.Left);
            sub.rectTransform.anchorMin = sub.rectTransform.anchorMax = new Vector2(0f, 1f);
            sub.rectTransform.pivot = new Vector2(0f, 1f);
            sub.rectTransform.sizeDelta = new Vector2(size.x - 40f, 32f);
            sub.rectTransform.anchoredPosition = new Vector2(28f, -66f);

            float y = -118f;
            foreach (Row row in data.Rows)
            {
                float x = 28f;
                foreach (string key in row.Keys)
                {
                    float w = Mathf.Max(54f, key.Length * 17f + 28f);
                    KeyCap(card.transform, key, data.Color, new Vector2(x, y), new Vector2(w, 46f));
                    x += w + 8f;
                }
                TextMeshProUGUI action = UIFactory.CreateLabel(card.transform, "Action", row.Action, 26f, Color.white, TextAlignmentOptions.Left);
                action.enableWordWrapping = false;
                action.rectTransform.anchorMin = action.rectTransform.anchorMax = new Vector2(0f, 1f);
                action.rectTransform.pivot = new Vector2(0f, 0.5f);
                action.rectTransform.sizeDelta = new Vector2(size.x - 28f - (x + 14f), 40f);
                action.rectTransform.anchoredPosition = new Vector2(x + 14f, y - 23f);
                // Keep the action text in a fixed column so rows line up.
                y -= 54f;
            }
        }

        private static void KeyCap(Transform parent, string text, Color accent, Vector2 topLeft, Vector2 size)
        {
            GameObject cap = new GameObject($"Key_{text}", typeof(Image));
            cap.transform.SetParent(parent, false);
            Image image = cap.GetComponent<Image>();
            image.sprite = UIFactory.RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(accent.r * 0.28f + 0.05f, accent.g * 0.28f + 0.05f, accent.b * 0.28f + 0.08f, 1f);
            image.raycastTarget = false;
            RectTransform rect = cap.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = topLeft;

            Image rim = UIFactory.CreatePanel(cap.transform, "Rim", new Color(accent.r, accent.g, accent.b, 0.9f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rim.sprite = UIFactory.FrameSprite;
            rim.type = Image.Type.Sliced;
            rim.raycastTarget = false;

            TextMeshProUGUI label = UIFactory.CreateLabel(cap.transform, "Label", text, text.Length > 5 ? 20f : 26f, Color.white);
            label.enableWordWrapping = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
