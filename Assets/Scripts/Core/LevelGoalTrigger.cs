using System.Collections.Generic;
using UnityEngine;
using NitroRhythm.Narrative;
using NitroRhythm.Player;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Goal trigger at the end of each level. In co-op / split-screen modes ALL
    /// active players must enter the checkpoint zone before the level completes;
    /// a waiting HUD message (exposed as <see cref="CurrentWaitingMessage"/>)
    /// shows which player is still behind. When everyone is inside, the 2D
    /// villain-escape cutscene starts.
    /// </summary>
    public class LevelGoalTrigger : MonoBehaviour
    {
        /// <summary>The level this goal belongs to (set by LevelManager at build time).</summary>
        public int LevelNumber { get; set; } = 1;

        /// <summary>Message shown by the HUD when waiting for the other player(s).</summary>
        public static string CurrentWaitingMessage { get; private set; } = "";

        /// <summary>Clears the (static) co-op waiting message when a new level screen starts.</summary>
        public static void ResetMessage() => CurrentWaitingMessage = "";

        /// <summary>Seconds the other players get to arrive once the first one has reached the goal.</summary>
        public const float GraceSeconds = 6f;

        // Arrivals are remembered (not tracked frame by frame): at race speed a kart crosses the goal zone in a
        // fraction of a second, so two players are almost never inside it at the same instant.
        private readonly HashSet<PlayerKartController> _insidePlayers = new HashSet<PlayerKartController>();
        private float _graceTimer;
        private LevelManager _levelManager;

        private void Start()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            _levelManager = FindObjectOfType<LevelManager>();
        }

        private void Update()
        {
            if (_levelManager == null)
            {
                _levelManager = FindObjectOfType<LevelManager>();
                return;
            }

            if (_levelManager.CurrentLevel != LevelNumber)
            {
                _insidePlayers.Clear();
                _graceTimer = 0f;
                CurrentWaitingMessage = "";
                return;
            }

            if (_levelManager.Players.Count == 0) return;
            if (CutsceneController.Instance != null && CutsceneController.Instance.IsPlaying) return;

            UpdateWaitingMessage();

            if (_insidePlayers.Count > 0) _graceTimer += Mathf.Min(Time.unscaledDeltaTime, 0.1f);

            if (AllActivePlayersInside() || _graceTimer >= GraceSeconds)
            {
                StartLevelEscape();
            }
        }

        private void UpdateWaitingMessage()
        {
            // Only meaningful once somebody has reached the goal and the others are still racing.
            if (_levelManager.Players.Count <= 1 || _insidePlayers.Count == 0)
            {
                CurrentWaitingMessage = "";
                return;
            }

            System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
            foreach (PlayerKartController player in _levelManager.Players)
            {
                if (!IsOut(player) && !_insidePlayers.Contains(player))
                {
                    missing.Add(DisplayName(player));
                }
            }

            CurrentWaitingMessage = missing.Count switch
            {
                0 => "",
                1 => $"Esperando a {missing[0]}...  {Mathf.CeilToInt(GraceSeconds - _graceTimer)}",
                _ => $"Esperando a {string.Join(" & ", missing)}...  {Mathf.CeilToInt(GraceSeconds - _graceTimer)}"
            };
        }

        private static string DisplayName(PlayerKartController player)
        {
            if (player == null) return "Jugador";

            switch (player.Scheme)
            {
                case PlayerKartController.InputScheme.PlayerOne: return "Jugador 1";
                case PlayerKartController.InputScheme.PlayerTwo: return "Jugador 2";
                case PlayerKartController.InputScheme.PlayerThree: return "Villano (P3)";
                case PlayerKartController.InputScheme.Bot: return "Bot";
                default: return player.name;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_levelManager == null) return;

            PlayerKartController kart = other.GetComponentInParent<PlayerKartController>();
            if (kart == null || !_levelManager.Players.Contains(kart)) return;

            _insidePlayers.Add(kart);
            kart.ControlLocked = true;   // hold the kart on the goal platform while the others arrive
        }

        private void OnTriggerExit(Collider other)
        {
            // Intentionally empty: an arrival stays counted even if the kart rolls out of the zone.
        }

        /// <summary>A player that is K.O. or has fallen off the track cannot be waited for.</summary>
        private static bool IsOut(PlayerKartController player)
        {
            if (player == null || player.transform.position.y < -5f) return true;
            KartHealthSpeed health = player.GetComponent<KartHealthSpeed>();
            return health != null && health.IsDown;
        }

        private bool AllActivePlayersInside()
        {
            if (_insidePlayers.Count == 0) return false;
            foreach (PlayerKartController player in _levelManager.Players)
            {
                if (IsOut(player)) continue;
                if (!_insidePlayers.Contains(player)) return false;
            }
            return true;
        }

        private void StartLevelEscape()
        {
            NitroRhythm.Audio.SfxPlayer.Play(NitroRhythm.Audio.SfxLibrary.Goal, 0.8f);
            if (CutsceneController.Instance != null)
            {
                CutsceneController.Instance.BeginLevelEscape(LevelNumber);
            }
            else
            {
                _levelManager.CompleteLevel(LevelNumber);
            }
        }
    }
}
