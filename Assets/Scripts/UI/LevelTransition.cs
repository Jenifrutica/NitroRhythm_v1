using System;
using System.Collections;
using System.Collections.Generic;
using NitroRhythm.Core;
using NitroRhythm.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// 2D animated transition between domains (≈4.5 s, unscaled time):
    /// 1. two neon panels sweep in diagonally with speed lines;
    /// 2. "¡DOMINIO SUPERADO!" slams in with overshoot and a flash;
    /// 3. time and points count up;
    /// 4. a map of the 7 domains: the cleared medallion bursts, a kart icon hops along the path to the
    ///    next one, which pulses with its name, its illustration and (with a player song) its section;
    /// 5. the panels sweep out and the next scene loads.
    /// </summary>
    public class LevelTransition : MonoBehaviour
    {
        public static bool IsPlaying { get; private set; }

        private const float PanelIn = 0.45f;

        private Canvas _canvas;
        private RectTransform _root;
        private Action _onDone;

        public static void Play(int clearedLevel, Action onDone)
        {
            GameObject go = new GameObject("LevelTransition");
            DontDestroyOnLoad(go);
            LevelTransition t = go.AddComponent<LevelTransition>();
            t._onDone = onDone;
            t.StartCoroutine(t.Run(clearedLevel));
        }

        private IEnumerator Run(int cleared)
        {
            IsPlaying = true;
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;

            LevelDefinition[] levels = PrototypeData.Instance.PlayableLevels;
            int total = Mathf.Max(1, levels.Length);
            int index = Mathf.Clamp(cleared - 1, 0, total - 1);
            LevelDefinition done = levels.Length > 0 ? levels[index] : null;
            LevelDefinition next = index + 1 < levels.Length ? levels[index + 1] : null;
            Color accent = done != null ? done.Accent : UIFactory.NeonGold;
            Color nextAccent = next != null ? next.Accent : UIFactory.NeonGold;

            _canvas = UIFactory.CreateCanvas("TransitionCanvas", 200);
            _canvas.transform.SetParent(transform, false);
            _root = _canvas.GetComponent<RectTransform>();
            UIFactory.EnsureEventSystem();

            // ---- 1. darkening backdrop, diagonal panels + speed lines
            Image backdrop = UIFactory.CreatePanel(_root, "Backdrop", new Color(0.01f, 0.01f, 0.03f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = true;
            RectTransform top = Panel("PanelTop", new Color(0.03f, 0.02f, 0.08f, 1f), accent, 1);
            RectTransform bottom = Panel("PanelBottom", new Color(0.05f, 0.02f, 0.06f, 1f), nextAccent, -1);
            List<RectTransform> lines = SpeedLines(accent, 18);
            yield return Animate(PanelIn, k =>
            {
                float e = EaseOutCubic(k);
                backdrop.color = new Color(0.01f, 0.01f, 0.03f, 0.92f * k);
                top.anchoredPosition = new Vector2(Mathf.Lerp(-2600f, 0f, e), 270f);
                bottom.anchoredPosition = new Vector2(Mathf.Lerp(2600f, 0f, e), -270f);
            });
            StartCoroutine(MoveLines(lines));
            NitroRhythm.Audio.SfxPlayer.Play(NitroRhythm.Audio.SfxLibrary.Checkpoint, 0.8f, 0.9f);

            // ---- 2. title slam + flash
            Image flash = UIFactory.CreatePanel(_root, "Flash", new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            flash.raycastTarget = false;
            string titleText = next != null ? "¡DOMINIO SUPERADO!" : "¡ÚLTIMO DOMINIO SUPERADO!";
            TextMeshProUGUI title = UIFactory.CreateTitle(_root, "Title", titleText, 86f, Color.white, accent);
            Place(title.rectTransform, new Vector2(0f, 330f), new Vector2(1800f, 120f));
            TextMeshProUGUI domainName = UIFactory.CreateLabel(_root, "DomainName", done != null ? done.displayName.ToUpperInvariant() : "", 40f, accent);
            Place(domainName.rectTransform, new Vector2(0f, 240f), new Vector2(1400f, 56f));
            domainName.characterSpacing = 12f;
            yield return Animate(0.5f, k =>
            {
                float s = Mathf.LerpUnclamped(2.6f, 1f, EaseOutBack(k));
                title.rectTransform.localScale = Vector3.one * s;
                title.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, 0f, EaseOutCubic(k)));
                title.alpha = Mathf.Clamp01(k * 3f);
                flash.color = new Color(1f, 1f, 1f, k < 0.25f ? k * 2.4f : Mathf.Lerp(0.6f, 0f, (k - 0.25f) / 0.75f));
                domainName.alpha = Mathf.Clamp01((k - 0.4f) * 2f);
                domainName.characterSpacing = Mathf.Lerp(40f, 12f, EaseOutCubic(k));
            });
            NitroRhythm.Audio.SfxPlayer.Play(NitroRhythm.Audio.SfxLibrary.Goal, 0.9f, 1f);

            // ---- 3. stats count-up
            GameSession session = GameSession.Instance;
            int score = session != null ? session.LastScore : 0;
            float time = session != null ? session.LastTime : 0f;
            TextMeshProUGUI timeText = Stat("TIEMPO", new Vector2(-260f, 150f), accent);
            TextMeshProUGUI scoreText = Stat("PUNTOS", new Vector2(260f, 150f), accent);
            StartCoroutine(Animate(0.9f, k =>
            {
                float e = EaseOutCubic(k);
                timeText.text = GameplayHud.FormatTime(time * e);
                scoreText.text = Mathf.RoundToInt(score * e).ToString("D6");
            }));

            // ---- 4. domain map
            yield return Map(levels, index, accent, nextAccent, next, session);

            // ---- 5. sweep out
            yield return Animate(0.4f, k =>
            {
                float e = EaseInCubic(k);
                foreach (RectTransform child in _root)
                {
                    if (child == top || child == bottom || child == backdrop.rectTransform) continue;
                    CanvasGroup g = child.GetComponent<CanvasGroup>();
                    if (g == null) g = child.gameObject.AddComponent<CanvasGroup>();
                    g.alpha = 1f - e;
                }
                top.localScale = new Vector3(1f, 1f + e * 0.6f, 1f);
                bottom.localScale = new Vector3(1f, 1f + e * 0.6f, 1f);
            });

            Time.timeScale = previousScale > 0f ? previousScale : 1f;
            IsPlaying = false;
            Action done2 = _onDone;
            _onDone = null;
            done2?.Invoke();
            // Keep the panels on screen while the next scene loads, then fade them away.
            yield return new WaitForSecondsRealtime(0.6f);
            CanvasGroup all = _canvas.gameObject.AddComponent<CanvasGroup>();
            yield return Animate(0.35f, k => all.alpha = 1f - k);
            Destroy(gameObject);
        }

        private IEnumerator Map(LevelDefinition[] levels, int index, Color accent, Color nextAccent, LevelDefinition next, GameSession session)
        {
            int total = Mathf.Max(1, levels.Length);
            float width = 1500f, baseY = -170f;
            Vector2[] points = new Vector2[total];
            for (int i = 0; i < total; i++)
            {
                float t = total > 1 ? i / (float)(total - 1) : 0.5f;
                points[i] = new Vector2(Mathf.Lerp(-width * 0.5f, width * 0.5f, t), baseY + Mathf.Sin(t * Mathf.PI * 2f) * 45f);
            }

            // Path dots between the medallions.
            for (int i = 0; i < total - 1; i++)
            {
                for (int d = 1; d < 6; d++)
                {
                    Vector2 p = Vector2.Lerp(points[i], points[i + 1], d / 6f);
                    Image dot = Circle("Dot", i < index ? new Color(accent.r, accent.g, accent.b, 0.9f) : new Color(1f, 1f, 1f, 0.22f), 10f);
                    Place(dot.rectTransform, p, new Vector2(10f, 10f));
                }
            }

            Image[] medals = new Image[total];
            for (int i = 0; i < total; i++)
            {
                Color c = levels.Length > i ? levels[i].Accent : Color.gray;
                bool cleared = i < index;
                Image ring = Circle($"Medal{i}", cleared ? c : new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.95f), 74f);
                Place(ring.rectTransform, points[i], new Vector2(74f, 74f));
                TextMeshProUGUI n = UIFactory.CreateNumber(ring.transform, "N", (i + 1).ToString(), 34f, Color.white, c);
                Place(n.rectTransform, Vector2.zero, new Vector2(70f, 50f));
                medals[i] = ring;
                ring.rectTransform.localScale = Vector3.zero;
            }

            // Medallions pop in one after another.
            yield return Animate(0.55f, k =>
            {
                for (int i = 0; i < total; i++)
                {
                    float local = Mathf.Clamp01(k * total * 0.6f - i * 0.5f);
                    medals[i].rectTransform.localScale = Vector3.one * EaseOutBack(local);
                }
            });

            // The cleared medallion lights up with a shock ring and star burst.
            Image clearedMedal = medals[index];
            clearedMedal.color = accent;
            Image shock = Ring(accent);
            Place(shock.rectTransform, points[index], new Vector2(80f, 80f));
            List<RectTransform> stars = Burst(points[index], accent, 14);
            NitroRhythm.Audio.SfxPlayer.Play(NitroRhythm.Audio.SfxLibrary.Boost, 0.7f, 1.1f);
            yield return Animate(0.55f, k =>
            {
                float e = EaseOutCubic(k);
                shock.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(80f, 300f, e);
                shock.color = new Color(accent.r, accent.g, accent.b, 1f - k);
                clearedMedal.rectTransform.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI));
                for (int s = 0; s < stars.Count; s++)
                {
                    float angle = s / (float)stars.Count * Mathf.PI * 2f;
                    stars[s].anchoredPosition = points[index] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(20f, 190f, e);
                    stars[s].localRotation = Quaternion.Euler(0f, 0f, k * 360f);
                    stars[s].localScale = Vector3.one * (1f - k);
                }
            });

            if (next == null) yield break;

            // Next domain card (illustration + name + song section) slides up.
            RectTransform card = NextCard(next, nextAccent, index + 1, total, session);
            StartCoroutine(Animate(0.5f, k => card.anchoredPosition = new Vector2(0f, Mathf.Lerp(-640f, -395f, EaseOutBack(k)))));

            // The kart icon hops from the cleared medallion to the next one.
            TextMeshProUGUI kart = UIFactory.CreateIcon(_root, UIIcons.Car, 64f, Color.white);
            UIFactory.ApplyGlow(kart, nextAccent, 0.6f, 0.15f);
            Vector2 from = points[index], to = points[index + 1];
            yield return Animate(1.1f, k =>
            {
                float e = EaseInOutCubic(k);
                Vector2 p = Vector2.Lerp(from, to, e);
                float hop = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)) * 34f * (1f - k * 0.5f);
                kart.rectTransform.anchoredPosition = p + new Vector2(0f, 62f + hop);
                kart.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 6f) * 8f);
            });

            // Arrival: the next medallion pulses in its colour.
            Image nextMedal = medals[index + 1];
            nextMedal.color = nextAccent;
            Image arrive = Ring(nextAccent);
            Place(arrive.rectTransform, to, new Vector2(80f, 80f));
            yield return Animate(0.6f, k =>
            {
                arrive.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(80f, 220f, EaseOutCubic(k));
                arrive.color = new Color(nextAccent.r, nextAccent.g, nextAccent.b, 1f - k);
                nextMedal.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(k * Mathf.PI * 2f));
                kart.rectTransform.localScale = Vector3.one * (1f + 0.2f * Mathf.Sin(k * Mathf.PI));
            });
        }

        private RectTransform NextCard(LevelDefinition next, Color accent, int nextIndex, int total, GameSession session)
        {
            Image card = UIFactory.CreateCard(_root, "NextCard", new Color(0.04f, 0.04f, 0.1f, 0.95f), accent, new Vector2(980f, 150f), new Vector2(0f, -640f));
            Sprite art = UIArt.RoundedDomain(next.id);
            if (art != null)
            {
                Image pic = UIFactory.CreatePanel(card.transform, "Art", Color.white, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                pic.sprite = art;
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                Place(pic.rectTransform, new Vector2(-370f, 0f), new Vector2(210f, 124f));
            }
            TextMeshProUGUI label = UIFactory.CreateLabel(card.transform, "Next", $"PRÓXIMO · DOMINIO {nextIndex + 1}/{total}", 24f, UIFactory.TextSoft, TextAlignmentOptions.Left);
            Place(label.rectTransform, new Vector2(110f, 42f), new Vector2(700f, 32f));
            TextMeshProUGUI name = UIFactory.CreateTitle(card.transform, "Name", next.displayName.ToUpperInvariant(), 44f, Color.white, accent, TextAlignmentOptions.Left);
            Place(name.rectTransform, new Vector2(110f, -2f), new Vector2(700f, 56f));
            string extra = next.musicHint;
            if (session != null && session.HasUserSong)
            {
                session.UserSong.GetSectionTime(nextIndex, total, out float start, out float length);
                extra = $"♪ {session.UserSong.Name} · {NitroRhythm.Audio.UserSong.FormatTime(start)}–{NitroRhythm.Audio.UserSong.FormatTime(start + length)}";
            }
            TextMeshProUGUI hint = UIFactory.CreateText(card.transform, "Hint", extra ?? "", 22f, accent, TextAlignmentOptions.Left);
            Place(hint.rectTransform, new Vector2(110f, -44f), new Vector2(700f, 30f));
            hint.enableWordWrapping = false;
            hint.overflowMode = TextOverflowModes.Ellipsis;
            return card.rectTransform;
        }

        // ------------------------------------------------------------ building blocks

        private RectTransform Panel(string name, Color fill, Color edge, int side)
        {
            Image panel = UIFactory.CreatePanel(_root, name, fill, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;   // blocks clicks during the transition
            RectTransform r = panel.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(3200f, 640f);
            r.localRotation = Quaternion.Euler(0f, 0f, 6f);
            r.anchoredPosition = new Vector2(side * 2600f, side * 270f);
            Image stripe = UIFactory.CreatePanel(r, "Edge", edge, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            stripe.raycastTarget = false;
            RectTransform s = stripe.rectTransform;
            s.anchorMin = new Vector2(0f, side > 0 ? 0f : 1f);
            s.anchorMax = new Vector2(1f, side > 0 ? 0f : 1f);
            s.sizeDelta = new Vector2(0f, 10f);
            return r;
        }

        private List<RectTransform> SpeedLines(Color accent, int count)
        {
            List<RectTransform> lines = new List<RectTransform>();
            System.Random rng = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                Image line = UIFactory.CreatePanel(_root, "SpeedLine", new Color(accent.r, accent.g, accent.b, 0.35f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                line.sprite = UIFactory.BarSprite;
                line.raycastTarget = false;
                RectTransform r = line.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(180f + (float)rng.NextDouble() * 360f, 4f + (float)rng.NextDouble() * 5f);
                r.anchoredPosition = new Vector2(-1200f + (float)rng.NextDouble() * 2400f, -520f + (float)rng.NextDouble() * 1040f);
                r.localRotation = Quaternion.Euler(0f, 0f, 6f);
                lines.Add(r);
            }
            return lines;
        }

        private IEnumerator MoveLines(List<RectTransform> lines)
        {
            float[] speed = new float[lines.Count];
            for (int i = 0; i < speed.Length; i++) speed[i] = 1400f + (i * 397 % 1000);
            while (this != null && lines.Count > 0 && lines[0] != null)
            {
                float dt = Time.unscaledDeltaTime;
                for (int i = 0; i < lines.Count; i++)
                {
                    Vector2 p = lines[i].anchoredPosition;
                    p.x -= speed[i] * dt;
                    if (p.x < -1300f) p.x += 2600f;
                    lines[i].anchoredPosition = p;
                }
                yield return null;
            }
        }

        private TextMeshProUGUI Stat(string label, Vector2 position, Color accent)
        {
            TextMeshProUGUI l = UIFactory.CreateLabel(_root, $"Lbl{label}", label, 26f, UIFactory.TextSoft);
            Place(l.rectTransform, position + new Vector2(0f, 34f), new Vector2(400f, 34f));
            TextMeshProUGUI v = UIFactory.CreateNumber(_root, $"Val{label}", "0", 56f, Color.white, accent);
            Place(v.rectTransform, position - new Vector2(0f, 18f), new Vector2(460f, 70f));
            return v;
        }

        private Image Circle(string name, Color color, float size)
        {
            Image img = UIFactory.CreatePanel(_root, name, color, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            img.sprite = UIFactory.CircleSprite;
            img.raycastTarget = false;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        private Image Ring(Color color)
        {
            Image img = Circle("Shock", color, 80f);
            img.sprite = UIFactory.RingSprite;
            return img;
        }

        private List<RectTransform> Burst(Vector2 centre, Color color, int count)
        {
            List<RectTransform> stars = new List<RectTransform>();
            for (int i = 0; i < count; i++)
            {
                TextMeshProUGUI star = UIFactory.CreateIcon(_root, UIIcons.Star, 30f, i % 2 == 0 ? color : Color.white);
                star.rectTransform.anchoredPosition = centre;
                stars.Add(star.rectTransform);
            }
            return stars;
        }

        private static void Place(RectTransform r, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        private static IEnumerator Animate(float duration, Action<float> step)
        {
            float t = 0f;
            while (t < duration)
            {
                step(Mathf.Clamp01(t / duration));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            step(1f);
        }

        private static float EaseOutCubic(float k) => 1f - Mathf.Pow(1f - k, 3f);
        private static float EaseInCubic(float k) => k * k * k;
        private static float EaseInOutCubic(float k) => k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) / 2f;
        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
        }
    }
}
