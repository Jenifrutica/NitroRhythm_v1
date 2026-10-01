using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NitroRhythm.EditorTools
{
    /// <summary>
    /// Evidence helper for AA3/AA4. Gives quick access to open each prototype
    /// scene and capture the Game View as a PNG into an "Antes"/"Despues"
    /// folder, so the before/after infographic can be assembled easily.
    /// In Play mode the capture includes the Canvas UI; in Edit mode it renders
    /// the main camera (3D only).
    /// </summary>
    public class EvidenceCaptureTool : EditorWindow
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/00_MainMenu.unity",
            "Assets/Scenes/01_CharacterSelect.unity",
            "Assets/Scenes/02_Gameplay.unity",
            "Assets/Scenes/03_Results.unity"
        };

        private string _folder = "Antes";
        private string _lastCapture = "";

        [MenuItem("NitroRhythm/Evidence Capture Tool")]
        public static void Open()
        {
            GetWindow<EvidenceCaptureTool>("Evidencias GA5");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Captura de evidencias (AA3 / AA4)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Abrir escena:", EditorStyles.boldLabel);
            foreach (string path in ScenePaths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"Abrir  {name}", GUILayout.Width(240f)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(path);
                    }
                }
                EditorGUILayout.LabelField(path);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            _folder = EditorGUILayout.TextField("Carpeta destino", _folder);
            EditorGUILayout.HelpBox(
                "Sugerencia: captura primero en 'Antes', aplica los ajustes y vuelve a capturar en 'Despues'.\n" +
                "En Play Mode la captura incluye la interfaz Canvas.",
                MessageType.Info);

            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0.4f, 0.9f, 1f);
            if (GUILayout.Button("📸  Capturar Game View", GUILayout.Height(40f)))
            {
                Capture();
            }
            GUI.backgroundColor = Color.white;

            if (!string.IsNullOrEmpty(_lastCapture))
            {
                EditorGUILayout.HelpBox($"Guardado en:\n{_lastCapture}", MessageType.None);
            }
        }

        private void Capture()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string directory = Path.Combine(projectRoot, "Evidencias", _folder);
            Directory.CreateDirectory(directory);

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "scene";
            string fileName = $"{sceneName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(directory, fileName);

            if (Application.isPlaying)
            {
                ScreenCapture.CaptureScreenshot(fullPath);
                _lastCapture = fullPath;
            }
            else
            {
                RenderEditMode(fullPath);
            }

            Debug.Log($"[EvidenceCaptureTool] Captured '{sceneName}' → {fullPath}");
        }

        private void RenderEditMode(string fullPath)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                if (cameras.Length > 0) camera = cameras[0];
            }

            if (camera == null)
            {
                EditorUtility.DisplayDialog("Evidencias", "No hay cámara en la escena para renderizar.", "OK");
                return;
            }

            const int width = 1920;
            const int height = 1080;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            RenderTexture previous = camera.targetTexture;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            camera.targetTexture = previous;
            RenderTexture.active = null;

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            DestroyImmediate(renderTexture);
            DestroyImmediate(texture);
            _lastCapture = fullPath;
        }
    }
}
