using System;
using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>The 4 selectable game modes for the NitroRhythm speedrun.</summary>
    public enum GameMode
    {
        SinglePlayer = 0,
        SinglePlayerAndBot = 1,
        TwoPlayerCoop = 2,
        ThreePlayerShowdown = 3
    }

    /// <summary>
    /// Stores the selected game mode at startup and notifies listeners (mainly the
    /// TestSceneBootstrapper) so the right players, cameras and villain AI spawn.
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public GameMode SelectedMode { get; private set; } = GameMode.TwoPlayerCoop;
        public bool HasSelectedMode { get; private set; }

        public event Action<GameMode> ModeChosen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SelectMode(GameMode mode)
        {
            SelectedMode = mode;
            HasSelectedMode = true;
            Debug.Log($"[GameModeManager] Selected mode: {mode}");
            ModeChosen?.Invoke(mode);
        }

        /// <summary>How many playable karts a mode spawns.</summary>
        public static int GetPlayerCount(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.SinglePlayer:
                    return 1;
                case GameMode.SinglePlayerAndBot:
                case GameMode.TwoPlayerCoop:
                    return 2;
                case GameMode.ThreePlayerShowdown:
                    return 3;
                default:
                    return 1;
            }
        }
    }
}