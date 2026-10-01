using System.Collections.Generic;
using System.IO;
using NitroRhythm.Core;
using NitroRhythm.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NitroRhythm.EditorTools
{
    /// <summary>
    /// Generates the four prototype scenes required by the GA5 evidence and
    /// registers them in the Build Settings. Re-runnable and deterministic.
    /// Menu: NitroRhythm ▸ Build Prototype Scenes.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";

        private static readonly string[] SceneOrder =
        {
            "Assets/Scenes/00_MainMenu.unity",
            "Assets/Scenes/01_CharacterSelect.unity",
            "Assets/Scenes/02_Gameplay.unity",
            "Assets/Scenes/03_Results.unity"
        };

        /// <summary>
        /// Generates the scenes automatically the first time the project is
        /// opened if they are still missing, so the prototype is ready to play.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (string path in SceneOrder)
                {
                    if (!File.Exists(path))
                    {
                        Debug.Log("[PrototypeSceneBuilder] Missing prototype scenes — generating now.");
                        BuildAll();
                        return;
                    }
                }
            };
        }

        [MenuItem("NitroRhythm/Build Prototype Scenes")]
        public static void BuildAll()
        {
            EnsureFolder(ScenesFolder);
            EnsureBaseMaterials();

            BuildMenuScene();
            BuildCharacterSelectScene();
            BuildGameplayScene();
            BuildResultsScene();

            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrototypeSceneBuilder] 4 prototype scenes generated and registered.");
        }

        private static void BuildMenuScene()
        {
            Scene scene = NewScene();
            AddCamera(new Color(0.02f, 0.02f, 0.06f));
            CreateRoot("MenuBootstrap").AddComponent<MainMenuScreen>();
            SaveScene(scene, $"{ScenesFolder}/00_MainMenu.unity");
        }

        private static void BuildCharacterSelectScene()
        {
            Scene scene = NewScene();
            AddCamera(new Color(0.02f, 0.02f, 0.06f));
            CreateRoot("SelectBootstrap").AddComponent<CharacterSelectScreen>();
            SaveScene(scene, $"{ScenesFolder}/01_CharacterSelect.unity");
        }

        private static void BuildGameplayScene()
        {
            Scene scene = NewScene();
            AddCamera(new Color(0.03f, 0.03f, 0.08f));
            AddDirectionalLight();

            GameObject bootstrapper = CreateRoot("Bootstrapper");
            bootstrapper.AddComponent<TestSceneBootstrapper>();

            SaveScene(scene, $"{ScenesFolder}/02_Gameplay.unity");
        }

        private static void BuildResultsScene()
        {
            Scene scene = NewScene();
            AddCamera(new Color(0.04f, 0.01f, 0.09f));
            CreateRoot("ResultsBootstrap").AddComponent<ResultsScreen>();
            SaveScene(scene, $"{ScenesFolder}/03_Results.unity");
        }

        // --------------------------------------------------------------- helpers

        private static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static GameObject CreateRoot(string name)
        {
            GameObject go = new GameObject(name);
            return go;
        }

        private static void AddCamera(Color background)
        {
            GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);
        }

        private static void AddDirectionalLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        /// <summary>
        /// Creates base URP materials under Resources so their shaders are
        /// guaranteed to be included in the WebGL/standalone build. Runtime
        /// code clones these instead of relying on Shader.Find (which returns
        /// stripped shaders at runtime and causes magenta materials).
        /// </summary>
        private static void EnsureBaseMaterials()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/NitroRhythm");
            EnsureFolder("Assets/Resources/NitroRhythm/Materials");
            EnsureFolder("Assets/Resources/NitroRhythm/Shaders");

            EnsureMaterial("NitroRhythm/Materials/BaseLit", "Universal Render Pipeline/Lit");
            EnsureMaterial("NitroRhythm/Materials/BaseUnlit", "Universal Render Pipeline/Unlit");
            EnsureMaterial("NitroRhythm/Materials/BaseUI", "UI/Default");

            AssetDatabase.SaveAssets();
        }

        private static void EnsureMaterial(string resourcePath, string shaderName)
        {
            string fullPath = $"Assets/Resources/{resourcePath}.mat";
            if (File.Exists(fullPath)) return;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[PrototypeSceneBuilder] Shader '{shaderName}' not found; material {resourcePath} skipped.");
                return;
            }

            Material material = new Material(shader);
            AssetDatabase.CreateAsset(material, fullPath);
        }

        private static void RegisterBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in SceneOrder)
            {
                if (File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ----------------------------------------------------------- web build

        [MenuItem("NitroRhythm/Build WebGL")]
        public static void BuildWebGL()
        {
            BuildAll();

            // Brotli with decompression fallback so the build runs on any static
            // host (itch.io, Netlify, S3) without custom Content-Encoding headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGL");
            Directory.CreateDirectory(outputDir);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = SceneOrder,
                locationPathName = outputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildPipeline.BuildPlayer(options);
            Debug.Log($"[PrototypeSceneBuilder] WebGL build finished at {outputDir}");
        }
    }
}
