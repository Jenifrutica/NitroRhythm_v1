using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NitroRhythm.Tests
{
    /// <summary>
    /// Genera capturas SIMULADAS del "estado anterior" del prototipo (antes de los
    /// ajustes): interfaz plana, karts y pista de cubos, HUD básico en texto. Sirve
    /// para la comparativa antes/después de AA3/AA4 cuando no hay capturas reales
    /// del prototipo original. Solo corre con NITRO_SHOT_DIR definido.
    /// </summary>
    public class LegacyEvidenceCapture
    {
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator CaptureLegacyBefore()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("NITRO_SHOT_DIR no definido: captura 'antes' omitida.");
            }

            Directory.CreateDirectory(dir);

            GameObject root = BuildLegacyMenu();
            yield return Shot(dir, "legacy_menu.png");
            Object.Destroy(root);
            yield return null;

            root = BuildLegacySelect();
            yield return Shot(dir, "legacy_select.png");
            Object.Destroy(root);
            yield return null;

            root = BuildLegacyTrack();
            yield return Shot(dir, "legacy_track.png");
            Object.Destroy(root);
            yield return null;

            root = BuildLegacyGameplay();
            yield return Shot(dir, "legacy_gameplay.png");
            Object.Destroy(root);
            yield return null;
        }

        // ------------------------------------------------------------- builders

        private static GameObject BuildLegacyMenu()
        {
            GameObject root = new GameObject("LegacyMenu");
            GameObject cam = NewCamera(root, "MenuCam");
            cam.transform.position = new Vector3(0f, 2.2f, -9f);
            cam.transform.LookAt(new Vector3(0f, 1f, 0f));

            Canvas canvas = NewCanvas(root, "MenuCanvas");
            Panel(canvas.transform, "Bg", new Color(0.10f, 0.10f, 0.13f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Label(canvas.transform, "Title", "NITRO RHYTHM", 44, new Color(0.75f, 0.75f, 0.78f), new Vector2(0f, 120f));
            Label(canvas.transform, "Sub", "Prototipo (version inicial)", 18, new Color(0.6f, 0.6f, 0.62f), new Vector2(0f, 70f));
            PlainButton(canvas.transform, "JUGAR", 20f);
            PlainButton(canvas.transform, "OPCIONES", -30f);
            PlainButton(canvas.transform, "SALIR", -80f);
            return root;
        }

        private static GameObject BuildLegacySelect()
        {
            GameObject root = new GameObject("LegacySelect");
            GameObject cam = NewCamera(root, "SelectCam");
            cam.transform.position = new Vector3(0f, 1.8f, -8f);
            cam.transform.LookAt(new Vector3(0f, 0.8f, 0f));

            Canvas canvas = NewCanvas(root, "SelectCanvas");
            Panel(canvas.transform, "Bg", new Color(0.10f, 0.10f, 0.13f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Label(canvas.transform, "Title", "SELECCIONA TU PILOTO", 30, new Color(0.8f, 0.8f, 0.82f), new Vector2(0f, 180f));

            Color[] colors = { new Color(0.2f, 0.8f, 1f), new Color(0f, 0.15f, 0.7f), Color.red };
            string[] names = { "Player 1", "Player 2", "Villano" };
            for (int i = 0; i < 3; i++)
            {
                float x = -280f + i * 280f;
                Panel(canvas.transform, "Card" + i, new Color(0.18f, 0.18f, 0.22f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(200f, 200f));
                Panel(canvas.transform, "Box" + i, colors[i], new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 40f), new Vector2(170f, 110f));
                Label(canvas.transform, "Name" + i, names[i], 18, Color.white, new Vector2(x, -70f));
            }
            return root;
        }

        private static GameObject BuildLegacyTrack()
        {
            GameObject root = new GameObject("LegacyTrack");
            GameObject cam = NewCamera(root, "TrackCam");
            cam.transform.position = new Vector3(6f, 6f, -14f);
            cam.transform.LookAt(new Vector3(18f, 0f, 0f));

            Color grey = new Color(0.42f, 0.42f, 0.50f);
            for (int i = 0; i < 8; i++)
            {
                Cube(root, "Platform" + i, new Vector3(i * 9f, -0.25f, 0f), new Vector3(6f, 0.5f, 8f), grey);
            }
            Cube(root, "Kart", new Vector3(1f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 3.8f), new Color(0.2f, 0.8f, 1f));
            return root;
        }

        private static GameObject BuildLegacyGameplay()
        {
            GameObject root = new GameObject("LegacyGameplay");
            GameObject cam = NewCamera(root, "GameplayCam");
            cam.transform.position = new Vector3(0f, 3.2f, -8f);
            cam.transform.LookAt(new Vector3(0f, 1f, 0f));

            Color grey = new Color(0.42f, 0.42f, 0.50f);
            Cube(root, "Platform", new Vector3(0f, -0.25f, 0f), new Vector3(10f, 0.5f, 8f), grey);
            Cube(root, "Platform2", new Vector3(12f, -0.25f, 0f), new Vector3(6f, 0.5f, 8f), grey);
            Cube(root, "KartP1", new Vector3(-1.6f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 3.8f), new Color(0.2f, 0.8f, 1f));
            Cube(root, "KartP2", new Vector3(1.6f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 3.8f), new Color(0f, 0.15f, 0.7f));

            // HUD básico (texto plano + barra gris), como el prototipo inicial.
            Canvas canvas = NewCanvas(root, "HudCanvas");
            Label(canvas.transform, "Score", "PUNTUACION 000000", 22, Color.white, new Vector2(-560f, 300f), TextAlignmentOptions.TopLeft);
            Label(canvas.transform, "Domain", "DOMINIO 1/7", 18, new Color(0.8f, 0.8f, 0.6f), new Vector2(-500f, 262f), TextAlignmentOptions.TopLeft);
            Label(canvas.transform, "Speed", "0 km/h", 20, Color.white, new Vector2(-580f, -300f), TextAlignmentOptions.TopLeft);
            Panel(canvas.transform, "BarBg", new Color(0.25f, 0.25f, 0.28f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 22f));
            Panel(canvas.transform, "BarFill", new Color(0.6f, 0.6f, 0.65f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 40f), new Vector2(300f, 22f));
            return root;
        }

        // -------------------------------------------------------------- helpers

        private static GameObject NewCamera(GameObject root, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            cam.fieldOfView = 55f;
            return go;
        }

        private static Canvas NewCanvas(GameObject root, string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(root.transform, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static Image Panel(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            RectTransform r = img.rectTransform;
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.sizeDelta = size; r.anchoredPosition = pos;
            return img;
        }

        private static void PlainButton(Transform parent, string label, float y)
        {
            Panel(parent, "Btn_" + label, new Color(0.20f, 0.20f, 0.24f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(360f, 46f));
            Label(parent, "BtnTxt_" + label, label, 20, new Color(0.85f, 0.85f, 0.87f), new Vector2(0f, y));
        }

        private static void Label(Transform parent, string name, string text, float size, Color color, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            t.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(700f, 40f);
            t.rectTransform.anchoredPosition = pos;
        }

        private static void Cube(GameObject root, string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && shader != null) { Material m = new Material(shader); m.color = color; r.sharedMaterial = m; }
        }

        private static IEnumerator Shot(string dir, string file, int width = 1280, int height = 720)
        {
            yield return null;

            RenderTexture rt = new RenderTexture(width, height, 24);
            Camera[] cameras = Camera.allCameras;
            System.Array.Sort(cameras, (x, y) => x.depth.CompareTo(y.depth));
            for (int i = 0; i < cameras.Length; i++)
            {
                RenderTexture previous = cameras[i].targetTexture;
                cameras[i].targetTexture = rt;
                cameras[i].Render();
                cameras[i].targetTexture = previous;
            }

            RenderTexture.active = rt;
            Texture2D scene = new Texture2D(width, height, TextureFormat.RGBA32, false);
            scene.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;

            RenderTexture uiRt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            GameObject uiCamObject = new GameObject("LegacyUiCam");
            Camera uiCam = uiCamObject.AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiCam.cullingMask = 1 << 31;
            uiCam.orthographic = true;
            uiCam.targetTexture = uiRt;

            System.Collections.Generic.List<(Transform t, int layer)> moved = new System.Collections.Generic.List<(Transform, int)>();
            foreach (Canvas canvas in canvases)
            {
                foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    moved.Add((t, t.gameObject.layer));
                    t.gameObject.layer = 31;
                }
            }
            RenderMode[] modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = uiCam;
                canvases[i].planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            uiCam.Render();
            for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
            foreach ((Transform t, int layer) in moved) if (t != null) t.gameObject.layer = layer;

            RenderTexture.active = uiRt;
            Texture2D ui = new Texture2D(width, height, TextureFormat.RGBA32, false);
            ui.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;

            Color32[] basePixels = scene.GetPixels32();
            Color32[] uiPixels = ui.GetPixels32();
            for (int i = 0; i < basePixels.Length; i++)
            {
                float a = uiPixels[i].a / 255f;
                basePixels[i] = new Color32(
                    (byte)Mathf.Min(255, uiPixels[i].r + basePixels[i].r * (1f - a)),
                    (byte)Mathf.Min(255, uiPixels[i].g + basePixels[i].g * (1f - a)),
                    (byte)Mathf.Min(255, uiPixels[i].b + basePixels[i].b * (1f - a)),
                    255);
            }
            scene.SetPixels32(basePixels);
            scene.Apply();
            File.WriteAllBytes(Path.Combine(dir, file), scene.EncodeToPNG());

            Object.Destroy(scene);
            Object.Destroy(ui);
            Object.Destroy(uiCamObject);
            rt.Release();
            uiRt.Release();
            Object.Destroy(rt);
            Object.Destroy(uiRt);
        }
    }
}
