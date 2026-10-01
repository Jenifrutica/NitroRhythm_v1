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

        /// <summary>Score captured when the run ends, shown in the results scene.</summary>
        public int LastScore;

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
