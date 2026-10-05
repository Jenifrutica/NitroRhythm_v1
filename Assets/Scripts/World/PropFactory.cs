using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>
    /// Builds scenery props: the user's own models (Torre*, TrebleClef, SpikeBall, Prize*) plus props
    /// assembled from procedural meshes. Every prop is created at the origin, base on y = 0, without
    /// colliders; the caller positions and scales the returned root.
    /// </summary>
    public static class PropFactory
    {
        public static GameObject Create(PropSpec spec, DomainTheme theme, System.Random rng)
        {
            GameObject root = new GameObject($"Prop_{spec.Kind}");
            switch (spec.Kind)
            {
                case PropKind.Drum: BuildDrum(root, spec); break;
                case PropKind.Dune: BuildDune(root, spec, rng); break;
                case PropKind.SpikeCluster: BuildSpikes(root, spec, rng); break;
                case PropKind.Torre: BuildTorre(root, spec); break;
                case PropKind.GlassTower: BuildGlassTower(root, spec, rng); break;
                case PropKind.FloatingIsland: BuildIsland(root, spec, rng); break;
                case PropKind.Ring: BuildRing(root, spec); break;
                case PropKind.CrystalCluster: BuildCrystals(root, spec, rng); break;
                case PropKind.HarpTree: BuildHarp(root, spec); break;
                case PropKind.TrebleClef: BuildModel(root, "TrebleClef", spec, 1f, true); Animate(root, new Vector3(0f, 25f, 0f), 0.6f, 0.5f); break;
                case PropKind.Cloud: BuildCloud(root, spec, rng); break;
                case PropKind.Spire: BuildSpire(root, spec); break;
                case PropKind.Monolith: BuildMonolith(root, spec); break;
                case PropKind.CrimsonSpire: BuildCrimsonSpire(root, spec, rng); break;
                case PropKind.SpikeBall: BuildModel(root, "SpikeBall", spec, 1f, false); Animate(root, new Vector3(20f, 45f, 10f), 0.8f, 0.6f); break;
            }
            return root;
        }

        // ------------------------------------------------------------------ helpers

        private static float Rand(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        private static GameObject Part(Transform parent, string name, Mesh mesh, Material material, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static GameObject Prim(Transform parent, PrimitiveType type, string name, Material material, Vector3 pos, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static PropAnimator Animate(GameObject root, Vector3 spin, float bobAmp, float bobSpeed)
        {
            PropAnimator a = root.AddComponent<PropAnimator>();
            a.Spin = spin;
            a.BobAmplitude = bobAmp;
            a.BobSpeed = bobSpeed;
            return a;
        }

        /// <summary>Instantiates a Resources FBX, fits it to unit height (or unit size) and puts its base at the origin.</summary>
        private static GameObject BuildModel(GameObject root, string modelName, PropSpec spec, float targetSize, bool byHeight, string textureName = null, float emission = 0.8f)
        {
            GameObject prefab = Resources.Load<GameObject>($"NitroRhythm/Props/{modelName}");
            if (prefab == null)
            {
                Debug.LogWarning($"[PropFactory] Model '{modelName}' not found.");
                Prim(root.transform, PrimitiveType.Cube, "Missing", DomainMaterials.Glow(spec.Tint, spec.Emission), Vector3.up * 0.5f, Vector3.one);
                return root;
            }

            GameObject model = NitroRhythm.Core.ModelSanitizer.Strip(Object.Instantiate(prefab, root.transform));
            model.name = modelName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Material material = DomainMaterials.ModelMaterial(textureName ?? modelName, spec.Tint, emission);
            foreach (Renderer r in renderers)
            {
                Material[] shared = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < shared.Length; i++) shared[i] = material;
                r.sharedMaterials = shared;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Collider c = r.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }

            Bounds b = Combined(renderers);
            float size = byHeight ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float k = targetSize / Mathf.Max(0.0001f, size);
            model.transform.localScale = Vector3.one * k;

            b = Combined(renderers);
            Vector3 shift = new Vector3(b.center.x, b.min.y, b.center.z) - root.transform.position;
            model.transform.position -= shift;
            return root;
        }

        private static Bounds Combined(Renderer[] renderers)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // ------------------------------------------------------------------- props

        private static void BuildDrum(GameObject root, PropSpec spec)
        {
            Material body = DomainMaterials.Glow(spec.Tint, spec.Emission, 0.25f);
            Material skin = DomainMaterials.Glow(Color.Lerp(spec.Tint, Color.white, 0.45f), spec.Emission, 1.4f);
            Material rim = DomainMaterials.Glow(spec.Tint * 0.6f, spec.Emission, 2f);

            Prim(root.transform, PrimitiveType.Cylinder, "Body", body, new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 0.5f, 1.6f));
            GameObject top = Prim(root.transform, PrimitiveType.Cylinder, "Skin", skin, new Vector3(0f, 1.02f, 0f), new Vector3(1.52f, 0.025f, 1.52f));
            Mesh torus = ProceduralMeshes.Torus(0.8f, 0.05f, 24, 6);
            Part(root.transform, "RimTop", torus, rim, new Vector3(0f, 1f, 0f), new Vector3(90f, 0f, 0f), Vector3.one);
            Part(root.transform, "RimBottom", torus, rim, new Vector3(0f, 0.04f, 0f), new Vector3(90f, 0f, 0f), Vector3.one);

            PropAnimator a = root.AddComponent<PropAnimator>();
            a.ScalePulse = 0.08f;
            a.EmissionPulse = 1.6f;
            a.GlowRenderers = new[] { top.GetComponent<Renderer>() };
        }

        private static void BuildDune(GameObject root, PropSpec spec, System.Random rng)
        {
            Mesh rock = ProceduralMeshes.Rock(1f, rng.Next(1, 99), false);
            Part(root.transform, "Dune", rock, DomainMaterials.Glow(spec.Tint, spec.Emission, 0.15f), Vector3.zero, new Vector3(0f, rng.Next(0, 360), 0f), new Vector3(1.6f, 0.45f, 1.1f));
        }

        private static void BuildSpikes(GameObject root, PropSpec spec, System.Random rng)
        {
            Material m = DomainMaterials.Glow(spec.Tint, spec.Emission, 0.9f);
            int n = rng.Next(3, 6);
            for (int i = 0; i < n; i++)
            {
                float a = Rand(rng, 0f, 6.28f), r = Rand(rng, 0f, 0.7f), h = Rand(rng, 1.2f, 2.8f);
                Part(root.transform, $"Spike{i}", ProceduralMeshes.Cone(0.28f, h, 6, 0.1f, rng.Next(1, 99)), m,
                    new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), new Vector3(Rand(rng, -12f, 12f), 0f, Rand(rng, -12f, 12f)), Vector3.one);
            }
        }

        private static void BuildTorre(GameObject root, PropSpec spec)
        {
            int variant = Mathf.Clamp(spec.Variant, 1, 4);
            BuildModel(root, $"Torre{variant}", spec, 1f, true, $"Torre{variant}", 0.75f);
        }

        private static void BuildGlassTower(GameObject root, PropSpec spec, System.Random rng)
        {
            float w = Rand(rng, 3.5f, 6f), d = Rand(rng, 3.5f, 6f), h = Rand(rng, 26f, 70f);
            Color[] palette = { new Color(0.2f, 0.9f, 1f), new Color(1f, 0.35f, 0.75f), new Color(0.75f, 0.85f, 1f) };
            Color lit = palette[rng.Next(palette.Length)];
            Texture2D albedo = DomainMaterials.WindowTexture(lit, false, 3);
            Texture2D glow = DomainMaterials.WindowTexture(lit, true, 3 + rng.Next(0, 3));
            Material m = DomainMaterials.Textured(albedo, Color.white, glow, Color.white * 1.7f, 0.8f);
            Vector2 tiling = new Vector2(w / 3f, h / 6f);
            m.mainTextureScale = tiling;
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_EmissionMap")) m.SetTextureScale("_EmissionMap", tiling);

            Prim(root.transform, PrimitiveType.Cube, "Tower", m, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d));
            Material cap = DomainMaterials.Glow(lit * 0.4f, lit, 2.2f);
            Prim(root.transform, PrimitiveType.Cube, "Crown", cap, new Vector3(0f, h + 0.25f, 0f), new Vector3(w * 1.04f, 0.5f, d * 1.04f));
            Part(root.transform, "Antenna", ProceduralMeshes.Cone(0.25f, 5f, 5), cap, new Vector3(0f, h + 0.5f, 0f), Vector3.zero, Vector3.one);

            // The caller scales roots uniformly; tower dimensions are already in metres.
            root.AddComponent<TowerMarker>();
        }

        private static void BuildIsland(GameObject root, PropSpec spec, System.Random rng)
        {
            Material rock = DomainMaterials.Glow(spec.Tint, spec.Emission, 0.12f);
            Material crystal = DomainMaterials.Glow(spec.Emission * 0.5f, spec.Emission, 2f);
            Part(root.transform, "Island", ProceduralMeshes.Rock(1f, rng.Next(1, 99), true), rock, new Vector3(0f, 0f, 0f), new Vector3(0f, rng.Next(0, 360), 0f), new Vector3(1.3f, 1f, 1.3f));
            int n = rng.Next(2, 5);
            for (int i = 0; i < n; i++)
            {
                float a = Rand(rng, 0f, 6.28f), r = Rand(rng, 0.1f, 0.7f);
                Part(root.transform, $"Hang{i}", ProceduralMeshes.Crystal(0.14f, Rand(rng, 0.5f, 1.2f)), crystal,
                    new Vector3(Mathf.Cos(a) * r, -0.35f, Mathf.Sin(a) * r), new Vector3(180f, 0f, Rand(rng, -15f, 15f)), Vector3.one);
            }
            Part(root.transform, "TopCrystal", ProceduralMeshes.Crystal(0.12f, Rand(rng, 0.5f, 0.9f)), crystal, new Vector3(Rand(rng, -0.3f, 0.3f), 0.18f, Rand(rng, -0.3f, 0.3f)), Vector3.zero, Vector3.one);
            Animate(root, Vector3.zero, 0.8f, 0.5f);
        }

        private static void BuildRing(GameObject root, PropSpec spec)
        {
            Material m = DomainMaterials.Glow(spec.Tint, spec.Emission, 2.4f);
            GameObject ring = Part(root.transform, "Ring", ProceduralMeshes.Torus(1f, 0.05f, 40, 8), m, new Vector3(0f, 1f, 0f), Vector3.zero, Vector3.one);
            Part(root.transform, "Inner", ProceduralMeshes.Torus(0.72f, 0.025f, 36, 6), m, new Vector3(0f, 1f, 0f), Vector3.zero, Vector3.one);
            PropAnimator a = Animate(root, new Vector3(0f, 0f, 14f), 0.6f, 0.4f);
            a.ScalePulse = 0.06f;
            a.EmissionPulse = 1.2f;
            a.GlowRenderers = new[] { ring.GetComponent<Renderer>() };
        }

        private static void BuildCrystals(GameObject root, PropSpec spec, System.Random rng)
        {
            Material m = DomainMaterials.Glow(spec.Tint, spec.Emission, 1.6f);
            int n = rng.Next(3, 7);
            for (int i = 0; i < n; i++)
            {
                float a = Rand(rng, 0f, 6.28f), r = Rand(rng, 0f, 0.6f);
                Part(root.transform, $"Crystal{i}", ProceduralMeshes.Crystal(Rand(rng, 0.15f, 0.3f), Rand(rng, 0.9f, 2.2f)), m,
                    new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), new Vector3(Rand(rng, -22f, 22f), Rand(rng, 0f, 360f), Rand(rng, -22f, 22f)), Vector3.one);
            }
        }

        private static void BuildHarp(GameObject root, PropSpec spec)
        {
            Material frame = DomainMaterials.Glow(spec.Tint, spec.Emission, 1.2f);
            Material strings = DomainMaterials.Glow(Color.white, spec.Emission, 2.6f);
            Part(root.transform, "Frame", ProceduralMeshes.Torus(1f, 0.06f, 28, 6, 180f), frame, Vector3.zero, Vector3.zero, Vector3.one);
            for (int i = -3; i <= 3; i++)
            {
                float x = i * 0.25f;
                float h = Mathf.Sqrt(Mathf.Max(0.02f, 1f - x * x));
                Prim(root.transform, PrimitiveType.Cylinder, $"String{i}", strings, new Vector3(x, h * 0.5f, 0f), new Vector3(0.018f, h * 0.5f, 0.018f));
            }
            Animate(root, Vector3.zero, 0.05f, 1.4f);
        }

        private static void BuildCloud(GameObject root, PropSpec spec, System.Random rng)
        {
            Material m = DomainMaterials.Glow(spec.Tint, spec.Emission, 0.35f);
            int n = rng.Next(5, 9);
            for (int i = 0; i < n; i++)
            {
                float s = Rand(rng, 0.45f, 0.95f);
                Prim(root.transform, PrimitiveType.Sphere, $"Puff{i}", m,
                    new Vector3(Rand(rng, -1.2f, 1.2f), Rand(rng, 0f, 0.35f), Rand(rng, -0.7f, 0.7f)), new Vector3(s * 1.3f, s * 0.75f, s));
            }
            PropAnimator a = root.AddComponent<PropAnimator>();
            a.Drift = new Vector3(Rand(rng, -0.6f, 0.2f), 0f, 0f);
            a.BobAmplitude = 0.6f;
            a.BobSpeed = 0.3f;
        }

        private static void BuildSpire(GameObject root, PropSpec spec)
        {
            Material m = DomainMaterials.Glow(spec.Tint, spec.Emission, 0.5f);
            Material gold = DomainMaterials.Glow(spec.Emission * 0.5f, spec.Emission, 2f);
            Part(root.transform, "Spire", ProceduralMeshes.Cone(0.32f, 1f, 6), m, Vector3.zero, Vector3.zero, Vector3.one);
            for (int i = 1; i <= 3; i++)
            {
                float y = i * 0.22f;
                float r = 0.32f * (1f - y);
                Part(root.transform, $"Band{i}", ProceduralMeshes.Torus(r + 0.01f, 0.012f, 20, 5), gold, new Vector3(0f, y, 0f), new Vector3(90f, 0f, 0f), Vector3.one);
            }
        }

        private static void BuildMonolith(GameObject root, PropSpec spec)
        {
            Material m = DomainMaterials.Glow(spec.Tint, spec.Emission, 0.3f);
            Material strip = DomainMaterials.Glow(spec.Emission * 0.3f, spec.Emission, 3f);
            Prim(root.transform, PrimitiveType.Cube, "Slab", m, new Vector3(0f, 0.5f, 0f), new Vector3(0.45f, 1f, 0.18f));
            Prim(root.transform, PrimitiveType.Cube, "Strip", strip, new Vector3(0f, 0.5f, 0.1f), new Vector3(0.05f, 0.9f, 0.01f));
            Animate(root, Vector3.zero, 0.2f, 0.3f);
        }

        private static void BuildCrimsonSpire(GameObject root, PropSpec spec, System.Random rng)
        {
            Material m = DomainMaterials.Glow(spec.Tint, spec.Emission, 0.8f);
            Material core = DomainMaterials.Glow(spec.Emission * 0.4f, spec.Emission, 2.2f);
            Part(root.transform, "Spire", ProceduralMeshes.Cone(0.3f, 1f, 6, 0.3f, rng.Next(1, 99)), m, Vector3.zero, new Vector3(Rand(rng, -5f, 5f), 0f, Rand(rng, -5f, 5f)), Vector3.one);
            Part(root.transform, "Shard", ProceduralMeshes.Cone(0.14f, 0.6f, 5, 0.2f, rng.Next(1, 99)), core, new Vector3(0.3f, 0f, 0.1f), new Vector3(0f, 0f, -12f), Vector3.one);
            Part(root.transform, "Shard2", ProceduralMeshes.Cone(0.12f, 0.45f, 5, 0.2f, rng.Next(1, 99)), core, new Vector3(-0.28f, 0f, -0.1f), new Vector3(0f, 0f, 14f), Vector3.one);
        }
    }

    /// <summary>Marks props that already carry real metre dimensions (not scaled by the spec scale).</summary>
    public class TowerMarker : MonoBehaviour { }
}
