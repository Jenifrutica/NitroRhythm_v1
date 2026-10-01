using System;
using UnityEngine;

namespace NitroRhythm.Data
{
    /// <summary>
    /// Serializable definition of one of the 10 musical domains.
    /// Playable domains (1..7) feed the procedural LevelManager; cinematic
    /// domains (intro / crossroad / finale) drive 3D animated transitions.
    /// </summary>
    [Serializable]
    public class LevelDefinition
    {
        public int index;
        public string id = "level";
        public string displayName = "Dominio";
        public string skyHex = "#111827";
        public string fogHex = "#1F2937";
        public string groundHex = "#374151";
        public string accentHex = "#FBBF24";
        public float trackWidth = 12f;
        public int difficulty = 1;
        public bool isPlayable = true;
        public bool isCinematic = false;
        public string musicHint = "";
        public string description = "";

        public Color Sky => CharacterDefinition.ParseHex(skyHex, Color.blue);
        public Color Fog => CharacterDefinition.ParseHex(fogHex, Color.gray);
        public Color Ground => CharacterDefinition.ParseHex(groundHex, Color.gray);
        public Color Accent => CharacterDefinition.ParseHex(accentHex, Color.yellow);
    }
}
