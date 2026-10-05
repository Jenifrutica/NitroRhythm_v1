using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace NitroRhythm.EditorTools
{
    /// <summary>
    /// Builds the TextMeshPro font assets from the free fonts in Assets/Fonts:
    /// Orbitron (titles, OFL), Rajdhani (UI, OFL) and Material Icons (icons, Apache 2.0).
    /// Run: -executeMethod NitroRhythm.EditorTools.FontAssetBuilder.Build
    /// </summary>
    public static class FontAssetBuilder
    {
        private const string OutDir = "Assets/Resources/NitroRhythm/Fonts";

        // Latin + Spanish punctuation/accents + a few symbols used by the UI.
        private const string TextChars =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "áéíóúüñÁÉÍÓÚÜÑ¡¿·—–…°×•«»";

        // Material Icons codepoints used by the UI (see MaterialIcons.codepoints).
        public static readonly uint[] IconCodepoints =
        {
            0xe9e4, 0xe425, 0xe153, 0xe87d, 0xea0b, 0xe80e, 0xea23, 0xe405, 0xe8b8, 0xe037, 0xe034, 0xe042, 0xe88a,
            0xe7fd, 0xe7ef, 0xe838, 0xe5ca, 0xe5cd, 0xe050, 0xe429, 0xe531, 0xe9e0, 0xef55, 0xe5c8, 0xf06e, 0xf20c,
            0xe01d, 0xea3f, 0xeac2, 0xe1d5, 0xe7af, 0xeb9b, 0xe55d, 0xea28, 0xe03d, 0xe1b8, 0xe019, 0xe068, 0xe31b,
        };

        [MenuItem("NitroRhythm/Build Font Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(OutDir);
            AssetDatabase.Refresh();

            MakeTextFont("Assets/Fonts/Orbitron.ttf", "Orbitron SDF");
            MakeTextFont("Assets/Fonts/Rajdhani-SemiBold.ttf", "Rajdhani SDF");
            MakeTextFont("Assets/Fonts/Rajdhani-Bold.ttf", "Rajdhani Bold SDF");
            MakeIconFont("Assets/Fonts/MaterialIcons.ttf", "MaterialIcons SDF");

            MakeGlowMaterial("Orbitron SDF");
            MakeGlowMaterial("Rajdhani SDF");
            MakeGlowMaterial("Rajdhani Bold SDF");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FontAssetBuilder] Font assets created in " + OutDir);
        }

        private static TMP_FontAsset Create(string ttfPath, string assetName)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[FontAssetBuilder] Missing font {ttfPath}");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = assetName;
            return asset;
        }

        private static void Save(TMP_FontAsset asset, string assetName)
        {
            string path = $"{OutDir}/{assetName}.asset";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(asset, path);
            if (asset.material != null)
            {
                asset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    if (asset.atlasTextures[i] == null) continue;
                    asset.atlasTextures[i].name = $"{assetName} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
                }
            }
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// A copy of the font material with GLOW and OUTLINE keywords enabled. Keeping it as an asset in
        /// Resources makes the build keep those shader variants (otherwise they are stripped).
        /// </summary>
        private static void MakeGlowMaterial(string fontAssetName)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{OutDir}/{fontAssetName}.asset");
            if (font == null || font.material == null) return;

            Material glow = new Material(font.material);
            glow.name = fontAssetName + " Glow";
            glow.EnableKeyword("GLOW_ON");
            glow.EnableKeyword("OUTLINE_ON");
            glow.SetFloat("_OutlineWidth", 0.12f);
            glow.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0.85f));
            glow.SetColor("_GlowColor", new Color(0.2f, 0.8f, 1f, 0.6f));
            glow.SetFloat("_GlowPower", 0.45f);
            glow.SetFloat("_GlowOuter", 0.5f);

            string path = $"{OutDir}/{fontAssetName} Glow.mat";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(glow, path);
        }

        private static void MakeTextFont(string ttfPath, string assetName)
        {
            TMP_FontAsset asset = Create(ttfPath, assetName);
            if (asset == null) return;
            asset.TryAddCharacters(TextChars, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[FontAssetBuilder] {assetName} is missing: {missing}");
            Save(asset, assetName);
            Debug.Log($"[FontAssetBuilder] {assetName}: {asset.characterTable.Count} glyphs.");
        }

        private static void MakeIconFont(string ttfPath, string assetName)
        {
            TMP_FontAsset asset = Create(ttfPath, assetName);
            if (asset == null) return;
            asset.TryAddCharacters(IconCodepoints, out uint[] missing);
            if (missing != null && missing.Length > 0) Debug.LogWarning($"[FontAssetBuilder] {assetName}: {missing.Length} icon codepoints missing.");
            Save(asset, assetName);
            Debug.Log($"[FontAssetBuilder] {assetName}: {asset.characterTable.Count} icons.");
        }
    }
}
