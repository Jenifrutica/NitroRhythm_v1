using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Builds the neon 3D stage used by the menu and character-select scenes:
    /// a reflective-looking grid floor, a fog-tinted void, coloured rim lights
    /// and a soft key light, so the karts are showcased as real 3D models.
    /// </summary>
    public static class StageFactory
    {
        private static Texture2D _gridTexture;

        public static GameObject CreateFloor(Color gridColor, Color voidColor, float size = 60f)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "StageFloor";
            floor.transform.localScale = new Vector3(size * 0.1f, 1f, size * 0.1f);

            Renderer renderer = floor.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateGridMaterial(gridColor, voidColor);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = voidColor;
            RenderSettings.fogDensity = 0.016f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.13f, 0.11f, 0.22f);

            return floor;
        }

        public static Material CreateGridMaterial(Color gridColor, Color voidColor)
        {
            Material baseMaterial = Resources.Load<Material>("NitroRhythm/Materials/BaseUnlit");

            Shader shader = baseMaterial != null && baseMaterial.shader != null
                ? baseMaterial.shader
                : (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));

            Material material = new Material(shader);
            Texture2D grid = GetGridTexture();

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", grid);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", grid);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", gridColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", gridColor);

            material.mainTextureScale = new Vector2(6f, 6f);
            material.mainTexture = grid;
            return material;
        }

        public static Texture2D GetGridTexture()
        {
            if (_gridTexture != null) return _gridTexture;

            const int size = 256;
            const int cells = 8;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color dark = new Color(0.015f, 0.01f, 0.04f, 1f);
            Color line = new Color(1f, 1f, 1f, 1f);
            int step = size / cells;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onLine = x % step == 0 || y % step == 0;
                    texture.SetPixel(x, y, onLine ? line : dark);
                }
            }

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            _gridTexture = texture;
            return texture;
        }

        /// <summary>Adds a coloured point light for neon rim lighting.</summary>
        public static Light AddRimLight(Transform parent, Vector3 localPosition, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject("RimLight");
            if (parent != null) lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        /// <summary>Adds a soft directional key light for model shading.</summary>
        public static Light AddKeyLight(Transform parent, float intensity = 1.0f)
        {
            GameObject lightObject = new GameObject("KeyLight");
            if (parent != null) lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.8f, 0.86f, 1f);
            light.intensity = intensity;
            return light;
        }
    }
}
