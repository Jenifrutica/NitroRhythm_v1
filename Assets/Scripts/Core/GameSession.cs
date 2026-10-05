using UnityEngine;
using NitroRhythm.Audio;
using NitroRhythm.Data;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Cross-scene session state (persists via DontDestroyOnLoad).
    /// Holds the selected game mode, character picks, audio-reactive settings
    /// and the optional pre-analysed music result that the procedural track
    /// builder consumes in the gameplay scene.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Match Setup")]
        public GameMode Mode = GameMode.TwoPlayerCoop;

        [Header("Character Picks (character ids from prototype_data.json)")]
        public string P1CharacterId = "lyra";
        public string P2CharacterId = "karel";
        public string P3CharacterId = "vox";

        [Header("Audio Reactive")]
        public bool UseAudioReactive = true;
        public string AudioTrackPath = "";
        [Range(0f, 1f)] public float MusicVolume = 0.8f;
        [Range(0f, 1f)] public float SfxVolume = 0.8f;
        public float AudioCalibrationMs = 0f;

        [Header("Progress")]
        public int StartLevelIndex = 1;

        /// <summary>Playable domain being played (1-based). Each domain is its own gameplay screen.</summary>
        public int CurrentLevelIndex = 1;

        /// <summary>Running totals across levels (the HUD continues from these, results show them).</summary>
        /// <summary>The first-level "how to play" screen is shown once per run.</summary>
        public bool ControlsGuideShown;

        public int LastScore;
        public float LastTime;

        /// <summary>Totals at the start of the current level, so "restart" can rewind to them.</summary>
        public int LevelStartScore;
        public float LevelStartTime;

        /// <summary>Starts a fresh run at the first domain with zeroed totals.</summary>
        public void BeginRun()
        {
            ControlsGuideShown = false;
            CurrentLevelIndex = Mathf.Max(1, StartLevelIndex);
            LastScore = 0;
            LastTime = 0f;
            LevelStartScore = 0;
            LevelStartTime = 0f;
        }

        /// <summary>Called when a level screen starts so a restart can rewind to this point.</summary>
        public void MarkLevelStart()
        {
            LevelStartScore = LastScore;
            LevelStartTime = LastTime;
        }

        /// <summary>Rewinds the totals to the start of the current level (restart).</summary>
        public void RestartLevel()
        {
            LastScore = LevelStartScore;
            LastTime = LevelStartTime;
        }

        /// <summary>Moves to the next domain; false when the last domain was completed.</summary>
        public bool AdvanceLevel()
        {
            int total = Mathf.Max(1, PrototypeData.Instance.PlayableCount);
            if (CurrentLevelIndex >= total) return false;
            CurrentLevelIndex++;
            return true;
        }

        /// <summary>Pre-analysed music (null when using the built-in generated track).</summary>
        public AudioAnalysisResult Analysis;

        public static GameSession EnsureExists()
        {
            if (Instance != null) return Instance;

            GameSession existing = FindObjectOfType<GameSession>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("GameSession");
            Instance = go.AddComponent<GameSession>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public CharacterDefinition GetCharacter(int playerSlot)
        {
            string id = P1CharacterId;
            if (playerSlot == 2) id = P2CharacterId;
            else if (playerSlot == 3) id = P3CharacterId;

            // Player 2 must never be the same pilot as player 1.
            if (playerSlot == 2 && id == P1CharacterId)
            {
                foreach (CharacterDefinition other in PrototypeData.Instance.characters)
                {
                    if (!other.IsVillain && other.id != P1CharacterId) { id = other.id; break; }
                }
            }

            CharacterDefinition def = PrototypeData.Instance.GetCharacter(id);
            if (def == null && PrototypeData.Instance.characters.Length > 0)
            {
                def = PrototypeData.Instance.characters[0];
            }
            return def;
        }

        public CharacterDefinition GetVillain()
        {
            PrototypeData data = PrototypeData.Instance;
            if (data.characters == null) return null;
            foreach (CharacterDefinition c in data.characters)
            {
                if (c != null && c.IsVillain) return c;
            }
            return data.GetCharacter("vox");
        }
    }
}
