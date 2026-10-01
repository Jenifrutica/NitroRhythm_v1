using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NitroRhythm.Tests.EditMode
{
    /// <summary>
    /// Verifies the 4 prototype scenes exist, are registered in the Build
    /// Settings and that the narrative JSON is imported as a TextAsset.
    /// </summary>
    public class SceneSetupTests
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/00_MainMenu.unity",
            "Assets/Scenes/01_CharacterSelect.unity",
            "Assets/Scenes/02_Gameplay.unity",
            "Assets/Scenes/03_Results.unity"
        };

        [Test]
        public void FourPrototypeScenesExist()
        {
            foreach (string path in ScenePaths)
            {
                Assert.IsTrue(File.Exists(path), $"Falta la escena {path}");
            }

            Assert.AreEqual(4, ScenePaths.Distinct().Count());
        }

        [Test]
        public void ScenesAreRegisteredInBuildSettings()
        {
            var buildScenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            foreach (string path in ScenePaths)
            {
                Assert.Contains(path, buildScenes, $"La escena {path} no está en Build Settings.");
            }
        }

        [Test]
        public void NarrativeDatabaseIsImported()
        {
            TextAsset json = Resources.Load<TextAsset>("NitroRhythm/prototype_data");
            Assert.IsNotNull(json, "prototype_data.json debe existir en Resources.");
            StringAssert.Contains("\"percusalia\"", json.text);
            StringAssert.Contains("\"lyra\"", json.text);
        }

        [Test]
        public void BlenderModelsAreImported()
        {
            string[] models = { "Kart_Neon_01", "Kart_Neon_02", "Kart_Neon_03", "Piloto_Neon_01", "PremioMalo" };
            foreach (string model in models)
            {
                GameObject prefab = Resources.Load<GameObject>($"NitroRhythm/Models/{model}");
                Assert.IsNotNull(prefab, $"El modelo {model} no se encontró en Resources/NitroRhythm/Models.");
            }
        }
    }
}
