using UnityEngine.SceneManagement;

namespace NitroRhythm.Core
{
    /// <summary>Centralised scene names and navigation helpers.</summary>
    public static class SceneFlow
    {
        public const string MainMenu = "00_MainMenu";
        public const string CharacterSelect = "01_CharacterSelect";
        public const string Gameplay = "02_Gameplay";
        public const string Results = "03_Results";

        public static readonly string[] BuildOrder = { MainMenu, CharacterSelect, Gameplay, Results };

        public static void LoadMainMenu() => Load(MainMenu);
        public static void LoadCharacterSelect() => Load(CharacterSelect);
        public static void LoadGameplay() => Load(Gameplay);
        public static void LoadResults() => Load(Results);

        public static void Load(string sceneName)
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SceneFlow] Scene '{sceneName}' is not in Build Settings; loading {MainMenu} instead.");
                SceneManager.LoadScene(MainMenu);
            }
        }
    }
}
