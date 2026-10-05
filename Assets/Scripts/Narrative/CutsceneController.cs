using NitroRhythm.Combat;
using NitroRhythm.Core;
using NitroRhythm.Data;
using UnityEngine;

namespace NitroRhythm.Narrative
{
    /// <summary>
    /// Level-transition controller. When the goal is reached it:
    /// 1. freezes world physics,
    /// 2. plays the data-driven villain taunt via <see cref="DialogueSystem"/>,
    /// 3. animates the villain escaping through a portal in unscaled time,
    /// 4. unlocks the next domain (or loads the results scene after the last one).
    /// </summary>
    public class CutsceneController : MonoBehaviour
    {
        public static CutsceneController Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float _escapeDuration = 1.8f;
        [SerializeField] private Color _portalColor = new Color(0.7f, 0.2f, 1f);

        private bool _playing;
        private bool _escaping;
        private float _escapeTimer;
        private int _pendingLevel;

        private LevelManager _levelManager;
        private VillainBoss _villain;
        private GameObject _portal;
        private Vector3 _villainStart;
        private Vector3 _villainScale;
        private Quaternion _villainRotation;

        public bool IsPlaying => _playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _levelManager = FindObjectOfType<LevelManager>();
        }

        /// <summary>Starts the escape sequence for a completed level.</summary>
        public void BeginLevelEscape(int levelNumber)
        {
            if (_playing) return;

            _playing = true;
            _pendingLevel = levelNumber;

            if (_levelManager == null) _levelManager = FindObjectOfType<LevelManager>();
            if (_villain == null) _villain = FindObjectOfType<VillainBoss>();

            if (_villain != null)
            {
                _villainStart = _villain.transform.position;
                _villainScale = _villain.transform.localScale;
                _villainRotation = _villain.transform.rotation;
            }

            Time.timeScale = 0f;

            CutsceneDefinition definition = ResolveCutscene(levelNumber);
            DialogueSystem dialogue = DialogueSystem.Instance;
            if (dialogue != null && definition != null)
            {
                dialogue.Play(definition, BeginEscapeAnimation);
            }
            else
            {
                BeginEscapeAnimation();
            }

            Debug.Log($"[CutsceneController] Level {levelNumber} escape started.");
        }

        private CutsceneDefinition ResolveCutscene(int levelNumber)
        {
            LevelDefinition[] playable = PrototypeData.Instance.PlayableLevels;
            int index = Mathf.Clamp(levelNumber - 1, 0, Mathf.Max(0, playable.Length - 1));

            string id = "outro";
            if (playable.Length > 0)
            {
                id = $"outro_{playable[index].id}";
            }

            CutsceneDefinition def = PrototypeData.Instance.GetCutscene(id);
            if (def == null) def = PrototypeData.Instance.GetCutscene("intro");
            return def;
        }

        private void BeginEscapeAnimation()
        {
            _escaping = true;
            _escapeTimer = 0f;
            SpawnPortal();
        }

        private void Update()
        {
            if (!_playing || !_escaping) return;

            _escapeTimer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_escapeTimer / Mathf.Max(0.1f, _escapeDuration));

            if (_villain != null)
            {
                Vector3 position = _villainStart;
                position.y += 14f * progress;
                position.x += 2f * progress;
                _villain.transform.position = position;

                float shrink = Mathf.Clamp01((progress - 0.5f) / 0.5f);
                _villain.transform.localScale = Vector3.Lerp(_villainScale, Vector3.zero, shrink);
            }

            if (_escapeTimer >= _escapeDuration)
            {
                FinishEscape();
            }
        }

        private void FinishEscape()
        {
            _escaping = false;
            _playing = false;
            Time.timeScale = 1f;
            HidePortal();

            if (_villain != null)
            {
                _villain.transform.localScale = _villainScale;
                _villain.transform.rotation = _villainRotation;
            }

            bool wasLastLevel = _pendingLevel >= (_levelManager != null ? _levelManager.TotalLevels : 1);

            // Each domain is its own screen: advance the session and load the next one (or results).
            if (_levelManager != null && _levelManager.SingleLevelMode)
            {
                GameSession session = GameSession.EnsureExists();
                if (session.AdvanceLevel()) SceneFlow.LoadGameplay();
                else SceneFlow.LoadResults();
                return;
            }

            if (_levelManager != null)
            {
                _levelManager.CompleteLevel(_pendingLevel);
            }

            if (wasLastLevel)
            {
                SceneFlow.LoadResults();
                return;
            }

            if (_levelManager != null)
            {
                _levelManager.RepositionPlayersToCheckpoint();

                if (_villain == null) _villain = FindObjectOfType<VillainBoss>();
                if (_villain != null && !_villain.IsPlayerControlled)
                {
                    _villain.RepositionToLevel(_levelManager, 60f);
                }
            }

            Debug.Log("[CutsceneController] Escape finished — next domain unlocked.");
        }

        private void SpawnPortal()
        {
            if (_portal != null) return;

            _portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _portal.name = "EscapePortal";
            _portal.transform.position = _villainStart + Vector3.up * 6f;
            _portal.transform.localScale = new Vector3(5f, 2f, 5f);

            Collider portalCollider = _portal.GetComponent<Collider>();
            if (portalCollider != null) Destroy(portalCollider);

            Renderer renderer = _portal.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(_portalColor);

            Light portalLight = _portal.AddComponent<Light>();

            portalLight.shadows = LightShadows.None;
            portalLight.type = LightType.Point;
            portalLight.color = _portalColor;
            portalLight.intensity = 3f;
            portalLight.range = 15f;
        }

        private void HidePortal()
        {
            if (_portal != null)
            {
                Destroy(_portal);
                _portal = null;
            }
        }
    }
}
