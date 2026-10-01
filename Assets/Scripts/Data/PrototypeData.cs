using System;
using UnityEngine;

namespace NitroRhythm.Data
{
    /// <summary>
    /// Root of the flat JSON narrative/design database shipped in
    /// Assets/Resources/NitroRhythm/prototype_data.json.
    /// Loads synchronously (works on WebGL) and exposes lookups used by the
    /// menu, character select, track builder and cutscene system.
    /// </summary>
    [Serializable]
    public class PrototypeData
    {
        public string gameTitle = "NITRO RHYTHM";
        public string tagline = "Velocidad sintética, frecuencia neón y vectores letales.";
        public CharacterDefinition[] characters = new CharacterDefinition[0];
        public LevelDefinition[] levels = new LevelDefinition[0];
        public CutsceneDefinition[] cutscenes = new CutsceneDefinition[0];

        private static PrototypeData _instance;

        public static PrototypeData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public static PrototypeData Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("NitroRhythm/prototype_data");
            if (asset == null)
            {
                Debug.LogWarning("[PrototypeData] prototype_data.json not found — using built-in defaults.");
                return CreateFallback();
            }

            try
            {
                PrototypeData data = JsonUtility.FromJson<PrototypeData>(asset.text);
                return data ?? CreateFallback();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PrototypeData] Failed to parse JSON: {e.Message}");
                return CreateFallback();
            }
        }

        public CharacterDefinition GetCharacter(string id)
        {
            if (characters == null) return null;
            foreach (CharacterDefinition c in characters)
            {
                if (c != null && c.id == id) return c;
            }
            return null;
        }

        public LevelDefinition GetLevel(string id)
        {
            if (levels == null) return null;
            foreach (LevelDefinition l in levels)
            {
                if (l != null && l.id == id) return l;
            }
            return null;
        }

        public LevelDefinition GetLevelByIndex(int index)
        {
            if (levels == null) return null;
            foreach (LevelDefinition l in levels)
            {
                if (l != null && l.index == index) return l;
            }
            return null;
        }

        public CutsceneDefinition GetCutscene(string id)
        {
            if (cutscenes == null) return null;
            foreach (CutsceneDefinition c in cutscenes)
            {
                if (c != null && c.id == id) return c;
            }
            return null;
        }

        /// <summary>The 7 playable domains, ordered by index.</summary>
        public LevelDefinition[] PlayableLevels
        {
            get
            {
                if (levels == null) return new LevelDefinition[0];
                System.Collections.Generic.List<LevelDefinition> list = new System.Collections.Generic.List<LevelDefinition>();
                foreach (LevelDefinition l in levels)
                {
                    if (l != null && l.isPlayable && !l.isCinematic) list.Add(l);
                }
                list.Sort((a, b) => a.index.CompareTo(b.index));
                return list.ToArray();
            }
        }

        public int PlayableCount
        {
            get
            {
                int count = 0;
                if (levels == null) return 0;
                foreach (LevelDefinition l in levels)
                {
                    if (l != null && l.isPlayable && !l.isCinematic) count++;
                }
                return count;
            }
        }

        private static PrototypeData CreateFallback()
        {
            PrototypeData data = new PrototypeData();
            data.characters = new[]
            {
                new CharacterDefinition { id = "lyra", displayName = "Lyra Pulse", role = "hero", colorHex = "#33CCFF", accentHex = "#6CB6B5", speed = 8, grip = 9, power = 6, kartModel = "Kart_Neon_01" },
                new CharacterDefinition { id = "karel", displayName = "Karel Volt", role = "hero", colorHex = "#0022B2", accentHex = "#5080A0", speed = 7, grip = 7, power = 8, kartModel = "Kart_Neon_02" },
                new CharacterDefinition { id = "vox", displayName = "Vox Null", role = "villain", colorHex = "#FF0000", accentHex = "#783C3C", speed = 9, grip = 6, power = 10, kartModel = "Kart_Neon_03" },
            };
            data.levels = new[]
            {
                new LevelDefinition { index = 0, id = "conservatorio", displayName = "Conservatorio Armónico", isPlayable = false, isCinematic = true, difficulty = 0 },
                new LevelDefinition { index = 1, id = "percusalia", displayName = "Percusalia", isPlayable = true, difficulty = 1 },
                new LevelDefinition { index = 2, id = "echoris", displayName = "Echoris", isPlayable = true, difficulty = 2 },
                new LevelDefinition { index = 3, id = "bassline_abyss", displayName = "Bassline Abyss", isPlayable = true, difficulty = 3 },
                new LevelDefinition { index = 4, id = "arpeggion", displayName = "Arpeggion", isPlayable = true, difficulty = 4 },
                new LevelDefinition { index = 5, id = "treble_spire", displayName = "Treble Spire", isPlayable = true, difficulty = 5 },
                new LevelDefinition { index = 6, id = "noctua_chord", displayName = "Noctua Chord", isPlayable = true, difficulty = 6 },
                new LevelDefinition { index = 7, id = "void_crescendo", displayName = "Void Crescendo", isPlayable = true, difficulty = 7 },
                new LevelDefinition { index = 8, id = "encrucijada", displayName = "Encrucijada", isPlayable = false, isCinematic = true, difficulty = 8 },
                new LevelDefinition { index = 9, id = "harmonya", displayName = "Renacimiento de Harmonya", isPlayable = false, isCinematic = true, difficulty = 9 },
            };
            data.FillFallbackCutscenes();
            return data;
        }

        private void FillFallbackCutscenes()
        {
            cutscenes = new[]
            {
                new CutsceneDefinition { id = "intro", title = "CONSERVATORIO ARMÓNICO", speaker = "vox", taunt = "El silencio se acerca... ¡y ustedes no podrán detenerlo!", lines = new[] { "Vox Null corrompe la armonía del reino.", "Lyra y Karel despiertan sus karts." }, nextLevelId = "percusalia" }
            };
        }
    }
}
