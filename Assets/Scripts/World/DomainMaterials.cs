using System.Collections.Generic;
using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>
    /// Materials and procedurally generated textures for the domain themes
    /// (tile patterns, window grids, soft particle dots). Everything is cached.
    /// </summary>
    public static class DomainMaterials
    {
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

        public static void ClearCaches()
        {
            TextureCache.Clear();
            MaterialCache.Clear();
        }

        // ------------------------------------------------------------ materials

        /// <summary>Lit material with a texture, a tint and an emissive accent.</summary>
        public static Material Textured(Texture2D albedo, Color tint, Texture2D emissionMap, Color emission, float smoothness = 0.35f)
        {
            Material m = VisualEntityFactory.CreateSolidMaterial(tint);
            if (albedo != null)
            {
                m.mainTexture = albedo;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", albedo);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                if (emissionMap != null && m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", emissionMap);
                m.SetColor("_EmissionColor", emission);
            }
            return m;
        }

        /// <summary>Plain colour with emission (for rings, crystals, neon strips).</summary>
        public static Material Glow(Color baseColor, Color emission, float intensity = 1.5f)
        {
            return VisualEntityFactory.CreateNeonMaterial(baseColor, emission, intensity);
        }

        /// <summary>Material for a user model: the JPG from Resources/Textures/Props + a glow derived from it.</summary>
        public static Material ModelMaterial(string textureName, Color tint, float emissionStrength)
        {
            string key = $"model:{textureName}:{tint}:{emissionStrength}";
            if (MaterialCache.TryGetValue(key, out Material cached) && cached != null) return cached;

            Texture2D tex = Resources.Load<Texture2D>($"NitroRhythm/Textures/Props/{textureName}");
            Material m = Textured(tex, tint, tex, Color.white * emissionStrength, 0.5f);
            MaterialCache[key] = m;
            return m;
        }

        /// <summary>Platform surface for a domain: stylised tile pattern + glowing seams.</summary>
        public static Material Platform(DomainTheme theme, float length, float width)
        {
            Texture2D albedo = PatternTexture(theme, false);
            Texture2D emission = PatternTexture(theme, true);
            Material m = Textured(albedo, Color.white, emission, theme.platformEmission * theme.platformGlow, 0.45f);
            Vector2 tiling = new Vector2(Mathf.Max(1f, length / 6f), Mathf.Max(1f, width / 6f));
            m.mainTextureScale = tiling;
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_EmissionMap")) m.SetTextureScale("_EmissionMap", tiling);
            return m;
        }

        /// <summary>Shared additive particle material (URP Particles/Unlit from Resources so it survives builds).</summary>
        public static Material Particle(Color tint)
        {
            Material baseMat = Resources.Load<Material>("NitroRhythm/Materials/BaseParticles");
            Material m;
            if (baseMat != null)
            {
                m = new Material(baseMat);
            }
            else
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                m = new Material(s);
            }
            ConfigureAdditive(m);
            Texture2D dot = SoftDot();
            m.mainTexture = dot;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", dot);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            m.color = tint;
            return m;
        }

        /// <summary>
        /// Sets a URP Particles/Unlit material to transparent + additive (the same values the material
        /// inspector writes). The Editor also bakes this into Resources/Materials/BaseParticles so the
        /// shader variant survives the build.
        /// </summary>
        public static void ConfigureAdditive(Material m)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 2f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // ------------------------------------------------------------ textures

        public static Texture2D SoftDot()
        {
            if (TextureCache.TryGetValue("dot", out Texture2D cached) && cached != null) return cached;
            const int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "SoftDot" };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            TextureCache["dot"] = t;
            return t;
        }

        /// <summary>Square tile (128 px) with the theme pattern; emission=true gives only the glowing lines/dots.</summary>
        public static Texture2D PatternTexture(DomainTheme theme, bool emission)
        {
            string key = $"pat:{theme.id}:{emission}";
            if (TextureCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = key };
            System.Random rng = new System.Random(theme.id.GetHashCode());
            Color baseColor = theme.platformColor;
            Color line = theme.accent;
            Color[] pixels = new Color[n * n];

            // Crack segments (random walks) for the "cracks" pattern.
            bool[] crack = new bool[n * n];
            if (theme.platformPattern == "cracks")
            {
                for (int c = 0; c < 6; c++)
                {
                    float x = (float)rng.NextDouble() * n, y = (float)rng.NextDouble() * n, a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    for (int s = 0; s < 90; s++)
                    {
                        crack[((int)y % n + n) % n * n + ((int)x % n + n) % n] = true;
                        a += ((float)rng.NextDouble() - 0.5f) * 0.9f;
                        x += Mathf.Cos(a); y += Mathf.Sin(a);
                    }
                }
            }

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.09f + theme.id.Length, y * 0.09f) * 0.5f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.25f;
                    bool isLine = false;
                    float soft = 0f;

                    switch (theme.platformPattern)
                    {
                        case "glass":      // square grid
                            isLine = x % 32 < 2 || y % 32 < 2;
                            break;
                        case "sand":       // brick seams
                            isLine = y % 32 < 2 || (x + (y / 32 % 2) * 32) % 64 < 2;
                            break;
                        case "void":       // hex-ish diagonal lattice
                            isLine = (x + y) % 40 < 2 || (x - y + n * 4) % 40 < 2;
                            break;
                        case "crystal":    // facets
                            isLine = (x + y * 2) % 48 < 2 || (x * 2 - y + n * 4) % 48 < 2;
                            break;
                        case "cloud":      // soft puffs, no hard lines
                            soft = Mathf.PerlinNoise(x * 0.05f + 20f, y * 0.05f) > 0.62f ? 0.5f : 0f;
                            break;
                        case "stars":      // sparse dots
                            isLine = ((x * 73856093) ^ (y * 19349663)) % 211 == 0 || (x % 64 < 1 && y % 64 < 1);
                            break;
                        case "cracks":
                            isLine = crack[y * n + x];
                            break;
                    }

                    if (emission)
                    {
                        float v = isLine ? 1f : soft;
                        pixels[y * n + x] = new Color(line.r * v, line.g * v, line.b * v, 1f);
                    }
                    else
                    {
                        float shade = 0.8f + noise * 0.4f;
                        Color c = baseColor * shade;
                        if (theme.platformPattern == "cloud") c = Color.Lerp(c, Color.white, soft * 0.6f);
                        if (isLine) c = Color.Lerp(c, line, 0.35f);
                        c.a = 1f;
                        pixels[y * n + x] = c;
                    }
                }
            }

            t.SetPixels(pixels);
            t.Apply();
            TextureCache[key] = t;
            return t;
        }

        /// <summary>Window-grid texture for the neon skyscrapers (emission map with random lit windows).</summary>
        public static Texture2D WindowTexture(Color lit, bool emission, int seed)
        {
            string key = $"win:{lit}:{emission}:{seed}";
            if (TextureCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = key };
            System.Random rng = new System.Random(seed);
            bool[,] on = new bool[8, 16];
            for (int i = 0; i < 8; i++) for (int j = 0; j < 16; j++) on[i, j] = rng.NextDouble() < 0.55;

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int cx = x / 16, cy = y / 8;
                    bool inWindow = x % 16 > 3 && x % 16 < 13 && y % 8 > 1 && y % 8 < 7;
                    bool windowOn = inWindow && on[cx, cy];
                    if (emission)
                    {
                        Color c = windowOn ? lit : Color.black;
                        c.a = 1f;
                        t.SetPixel(x, y, c);
                    }
                    else
                    {
                        Color c = inWindow ? new Color(0.06f, 0.1f, 0.2f) : new Color(0.04f, 0.05f, 0.1f);
                        c.a = 1f;
                        t.SetPixel(x, y, c);
                    }
                }
            }
            t.Apply();
            TextureCache[key] = t;
            return t;
        }

        /// <summary>Chevron arrows texture for speed pads (emission).</summary>
        public static Texture2D ChevronTexture(Color color)
        {
            string key = $"chev:{color}";
            if (TextureCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
            const int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = key };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x - n / 2f);
                    float d = (y - dx * 0.9f) % 22f;
                    bool arrow = d > 0f && d < 7f && y > 4 && y < n - 4;
                    t.SetPixel(x, y, arrow ? new Color(color.r, color.g, color.b, 1f) : new Color(0.02f, 0.02f, 0.04f, 1f));
                }
            }
            t.Apply();
            TextureCache[key] = t;
            return t;
        }
    }
}
