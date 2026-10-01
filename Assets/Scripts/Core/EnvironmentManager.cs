using System;
using NitroRhythm.Data;
using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Applies the dynamic per-level neon environment themes as players unlock
    /// levels: skybox tint, fog color/density and background music pitch.
    /// Theme per level: Cyber Blue, Neon Purple, Sunset Orange, Toxic Green,
    /// Deep Space Red, Electric Yellow, Golden Void.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        [Header("Audio")]
        public AudioSource BackgroundMusic;
        [SerializeField] private float _basePitch = 1f;
        [SerializeField] private float _pitchPerLevel = 0.06f;

        [Header("Fog")]
        [SerializeField] private bool _enableFog = true;
        [SerializeField] private float _baseFogDensity = 0.010f;
        [SerializeField] private float _fogDensityPerLevel = 0.0012f;

        public int CurrentLevel { get; private set; } = 1;

        /// <summary>Raised after a theme is applied (level crossed).</summary>
        public event Action<int> LevelThemeApplied;

        private static readonly Color[] LevelThemes =
        {
            new Color(0.20f, 0.45f, 1.00f), // 1 Cyber Blue
            new Color(0.70f, 0.20f, 1.00f), // 2 Neon Purple
            new Color(1.00f, 0.45f, 0.10f), // 3 Sunset Orange
            new Color(0.30f, 1.00f, 0.25f), // 4 Toxic Green
            new Color(0.85f, 0.10f, 0.20f), // 5 Deep Space Red
            new Color(1.00f, 0.90f, 0.10f), // 6 Electric Yellow
            new Color(1.00f, 0.75f, 0.15f), // 7 Golden Void
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : 1;
            ApplyLevelTheme(level);
        }

        public void ApplyLevelTheme(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 1, LevelThemes.Length);
            int index = CurrentLevel - 1;
            Color theme = LevelThemes[index];
            Color fogColor = theme * 0.85f;

            // Prefer the colours authored in prototype_data.json for playable domains.
            LevelDefinition[] playable = PrototypeData.Instance.PlayableLevels;
            if (playable != null && playable.Length > 0)
            {
                LevelDefinition def = playable[Mathf.Clamp(index, 0, playable.Length - 1)];
                theme = def.Sky;
                fogColor = def.Fog;
            }

            // Skybox / background tint.
            Material skybox = RenderSettings.skybox;
            if (skybox == null || !skybox.HasProperty("_Tint"))
            {
                Shader procedural = Shader.Find("Skybox/Procedural");
                if (procedural != null)
                {
                    skybox = new Material(procedural);
                    skybox.SetColor("_GroundColor", theme * 0.25f);
                }
            }

            if (skybox != null && skybox.HasProperty("_Tint"))
            {
                skybox.SetColor("_Tint", theme);
                RenderSettings.skybox = skybox;
                DynamicGI.UpdateEnvironment();
            }

            // Fog color grows denser & more intense with each level.
            RenderSettings.fog = _enableFog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = _baseFogDensity + CurrentLevel * _fogDensityPerLevel;

            // Music pitch climbs to match speedrun intensity.
            if (BackgroundMusic != null)
            {
                BackgroundMusic.pitch = _basePitch + (CurrentLevel - 1) * _pitchPerLevel;
            }

            Debug.Log($"[EnvironmentManager] Level {CurrentLevel} theme applied — {theme}");
            LevelThemeApplied?.Invoke(CurrentLevel);
        }

        public void SetBackgroundMusic(AudioSource source)
        {
            BackgroundMusic = source;
        }
    }
}