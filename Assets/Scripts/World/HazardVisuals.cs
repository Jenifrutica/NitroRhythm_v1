using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>
    /// Themed visuals for the track elements (hazards, pads, portal). Every builder returns a root
    /// with the right collider(s) and no scale on the root, so the gameplay components
    /// (spinner, mover, bouncer, speed pad) can drive it directly.
    /// </summary>
    public static class HazardVisuals
    {
        private static GameObject Prim(Transform parent, PrimitiveType type, string name, Material mat, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static GameObject Mesh(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static GameObject SpikeBall(Transform parent, DomainTheme t, Vector3 center, float diameter)
        {
            PropSpec spec = new PropSpec(PropKind.SpikeBall, 0, 0, 0, 1, 1, 0, 0, Color.white, t.hazardColor);
            GameObject ball = PropFactory.Create(spec, t, new System.Random(5));
            ball.transform.SetParent(parent, false);
            ball.transform.localScale = Vector3.one * diameter;
            ball.transform.localPosition = center - Vector3.up * diameter * 0.5f;
            // PropFactory attaches a PropAnimator that moves the ball in world space: not wanted on a hazard.
            PropAnimator a = ball.GetComponent<PropAnimator>();
            if (a != null) { a.BobAmplitude = 0f; a.Drift = Vector3.zero; a.Spin = new Vector3(40f, 90f, 20f); }
            return ball;
        }

        private static GameObject Root(string name, Vector3 position, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.position = position;
            return root;
        }

        /// <summary>Spinning mace bar: glowing bar with a spiked ball at each end.</summary>
        public static GameObject SpinningBar(DomainTheme t, Vector3 position, Transform parent)
        {
            GameObject root = Root("SpinningBar", position, parent);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 0.6f, 4f);

            Material glow = DomainMaterials.Glow(t.hazardColor * 0.4f, t.hazardColor, 2.4f);
            Prim(root.transform, PrimitiveType.Capsule, "Bar", glow, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.28f, 1.7f, 0.28f));
            SpikeBall(root.transform, t, new Vector3(0f, 0f, 1.95f), 1.2f);
            SpikeBall(root.transform, t, new Vector3(0f, 0f, -1.95f), 1.2f);
            return root;
        }

        /// <summary>Sweeping hazard: long glowing bar between two big spiked balls.</summary>
        public static GameObject MovingHazard(DomainTheme t, Vector3 position, float length, Transform parent)
        {
            GameObject root = Root("MovingHazard", position, parent);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 1.0f, length);

            Material glow = DomainMaterials.Glow(t.hazardColor * 0.4f, t.hazardColor, 2.2f);
            Prim(root.transform, PrimitiveType.Capsule, "Bar", glow, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.22f, length * 0.5f, 0.22f));
            SpikeBall(root.transform, t, new Vector3(0f, 0f, length * 0.5f - 0.5f), 1.5f);
            SpikeBall(root.transform, t, new Vector3(0f, 0f, -length * 0.5f + 0.5f), 1.5f);
            return root;
        }

        /// <summary>Static barrier: dark slab with glowing bands and a crown of spikes.</summary>
        public static GameObject StaticBarrier(DomainTheme t, Vector3 position, float height, Transform parent)
        {
            GameObject root = Root("StaticBarrier", position, parent);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(0.8f, height, 2.5f);

            Material body = DomainMaterials.Glow(t.platformColor * 1.2f, t.hazardColor, 0.25f);
            Material band = DomainMaterials.Glow(t.hazardColor * 0.4f, t.hazardColor, 2.6f);
            Prim(root.transform, PrimitiveType.Cube, "Slab", body, Vector3.zero, Vector3.zero, new Vector3(0.8f, height, 2.5f));
            Prim(root.transform, PrimitiveType.Cube, "BandLow", band, new Vector3(0f, -height * 0.25f, 0f), Vector3.zero, new Vector3(0.84f, 0.12f, 2.54f));
            Prim(root.transform, PrimitiveType.Cube, "BandHigh", band, new Vector3(0f, height * 0.2f, 0f), Vector3.zero, new Vector3(0.84f, 0.12f, 2.54f));
            for (int i = -1; i <= 1; i++)
            {
                Mesh(root.transform, $"Spike{i}", ProceduralMeshes.Cone(0.28f, 0.9f, 5), band, new Vector3(0f, height * 0.5f, i * 0.8f), Vector3.zero, Vector3.one);
            }
            return root;
        }

        /// <summary>Bounce pad: glowing disc, ring and an arrow pointing up.</summary>
        public static GameObject JumpPad(DomainTheme t, Vector3 position, Transform parent)
        {
            GameObject root = Root("JumpPad", position, parent);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(2f, 0.3f, 2f);
            box.isTrigger = true;

            Color green = new Color(0.25f, 1f, 0.6f);
            Material glow = DomainMaterials.Glow(green * 0.3f, green, 2.2f);
            Prim(root.transform, PrimitiveType.Cylinder, "Disc", glow, new Vector3(0f, -0.05f, 0f), Vector3.zero, new Vector3(2f, 0.04f, 2f));
            Mesh(root.transform, "Ring", ProceduralMeshes.Torus(0.95f, 0.06f, 28, 6), glow, new Vector3(0f, 0.02f, 0f), new Vector3(90f, 0f, 0f), Vector3.one);
            Mesh(root.transform, "Arrow", ProceduralMeshes.Cone(0.35f, 0.7f, 4), glow, new Vector3(0f, 0.02f, 0f), Vector3.zero, Vector3.one);
            return root;
        }

        /// <summary>Boost pad: flat plate with glowing chevrons pointing along the track (+X).</summary>
        public static GameObject SpeedPad(DomainTheme t, Vector3 position, Transform parent)
        {
            GameObject root = Root("SpeedPad", position, parent);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(2.4f, 0.3f, 2.4f);
            box.isTrigger = true;

            Color cyan = new Color(0.3f, 0.95f, 1f);
            Texture2D chevrons = DomainMaterials.ChevronTexture(cyan);
            Material m = DomainMaterials.Textured(chevrons, Color.white, chevrons, Color.white * 1.8f, 0.6f);
            Prim(root.transform, PrimitiveType.Cube, "Plate", m, new Vector3(0f, -0.04f, 0f), new Vector3(0f, 90f, 0f), new Vector3(2.4f, 0.1f, 2.4f));
            return root;
        }

        /// <summary>Floating prize (the user's PrizeGood model) that gives a turbo when collected.</summary>
        public static GameObject Prize(DomainTheme t, Vector3 position, Transform parent)
        {
            GameObject root = Root("PrizePickup", position, parent);
            SphereCollider sphere = root.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 1.5f;
            root.AddComponent<PrizePickup>();

            PropSpec spec = new PropSpec(PropKind.Drum, 0, 0, 0, 1, 1, 0, 0, Color.white, t.accent);
            GameObject model = null;
            GameObject prefab = Resources.Load<GameObject>("NitroRhythm/Props/PrizeGood");
            if (prefab != null)
            {
                model = NitroRhythm.Core.ModelSanitizer.Strip(Object.Instantiate(prefab, root.transform));
                model.transform.localPosition = Vector3.zero;
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                Material m = DomainMaterials.ModelMaterial("PrizeGood", Color.white, 1.1f);
                foreach (Renderer r in renderers)
                {
                    Material[] shared = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                    for (int i = 0; i < shared.Length; i++) shared[i] = m;
                    r.sharedMaterials = shared;
                    Collider c = r.GetComponent<Collider>();
                    if (c != null) Object.Destroy(c);
                }
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                float k = 1.5f / Mathf.Max(0.0001f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
                model.transform.localScale = Vector3.one * k;
                b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                model.transform.position -= b.center - root.transform.position;
            }
            else
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(fallback.GetComponent<Collider>());
                fallback.transform.SetParent(root.transform, false);
                fallback.transform.localScale = Vector3.one * 1f;
                fallback.GetComponent<Renderer>().sharedMaterial = DomainMaterials.Glow(t.accent, t.accent, 2f);
                model = fallback;
            }

            PropAnimator a = model.AddComponent<PropAnimator>();
            a.Spin = new Vector3(0f, 110f, 0f);

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = t.accent;
            light.intensity = 1.6f;
            light.range = 7f;
            light.shadows = LightShadows.None;
            return root;
        }

        /// <summary>Standing portal at the end of the level: three counter-rotating rings and a glow.</summary>
        public static GameObject Portal(DomainTheme t, Vector3 position, float radius, Transform parent)
        {
            GameObject root = Root("GoalPortal", position, parent);
            Color color = Color.Lerp(t.accent, new Color(0.4f, 1f, 0.6f), 0.5f);
            Material glow = DomainMaterials.Glow(color * 0.4f, color, 2.8f);

            for (int i = 0; i < 3; i++)
            {
                float r = radius * (1f - i * 0.2f);
                GameObject ring = Mesh(root.transform, $"Ring{i}", ProceduralMeshes.Torus(r, 0.12f - i * 0.03f, 40, 8), glow,
                    Vector3.zero, new Vector3(0f, 90f, 0f), Vector3.one);
                PropAnimator a = ring.AddComponent<PropAnimator>();
                a.Spin = new Vector3(0f, 0f, (i % 2 == 0 ? 1f : -1f) * (20f + i * 15f));
                if (i == 0) { a.ScalePulse = 0.05f; a.EmissionPulse = 1.4f; a.GlowRenderers = new[] { ring.GetComponent<Renderer>() }; }
            }

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 3.2f;
            light.range = radius * 3.5f;
            light.shadows = LightShadows.None;
            return root;
        }
    }
}
