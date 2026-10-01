using System;
using NitroRhythm.Data;
using NitroRhythm.UI;
using TMPro;
using UnityEngine;

namespace NitroRhythm.Narrative
{
    /// <summary>
    /// Data-driven comic card player. Consumes a <see cref="CutsceneDefinition"/>
    /// (loaded from prototype_data.json) and shows a portrait panel, title and
    /// one dialogue line at a time. Advances automatically or on player input.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [SerializeField] private float _lineDuration = 3.2f;

        private Canvas _canvas;
        private CanvasGroup _group;
        private UnityEngine.UI.Image _portrait;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _speaker;
        private TextMeshProUGUI _body;
        private TextMeshProUGUI _hint;

        private CutsceneDefinition _definition;
        private Action _onComplete;
        private int _lineIndex;
        private float _timer;
        private bool _playing;

        public bool IsPlaying => _playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildUI();
            SetVisible(false);
        }

        private void BuildUI()
        {
            _canvas = UIFactory.CreateCanvas("DialogueCanvas", 50);
            _canvas.transform.SetParent(transform, false);

            _group = _canvas.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            // Dimmed backdrop.
            UIFactory.CreatePanel(_canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Card.
            UnityEngine.UI.Image card = UIFactory.CreateCard(_canvas.transform, "Card", new Color(0.08f, 0.09f, 0.16f, 0.97f),
                new Vector2(900f, 460f), Vector2.zero);

            // Portrait strip (villain / hero colour).
            _portrait = UIFactory.CreatePanel(card.transform, "Portrait", new Color(1f, 0.15f, 0.1f, 0.9f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            RectTransform portraitRect = _portrait.rectTransform;
            portraitRect.sizeDelta = new Vector2(260f, 360f);
            portraitRect.anchoredPosition = new Vector2(170f, -230f);

            _speaker = UIFactory.CreateText(card.transform, "Speaker", "VOX NULL", 40f, Color.white);
            RectTransform speakerRect = _speaker.rectTransform;
            speakerRect.sizeDelta = new Vector2(240f, 60f);
            speakerRect.anchoredPosition = new Vector2(-190f, -40f);

            _title = UIFactory.CreateText(card.transform, "Title", "", 30f, UIFactory.NeonGold);
            RectTransform titleRect = _title.rectTransform;
            titleRect.sizeDelta = new Vector2(540f, 50f);
            titleRect.anchoredPosition = new Vector2(140f, -50f);

            _body = UIFactory.CreateText(card.transform, "Body", "", 26f, Color.white);
            RectTransform bodyRect = _body.rectTransform;
            bodyRect.sizeDelta = new Vector2(540f, 240f);
            bodyRect.anchoredPosition = new Vector2(140f, -210f);

            _hint = UIFactory.CreateText(card.transform, "Hint", "[ clic / Espacio para continuar ]", 18f, new Color(0.7f, 0.7f, 0.8f));
            RectTransform hintRect = _hint.rectTransform;
            hintRect.sizeDelta = new Vector2(860f, 30f);
            hintRect.anchoredPosition = new Vector2(0f, -200f);
        }

        public void Play(CutsceneDefinition definition, Action onComplete)
        {
            if (definition == null)
            {
                onComplete?.Invoke();
                return;
            }

            _definition = definition;
            _onComplete = onComplete;
            _lineIndex = 0;
            _timer = 0f;
            _playing = true;

            _title.text = definition.title;
            _speaker.text = ResolveSpeakerName(definition.speaker);
            _portrait.color = CharacterDefinition.ParseHex(definition.backgroundHex, new Color(0.6f, 0.1f, 0.1f));

            SetVisible(true);
            ShowLine();
        }

        private string ResolveSpeakerName(string id)
        {
            CharacterDefinition def = PrototypeData.Instance.GetCharacter(id);
            return def != null ? def.displayName.ToUpperInvariant() : "???";
        }

        private void ShowLine()
        {
            if (_definition.lines == null || _definition.lines.Length == 0)
            {
                _body.text = _definition.taunt;
                return;
            }

            if (_lineIndex < _definition.lines.Length)
            {
                _body.text = _definition.lines[_lineIndex];
            }
            else
            {
                _body.text = _definition.taunt;
            }
        }

        private void Update()
        {
            if (!_playing) return;

            _timer += Time.unscaledDeltaTime;

            bool advance = _timer >= _lineDuration
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetMouseButtonDown(0);

            if (advance)
            {
                Advance();
            }
        }

        private void Advance()
        {
            _timer = 0f;
            _lineIndex++;

            int total = _definition.lines != null ? _definition.lines.Length : 0;
            if (_lineIndex > total)
            {
                Finish();
                return;
            }

            ShowLine();
        }

        public void Skip()
        {
            if (!_playing) return;
            Finish();
        }

        private void Finish()
        {
            _playing = false;
            SetVisible(false);

            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(visible);
        }
    }
}
