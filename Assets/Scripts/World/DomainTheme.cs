using UnityEngine;

namespace NitroRhythm.World
{
    public enum PropKind
    {
        Drum, Dune, SpikeCluster, Torre, GlassTower, FloatingIsland, Ring, CrystalCluster,
        HarpTree, TrebleClef, Cloud, Spire, Monolith, CrimsonSpire, SpikeBall
    }

    public enum ParticleKind { Dust, Sparks, Embers, Fireflies, Wisps, Stars, Notes }

    /// <summary>One kind of scenery prop and how densely it is scattered around the track.</summary>
    public struct PropSpec
    {
        public PropKind Kind;
        public float PerHundred;     // instances per 100 m of level
        public float MinDistance;    // distance from the track edge
        public float MaxDistance;
        public float MinScale;
        public float MaxScale;
        public float MinHeight;      // vertical offset range (floating props)
        public float MaxHeight;
        public Color Tint;
        public Color Emission;
        public int Variant;          // e.g. which Torre model

        public PropSpec(PropKind kind, float perHundred, float minDist, float maxDist, float minScale, float maxScale,
            float minHeight, float maxHeight, Color tint, Color emission, int variant = 0)
        {
            Kind = kind; PerHundred = perHundred; MinDistance = minDist; MaxDistance = maxDist;
            MinScale = minScale; MaxScale = maxScale; MinHeight = minHeight; MaxHeight = maxHeight;
            Tint = tint; Emission = emission; Variant = variant;
        }
    }

    /// <summary>Everything that makes a domain look and feel different (sky, light, fog, floor, props, particles, post).</summary>
    public class DomainTheme
    {
        public string id = "default";

        // Sky / atmosphere
        public string panorama;                 // file in Resources/NitroRhythm/Skies (null = procedural sky)
        public Color skyTint = new Color(0.3f, 0.4f, 0.8f);
        public Color groundColor = Color.black;
        public float exposure = 1.2f;
        public float atmosphere = 1f;
        public Color fogColor = new Color(0.1f, 0.1f, 0.2f);
        public float fogDensity = 0.008f;
        public Color ambient = new Color(0.3f, 0.3f, 0.4f);

        // Sun / key light
        public Color lightColor = Color.white;
        public float lightIntensity = 1.2f;
        public Vector3 lightEuler = new Vector3(40f, -40f, 0f);

        // Track
        public string platformPattern = "glass";
        public Color platformColor = new Color(0.1f, 0.12f, 0.2f);
        public Color platformEmission = Color.cyan;
        public float platformGlow = 1.2f;
        public Color accent = Color.cyan;
        public Color hazardColor = new Color(1f, 0.4f, 0.1f);

        // Post-processing
        public float bloom = 0.9f;
        public float saturation = 10f;
        public Color colorFilter = Color.white;
        public float vignette = 0.3f;

        // Scenery
        public ParticleKind particles = ParticleKind.Dust;
        public Color particleColor = Color.white;
        public PropSpec[] props = new PropSpec[0];

        public static DomainTheme Current { get; private set; }

        public static void SetCurrent(DomainTheme theme) { Current = theme; }

        private static Color C(float r, float g, float b) => new Color(r, g, b);
        private static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out Color c); return c; }

        public static DomainTheme Get(string id)
        {
            switch (id)
            {
                case "percusalia":
                    return new DomainTheme
                    {
                        id = id, panorama = "qwantani_dusk_2_puresky", skyTint = H("#FBBF24"), groundColor = H("#3A2208"), exposure = 1f,
                        fogColor = H("#B4631A"), fogDensity = 0.0075f, ambient = C(0.55f, 0.38f, 0.22f),
                        lightColor = C(1f, 0.78f, 0.48f), lightIntensity = 1.5f, lightEuler = new Vector3(28f, 135f, 0f),
                        platformPattern = "sand", platformColor = H("#8A6232"), platformEmission = H("#FBBF24"), platformGlow = 1.1f,
                        accent = H("#FBBF24"), hazardColor = H("#DC2626"),
                        bloom = 0.8f, saturation = 18f, colorFilter = C(1f, 0.93f, 0.82f), vignette = 0.28f,
                        particles = ParticleKind.Dust, particleColor = H("#FFD27A"),
                        props = new[]
                        {
                            new PropSpec(PropKind.Drum, 7f, 6f, 28f, 1.4f, 2.6f, 0f, 0f, H("#B98A3C"), H("#FBBF24")),
                            new PropSpec(PropKind.Torre, 3f, 10f, 40f, 4f, 7f, -6f, -1f, H("#FFD9A0"), H("#FBBF24"), 1),
                            new PropSpec(PropKind.SpikeCluster, 6f, 5f, 24f, 1.2f, 2.4f, -1f, 0f, H("#C98A2B"), H("#F59E0B")),
                            new PropSpec(PropKind.Dune, 4f, 40f, 130f, 14f, 38f, -22f, -8f, H("#C99A4A"), H("#4A2A08")),
                        }
                    };
                case "echoris":
                    return new DomainTheme
                    {
                        id = id, panorama = "kloppenheim_07_puresky", skyTint = H("#3B82F6"), groundColor = H("#050A1C"), exposure = 1f,
                        fogColor = H("#0E2A66"), fogDensity = 0.009f, ambient = C(0.14f, 0.22f, 0.42f),
                        lightColor = C(0.6f, 0.78f, 1f), lightIntensity = 1.05f, lightEuler = new Vector3(35f, 45f, 0f),
                        platformPattern = "glass", platformColor = H("#10204A"), platformEmission = H("#22D3EE"), platformGlow = 1.6f,
                        accent = H("#22D3EE"), hazardColor = H("#F472B6"),
                        bloom = 1.15f, saturation = 22f, colorFilter = C(0.88f, 0.96f, 1.05f), vignette = 0.34f,
                        particles = ParticleKind.Sparks, particleColor = H("#67E8F9"),
                        props = new[]
                        {
                            new PropSpec(PropKind.GlassTower, 9f, 12f, 70f, 1f, 2f, -30f, -10f, H("#16264F"), H("#22D3EE")),
                            new PropSpec(PropKind.Torre, 3f, 9f, 30f, 4f, 6.5f, -5f, -1f, H("#9FE8FF"), H("#38BDF8"), 2),
                            new PropSpec(PropKind.Ring, 2f, 25f, 60f, 8f, 16f, 6f, 30f, H("#0B1E4A"), H("#F472B6")),
                        }
                    };
                case "bassline_abyss":
                    return new DomainTheme
                    {
                        id = id, panorama = null, skyTint = H("#2A0E6B"), groundColor = H("#05010F"), exposure = 0.8f, atmosphere = 0.45f,
                        fogColor = H("#1E1058"), fogDensity = 0.011f, ambient = C(0.18f, 0.12f, 0.34f),
                        lightColor = C(0.6f, 0.5f, 1f), lightIntensity = 0.95f, lightEuler = new Vector3(32f, 40f, 0f),
                        platformPattern = "void", platformColor = H("#1B0F40"), platformEmission = H("#A78BFA"), platformGlow = 1.5f,
                        accent = H("#A78BFA"), hazardColor = H("#F43F5E"),
                        bloom = 1.2f, saturation = 20f, colorFilter = C(0.9f, 0.85f, 1.08f), vignette = 0.42f,
                        particles = ParticleKind.Embers, particleColor = H("#C4B5FD"),
                        props = new[]
                        {
                            new PropSpec(PropKind.FloatingIsland, 6f, 14f, 75f, 5f, 15f, -14f, 28f, H("#2A1A5E"), H("#7C3AED")),
                            new PropSpec(PropKind.Ring, 4f, 20f, 55f, 7f, 16f, 4f, 26f, H("#12082E"), H("#A78BFA")),
                            new PropSpec(PropKind.Torre, 2f, 10f, 34f, 4f, 7f, -6f, -1f, H("#B9A4FF"), H("#8B5CF6"), 3),
                        }
                    };
                case "arpeggion":
                    return new DomainTheme
                    {
                        id = id, panorama = null, skyTint = H("#10B981"), groundColor = H("#02130E"), exposure = 1f, atmosphere = 1.4f,
                        fogColor = H("#065F46"), fogDensity = 0.01f, ambient = C(0.16f, 0.34f, 0.28f),
                        lightColor = C(0.72f, 1f, 0.86f), lightIntensity = 1.15f, lightEuler = new Vector3(42f, 50f, 0f),
                        platformPattern = "crystal", platformColor = H("#0E3B2E"), platformEmission = H("#6EE7B7"), platformGlow = 1.4f,
                        accent = H("#6EE7B7"), hazardColor = H("#F59E0B"),
                        bloom = 1.1f, saturation = 24f, colorFilter = C(0.9f, 1.06f, 0.98f), vignette = 0.32f,
                        particles = ParticleKind.Fireflies, particleColor = H("#A7F3D0"),
                        props = new[]
                        {
                            new PropSpec(PropKind.CrystalCluster, 11f, 5f, 40f, 1.4f, 3.4f, -2f, 0f, H("#0F5C46"), H("#34D399")),
                            new PropSpec(PropKind.HarpTree, 5f, 8f, 36f, 2.2f, 4.2f, -2f, 0f, H("#064E3B"), H("#6EE7B7")),
                            new PropSpec(PropKind.TrebleClef, 1.5f, 14f, 40f, 4f, 7f, 5f, 16f, H("#B8FFE6"), H("#6EE7B7")),
                            new PropSpec(PropKind.FloatingIsland, 2.5f, 25f, 80f, 6f, 14f, -12f, 20f, H("#0B3D2E"), H("#10B981")),
                        }
                    };
                case "treble_spire":
                    return new DomainTheme
                    {
                        id = id, panorama = "kloofendal_48d_partly_cloudy_puresky", skyTint = H("#93C5FD"), groundColor = H("#C7D2FE"), exposure = 0.95f,
                        fogColor = H("#9DB8EA"), fogDensity = 0.0042f, ambient = C(0.42f, 0.5f, 0.68f),
                        lightColor = C(1f, 0.95f, 0.82f), lightIntensity = 1.25f, lightEuler = new Vector3(48f, 40f, 0f),
                        platformPattern = "cloud", platformColor = H("#8FA6D8"), platformEmission = H("#FDE68A"), platformGlow = 0.9f,
                        accent = H("#FBBF24"), hazardColor = H("#F472B6"),
                        bloom = 0.45f, saturation = 16f, colorFilter = C(0.96f, 0.98f, 1.02f), vignette = 0.3f,
                        particles = ParticleKind.Wisps, particleColor = new Color(1f, 1f, 1f, 0.5f),
                        props = new[]
                        {
                            new PropSpec(PropKind.Cloud, 10f, 14f, 100f, 7f, 20f, -26f, 10f, H("#D8E4FF"), H("#7F9BD6")),
                            new PropSpec(PropKind.Spire, 4f, 12f, 60f, 6f, 16f, -12f, -2f, H("#C9D6F2"), H("#FDE68A")),
                            new PropSpec(PropKind.TrebleClef, 2f, 14f, 50f, 5f, 9f, 6f, 24f, H("#FFF3C4"), H("#FBBF24")),
                            new PropSpec(PropKind.FloatingIsland, 3f, 25f, 90f, 6f, 15f, -16f, 14f, H("#E3EAFF"), H("#93C5FD")),
                        }
                    };
                case "noctua_chord":
                    return new DomainTheme
                    {
                        id = id, panorama = "qwantani_moonrise_puresky", skyTint = H("#111827"), groundColor = H("#02030A"), exposure = 0.9f,
                        fogColor = H("#0B1024"), fogDensity = 0.0085f, ambient = C(0.16f, 0.16f, 0.32f),
                        lightColor = C(0.62f, 0.62f, 1f), lightIntensity = 0.85f, lightEuler = new Vector3(30f, 30f, 0f),
                        platformPattern = "stars", platformColor = H("#10142B"), platformEmission = H("#FCD34D"), platformGlow = 1.8f,
                        accent = H("#FCD34D"), hazardColor = H("#8B5CF6"),
                        bloom = 1.25f, saturation = 18f, colorFilter = C(0.92f, 0.92f, 1.08f), vignette = 0.45f,
                        particles = ParticleKind.Stars, particleColor = H("#FDE68A"),
                        props = new[]
                        {
                            new PropSpec(PropKind.TrebleClef, 4.5f, 12f, 50f, 3.5f, 8f, 3f, 22f, H("#FFE9A8"), H("#FCD34D")),
                            new PropSpec(PropKind.Ring, 2.5f, 22f, 60f, 6f, 14f, 5f, 28f, H("#0C0F26"), H("#8B5CF6")),
                            new PropSpec(PropKind.FloatingIsland, 3f, 22f, 85f, 5f, 13f, -14f, 20f, H("#1A1B3F"), H("#6366F1")),
                            new PropSpec(PropKind.Torre, 2f, 9f, 30f, 4f, 6.5f, -5f, -1f, H("#B4B9FF"), H("#818CF8"), 4),
                        }
                    };
                case "void_crescendo":
                    return new DomainTheme
                    {
                        id = id, panorama = null, skyTint = H("#1F0505"), groundColor = Color.black, exposure = 0.6f, atmosphere = 0.4f,
                        fogColor = H("#1A0303"), fogDensity = 0.012f, ambient = C(0.2f, 0.07f, 0.07f),
                        lightColor = C(1f, 0.32f, 0.26f), lightIntensity = 0.95f, lightEuler = new Vector3(22f, 200f, 0f),
                        platformPattern = "cracks", platformColor = H("#14090A"), platformEmission = H("#EF4444"), platformGlow = 2.2f,
                        accent = H("#EF4444"), hazardColor = H("#FBBF24"),
                        bloom = 1.4f, saturation = 24f, colorFilter = C(1.08f, 0.9f, 0.9f), vignette = 0.5f,
                        particles = ParticleKind.Embers, particleColor = H("#FB7185"),
                        props = new[]
                        {
                            new PropSpec(PropKind.CrimsonSpire, 9f, 8f, 70f, 6f, 20f, -14f, -2f, H("#220808"), H("#DC2626")),
                            new PropSpec(PropKind.Monolith, 3f, 14f, 55f, 8f, 16f, -8f, -1f, H("#0B0506"), H("#EF4444")),
                            new PropSpec(PropKind.SpikeBall, 3f, 12f, 40f, 2.2f, 4.2f, 2f, 14f, H("#FFFFFF"), H("#FF3B30")),
                            new PropSpec(PropKind.Ring, 2f, 24f, 60f, 8f, 16f, 6f, 30f, H("#120405"), H("#EF4444")),
                        }
                    };
                default:
                    return new DomainTheme { id = "default" };
            }
        }
    }
}
