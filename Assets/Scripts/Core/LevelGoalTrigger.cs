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

        private readonly HashSet<PlayerKartController> _insidePlayers = new HashSet<PlayerKartController>();
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
                CurrentWaitingMessage = "";
                return;
            }

            if (_levelManager.Players.Count == 0) return;
            if (CutsceneController.Instance != null && CutsceneController.Instance.IsPlaying) return;

            UpdateWaitingMessage();

            if (AllActivePlayersInside())
            {
                StartLevelEscape();
            }
        }

        private void UpdateWaitingMessage()
        {
            if (_levelManager.Players.Count <= 1)
            {
                CurrentWaitingMessage = "";
                return;
            }

            System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
            foreach (PlayerKartController player in _levelManager.Players)
            {
                if (!_insidePlayers.Contains(player))
                {
                    missing.Add(DisplayName(player));
                }
            }

            CurrentWaitingMessage = missing.Count switch
            {
                0 => "",
                1 => $"Esperando a {missing[0]}...",
                _ => $"Esperando a {string.Join(" & ", missing)}..."
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
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerKartController kart = other.GetComponentInParent<PlayerKartController>();
            if (kart != null)
            {
                _insidePlayers.Remove(kart);
            }
        }

        private bool AllActivePlayersInside()
        {
            foreach (PlayerKartController player in _levelManager.Players)
            {
                if (!_insidePlayers.Contains(player)) return false;
            }
            return true;
        }

        private void StartLevelEscape()
        {
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
