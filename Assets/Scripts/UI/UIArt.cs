using System.Collections.Generic;
using UnityEngine;

namespace NitroRhythm.UI
{
    /// <summary>Loads the interface art kept in Resources (character portraits, domain illustrations).</summary>
    public static class UIArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static Sprite Load(string path)
        {
            if (Cache.TryGetValue(path, out Sprite cached) && cached != null) return cached;

            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[path] = sprite;
            return sprite;
        }

        /// <summary>3:4 portrait of a character, cropped from the concept-art sheets.</summary>
        public static Sprite Portrait(string characterId) => Load($"NitroRhythm/UI/Portraits/{characterId}");

        /// <summary>
        /// The portrait with its corners cut to a rounded rectangle (baked into the alpha channel, so it needs no
        /// stencil mask: UI masks interfered with 3D rendering in the WebGL build).
        /// </summary>
        public static Sprite RoundedPortrait(string characterId, float cornerFraction = 0.12f)
            => Rounded($"NitroRhythm/UI/Portraits/{characterId}", cornerFraction);

        /// <summary>The domain illustration with rounded corners baked into its alpha.</summary>
        public static Sprite RoundedDomain(string domainId, float cornerFraction = 0.06f)
            => Rounded($"NitroRhythm/UI/Domains/{domainId}", cornerFraction);

        private static Sprite Rounded(string resourcePath, float cornerFraction)
        {
            string key = $"rounded:{resourcePath}:{cornerFraction}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Texture2D source = Resources.Load<Texture2D>(resourcePath);
            if (source == null) return null;

            // Copy through a RenderTexture: the imported texture is not readable in a build.
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            int w = copy.width, h = copy.height;
            float r = Mathf.Min(w, h) * cornerFraction;
            Color32[] pixels = copy.GetPixels32();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Distance outside the rounded rectangle (negative inside), 1 px soft edge.
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - r), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - r), 0f);
                    float outside = Mathf.Sqrt(dx * dx + dy * dy) - r;
                    float coverage = Mathf.Clamp01(0.5f - outside);
                    if (coverage < 1f)
                    {
                        int i = y * w + x;
                        pixels[i].a = (byte)(pixels[i].a * coverage);
                    }
                }
            }
            copy.SetPixels32(pixels);
            copy.Apply(false, false);
            copy.filterMode = FilterMode.Bilinear;

            Sprite sprite = Sprite.Create(copy, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Concept-art illustration of a domain (isometric board).</summary>
        public static Sprite Domain(string domainId) => Load($"NitroRhythm/UI/Domains/{domainId}");
    }
}
