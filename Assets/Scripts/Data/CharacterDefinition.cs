using System;
using UnityEngine;

namespace NitroRhythm.Data
{
    /// <summary>
    /// Serializable definition of a playable or antagonistic character.
    /// Loaded from the prototype JSON database so artists/designers can tune
    /// palette, stats and model anchors without recompiling.
    /// </summary>
    [Serializable]
    public class CharacterDefinition
    {
        public string id = "lyra";
        public string displayName = "Lyra Pulse";
        public string role = "hero";        // "hero" | "villain"
        public string colorHex = "#33CCFF";
        public string accentHex = "#FFFFFF";
        public int speed = 5;
        public int grip = 5;
        public int power = 5;
        public string kartModel = "Kart_Neon_01";
        public string pilotModel = "Piloto_Neon_01";
        public string texture = "Lyra";
        public string description = "";

        /// <summary>
        /// Local offset of the pilot inside the showcase pivot. Each FBX exports with a
        /// different origin, so this is tuned per character (measured by
        /// CharacterDisplayAlignmentTests).
        /// </summary>
        public Vector3 pilotOffset = new Vector3(0f, 0.45f, 0f);

        /// <summary>Extra yaw (degrees) of the showcase 3/4 view, for models exported facing another way.</summary>
        public float modelYawOffset = 0f;

        /// <summary>Multiplier on the pilot's size (Lyra's lotus pose makes her bounding box huge, so she needs more).</summary>
        public float pilotScale = 1f;

        public bool IsVillain => string.Equals(role, "villain", StringComparison.OrdinalIgnoreCase);

        public Color Color => ParseHex(colorHex, Color.cyan);
        public Color Accent => ParseHex(accentHex, Color.white);

        /// <summary>Normalized (0..1) stat for UI bars.</summary>
        public float StatSpeed01 => Mathf.Clamp01(speed / 10f);
        public float StatGrip01 => Mathf.Clamp01(grip / 10f);
        public float StatPower01 => Mathf.Clamp01(power / 10f);

        public static Color ParseHex(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : fallback;
        }
    }
}
