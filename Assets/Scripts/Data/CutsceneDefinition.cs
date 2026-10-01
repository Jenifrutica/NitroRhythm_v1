using System;

namespace NitroRhythm.Data
{
    /// <summary>
    /// Serializable 2D comic-style cutscene card: villain taunt, background
    /// colour, speaker character id and one or more dialogue lines.
    /// </summary>
    [Serializable]
    public class CutsceneDefinition
    {
        public string id = "cutscene";
        public string title = "";
        public string speaker = "vox";
        public string backgroundHex = "#7F1D1D";
        public string taunt = "";
        public string[] lines = new string[0];
        public string nextLevelId = "";
    }
}
