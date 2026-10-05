using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Applies the neon cyberpunk post-processing look (Bloom, Vignette,
    /// Chromatic Aberration, colour grading and ACES tonemapping) through a
    /// global URP Volume created at runtime and enables post-processing on every
    /// camera. Idempotent, so any scene can call <see cref="Ensure"/>.
    /// </summary>
    public static class NeonPostFx
    {
        private static Volume _volume;

        public static void Ensure()
        {
            if (_volume == null)
            {
                Volume existing = Object.FindObjectOfType<Volume>();
                if (existing != null)
                {
                    _volume = existing;
                }
                else
                {
                    GameObject volumeObject = new GameObject("Global Neon Volume");
                    _volume = volumeObject.AddComponent<Volume>();
                    _volume.isGlobal = true;
                    _volume.priority = 10f;
                }

                if (_volume.sharedProfile == null)
                {
                    _volume.sharedProfile = BuildProfile();
                }
            }

            EnableOnCameras();
        }

        /// <summary>Per-domain grading: bloom strength, saturation, colour filter and vignette.</summary>
        public static void ApplyTheme(float bloomIntensity, float saturation, Color colorFilter, float vignetteIntensity)
        {
            Ensure();
            if (_volume == null || _volume.sharedProfile == null) return;

            VolumeProfile profile = _volume.sharedProfile;
            if (profile.TryGet(out Bloom bloom)) bloom.intensity.value = bloomIntensity;
            if (profile.TryGet(out ColorAdjustments adjustments))
            {
                adjustments.saturation.overrideState = true;
                adjustments.saturation.value = saturation;
                adjustments.colorFilter.overrideState = true;
                adjustments.colorFilter.value = colorFilter;
            }
            if (profile.TryGet(out Vignette vignette)) vignette.intensity.value = vignetteIntensity;
        }

        private static VolumeProfile BuildProfile()
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.7f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.0f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.55f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(0.7f, 0.85f, 1f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.36f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.55f;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.02f, 0.0f, 0.06f);

            ChromaticAberration aberration = profile.Add<ChromaticAberration>(true);
            aberration.intensity.overrideState = true;
            aberration.intensity.value = 0.14f;

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.contrast.overrideState = true;
            color.contrast.value = 18f;
            color.saturation.overrideState = true;
            color.saturation.value = 22f;
            color.postExposure.overrideState = true;
            color.postExposure.value = 0.15f;

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            return profile;
        }

        private static void EnableOnCameras()
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            foreach (Camera camera in cameras)
            {
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    data.renderPostProcessing = true;
                }
            }
        }
    }
}
