using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Factory that creates the core visual entities for NitroRhythm:
    /// Player 1 (light blue / cyan cube), Player 2 (dark blue cube) and the Villain (red cube).
    /// Each entity is a cube primitive scaled like a small kart with a solid material in its color.
    /// </summary>
    public static class VisualEntityFactory
    {
        public const string Player1Name = "Player1";
        public const string Player2Name = "Player2";
        public const string VillainName = "Villain";

        // Player 1: Light Blue / Cyan
        public static readonly Color Player1Color = new Color(0.2f, 0.8f, 1f);

        // Player 2: Dark Blue
        public static readonly Color Player2Color = new Color(0f, 0.15f, 0.7f);

        // Villain: Red
        public static readonly Color VillainColor = Color.red;

        private static readonly Vector3 KartScale = new Vector3(1f, 0.5f, 1.5f);

        public static GameObject CreatePlayer1(Transform parent = null)
        {
            return CreateKartEntity(Player1Name, Player1Color, parent);
        }

        public static GameObject CreatePlayer2(Transform parent = null)
        {
            return CreateKartEntity(Player2Name, Player2Color, parent);
        }

        public static GameObject CreateVillain(Transform parent = null)
        {
            return CreateKartEntity(VillainName, VillainColor, parent);
        }

        /// <summary>
        /// Creates a cube entity with kart-like proportions and the given solid color.
        /// Optionally swaps in a Blender FBX from Resources/NitroRhythm/Models and
        /// recolours it so the URP look matches the character palette.
        /// </summary>
        public static GameObject CreateKartEntity(string entityName, Color color, Transform parent = null, string modelName = null, string textureName = null)
        {
            GameObject entity = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entity.name = entityName;

            if (parent != null)
            {
                entity.transform.SetParent(parent, false);
            }

            entity.transform.localScale = KartScale;

            Renderer renderer = entity.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSolidMaterial(color);
            }

            // Attach the standard modular anchors (RootTransform, CenterOfMass,
            // FiringPoint, WheelMeshes, HoverEffect) so a Blender FBX can replace
            // the primitive without touching gameplay code.
            KartAnchors.Attach(entity, KartScale);

            AttachVisualModel(entity, modelName, textureName, color);

            return entity;
        }

        /// <summary>
        /// Instantiates the optional FBX visual as a non-physical child and hides
        /// the placeholder renderer. Materials are rebuilt as URP Lit using the
        /// character's PBR base-colour texture (Hunyuan output) so imported FBX
        /// materials never render magenta and keep their detail.
        /// </summary>
        private static void AttachVisualModel(GameObject entity, string modelName, string textureName, Color color)
        {
            if (string.IsNullOrEmpty(modelName)) return;

            GameObject model = Resources.Load<GameObject>($"NitroRhythm/Models/{modelName}");
            if (model == null) return;

            GameObject visual = Object.Instantiate(model, entity.transform);
            visual.name = $"{modelName}_Visual";

            // Normalize the (Hunyuan/Blender) model to a consistent world size.
            ModelNormalizer normalizer = visual.AddComponent<ModelNormalizer>();
            normalizer.TargetSize = 4.4f;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Texture2D texture = LoadTexture(textureName);
            Material material = CreateModelMaterial(color, texture);
            foreach (Renderer modelRenderer in visual.GetComponentsInChildren<Renderer>())
            {
                modelRenderer.sharedMaterial = material;
                modelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // Hover glow: a coloured light plus an emissive disc at the anchor,
            // giving each kart a neon underglow that reacts with Bloom.
            KartAnchors anchors = entity.GetComponent<KartAnchors>();
            if (anchors != null && anchors.HoverEffect != null)
            {
                Light hoverLight = anchors.HoverEffect.gameObject.AddComponent<Light>();
                hoverLight.type = LightType.Point;
                hoverLight.color = color;
                hoverLight.range = 5f;
                hoverLight.intensity = 1.1f;

                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "HoverGlow";
                disc.transform.SetParent(anchors.HoverEffect, false);
                disc.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
                Collider discCollider = disc.GetComponent<Collider>();
                if (discCollider != null) Object.Destroy(discCollider);
                Renderer discRenderer = disc.GetComponent<Renderer>();
                if (discRenderer != null) discRenderer.sharedMaterial = CreateEmissiveMaterial(color, 0.9f);
            }

            // Hide the primitive placeholder but keep its collider for physics.
            Renderer placeholder = entity.GetComponent<Renderer>();
            if (placeholder != null) placeholder.enabled = false;
        }

        /// <summary>
        /// Creates a URP material with a distinct emissive accent (platforms,
        /// neon strips) so the track reacts to Bloom post-processing.
        /// </summary>
        public static Material CreateNeonMaterial(Color baseColor, Color emissionColor, float intensity = 1.5f)
        {
            Material material = CreateSolidMaterial(baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor * intensity);
            }
            return material;
        }

        /// <summary>Loads a PBR base-colour texture from Resources by character name.</summary>
        public static Texture2D LoadTexture(string textureName)
        {
            if (string.IsNullOrEmpty(textureName)) return null;
            return Resources.Load<Texture2D>($"NitroRhythm/Textures/{textureName}");
        }

        /// <summary>
        /// Builds a URP Lit material for a model, using the character's texture
        /// when available (preserving the Hunyuan PBR detail) or a flat colour
        /// otherwise, with a subtle self-illumination for the neon look.
        /// </summary>
        public static Material CreateModelMaterial(Color color, Texture2D texture)
        {
            Material material = CreateSolidMaterial(texture != null ? Color.white : color);

            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.55f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.35f);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.18f);
            }

            return material;
        }

        /// <summary>
        /// Creates a solid opaque material. Uses the URP Lit shader when available
        /// and falls back to the legacy Standard/Diffuse shaders otherwise.
        /// </summary>
        public static Material CreateSolidMaterial(Color color)
        {
            Shader shader = ResolveLitShader();

            Material material = new Material(shader);
            material.color = color;

            return material;
        }

        private static Shader _litShader;

        /// <summary>
        /// Resolves the URP Lit shader. Prefers the base material shipped in
        /// Resources (so the shader is included in builds); falls back to
        /// Shader.Find in the editor.
        /// </summary>
        private static Shader ResolveLitShader()
        {
            if (_litShader != null) return _litShader;

            Material baseMaterial = Resources.Load<Material>("NitroRhythm/Materials/BaseLit");
            if (baseMaterial != null && baseMaterial.shader != null)
            {
                _litShader = baseMaterial.shader;
                return _litShader;
            }

            _litShader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
            return _litShader;
        }

        /// <summary>
        /// Creates a solid material with an emissive glow used for checkpoint markers
        /// (best-effort: enables the _EMISSION keyword and sets an intensified
        /// emission color when the shader supports it).
        /// </summary>
        public static Material CreateEmissiveMaterial(Color color, float intensity = 1.5f)
        {
            Material material = CreateSolidMaterial(color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }
            return material;
        }
    }
}
