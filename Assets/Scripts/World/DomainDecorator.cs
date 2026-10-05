using NitroRhythm.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NitroRhythm.World
{
    /// <summary>
    /// Dresses the current domain: sky, fog, ambient and key light, post-processing grade,
    /// scenery props scattered on both sides of the track and ambient particles.
    /// Everything is deterministic per level (seeded) so a level always looks the same.
    /// </summary>
    public class DomainDecorator : MonoBehaviour
    {
        public DomainTheme Theme { get; private set; }
        public int PropCount { get; private set; }

        private const float ClearMargin = 5f;

        public static DomainDecorator Build(LevelManager levelManager, int levelNumber, string domainId, Transform followTarget)
        {
            GameObject go = new GameObject("DomainDecorator");
            DomainDecorator decorator = go.AddComponent<DomainDecorator>();
            decorator.Apply(levelManager, levelNumber, domainId, followTarget);
            return decorator;
        }

        public void Apply(LevelManager levelManager, int levelNumber, string domainId, Transform followTarget)
        {
            Theme = DomainTheme.Get(domainId);
            DomainTheme.SetCurrent(Theme);

            ApplyAtmosphere(Theme);
            NeonPostFx.ApplyTheme(Theme.bloom, Theme.saturation, Theme.colorFilter, Theme.vignette);

            float start = levelManager.GetLevelStartX(levelNumber);
            float end = levelManager.GetLevelEndX(levelNumber);
            ScatterProps(Theme, start, end, levelManager.TrackHalfWidth, levelNumber);

            if (followTarget != null) DomainParticles.Create(Theme, followTarget).transform.SetParent(transform, true);

            // Looping ambience of the domain (wind, hum, drone...), at the effects volume.
            AudioSource ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = NitroRhythm.Audio.AmbientBed.Create(Theme.id);
            ambience.loop = true;
            ambience.spatialBlend = 0f;
            ambience.volume = 0.4f * NitroRhythm.Audio.SfxPlayer.MasterVolume;
            ambience.Play();
            Debug.Log($"[DomainDecorator] '{Theme.id}' dressed: {PropCount} props over {end - start:F0} m.");
        }

        // ------------------------------------------------------------ atmosphere

        private static void ApplyAtmosphere(DomainTheme t)
        {
            Material sky = null;
            if (!string.IsNullOrEmpty(t.panorama))
            {
                Texture2D tex = Resources.Load<Texture2D>($"NitroRhythm/Skies/{t.panorama}");
                Material baseMat = Resources.Load<Material>("NitroRhythm/Materials/SkyPanoramic");
                Shader shader = baseMat != null ? baseMat.shader : Shader.Find("Skybox/Panoramic");
                if (tex != null && shader != null)
                {
                    tex.wrapMode = TextureWrapMode.Repeat;
                    sky = new Material(shader);
                    sky.SetTexture("_MainTex", tex);
                    if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", t.exposure);
                    if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", Color.Lerp(Color.white, t.skyTint, 0.25f));
                }
            }

            if (sky == null)
            {
                Material baseMat = Resources.Load<Material>("NitroRhythm/Materials/SkyProcedural");
                Shader shader = baseMat != null ? baseMat.shader : Shader.Find("Skybox/Procedural");
                if (shader != null)
                {
                    sky = new Material(shader);
                    sky.SetColor("_SkyTint", t.skyTint);
                    sky.SetColor("_GroundColor", t.groundColor);
                    sky.SetFloat("_Exposure", t.exposure);
                    sky.SetFloat("_AtmosphereThickness", t.atmosphere);
                    sky.SetFloat("_SunSize", 0.03f);
                }
            }

            if (sky != null) RenderSettings.skybox = sky;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = t.ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = t.fogColor;
            RenderSettings.fogDensity = t.fogDensity;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional) { sun = l; break; }
                }
            }
            if (sun == null)
            {
                GameObject sunObject = new GameObject("DomainSun");
                sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = t.lightColor;
            sun.intensity = t.lightIntensity;
            sun.transform.rotation = Quaternion.Euler(t.lightEuler);
            sun.shadows = LightShadows.None;
            RenderSettings.sun = sun;

            DynamicGI.UpdateEnvironment();
        }

        // ---------------------------------------------------------------- props

        private void ScatterProps(DomainTheme t, float start, float end, float halfWidth, int levelNumber)
        {
            System.Random rng = new System.Random(levelNumber * 7919 + 13);
            Transform parent = new GameObject("Scenery").transform;
            parent.SetParent(transform, false);

            float length = Mathf.Max(60f, end - start);

            foreach (PropSpec spec in t.props)
            {
                int count = Mathf.Max(1, Mathf.RoundToInt(spec.PerHundred * length / 100f));
                for (int i = 0; i < count; i++)
                {
                    GameObject prop = PropFactory.Create(spec, t, rng);
                    float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                    float x = Mathf.Lerp(start - 12f, end + 50f, (float)rng.NextDouble());
                    float dist = Mathf.Lerp(spec.MinDistance, spec.MaxDistance, (float)rng.NextDouble());
                    float y = Mathf.Lerp(spec.MinHeight, spec.MaxHeight, (float)rng.NextDouble());

                    float scale = Mathf.Lerp(spec.MinScale, spec.MaxScale, (float)rng.NextDouble());
                    if (prop.GetComponent<TowerMarker>() != null) scale = Mathf.Lerp(0.9f, 1.25f, (float)rng.NextDouble());
                    prop.transform.localScale = Vector3.one * scale;

                    prop.transform.SetParent(parent, false);
                    prop.transform.position = new Vector3(x, y, side * (halfWidth + dist));
                    prop.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                    // Keep the whole prop (not just its centre) clear of the track and the chase camera.
                    Renderer[] renderers = prop.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds bounds = renderers[0].bounds;
                        for (int r = 1; r < renderers.Length; r++) bounds.Encapsulate(renderers[r].bounds);
                        float clearance = halfWidth + ClearMargin + bounds.extents.z;
                        Vector3 p = prop.transform.position;
                        if (Mathf.Abs(p.z) < clearance)
                        {
                            p.z = side * clearance;
                            prop.transform.position = p;
                        }
                    }
                    PropCount++;
                }
            }
        }
    }
}
