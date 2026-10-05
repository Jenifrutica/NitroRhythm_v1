using System;
using NitroRhythm.Core;
using NitroRhythm.Data;
using UnityEngine;

namespace NitroRhythm.UI
{
    /// <summary>
    /// A 3D showcase of one character: a real kart + pilot model spinning on a
    /// neon pedestal with rim lighting. Selecting it scales it up, brightens the
    /// ring and speeds up the spin, so the character-select screen reads as a
    /// living 3D garage instead of flat cards.
    /// </summary>
    public class CharacterDisplay : MonoBehaviour
    {
        public CharacterDefinition Definition { get; private set; }
        public event Action<CharacterDisplay> Clicked;

        public bool IsSelected { get; private set; }

        private Transform _pivot;
        private Light _rim;
        private Renderer _ring;
        private Color _accent;
        // Showcase: kart and pilot face the camera, with a gentle sway.
        private const float BaseYaw = 0f;
        private float _swayAmplitude = 10f;
        private float _swaySpeed = 0.7f;
        private float _clock;
        private float _phase;
        private float _scaleTarget = 1f;

        /// <summary>Size factor set by the screen layout so the kart matches its card at any window shape.</summary>
        public float LayoutScale = 1f;

        public void Build(CharacterDefinition def, Vector3 groundPosition, bool pedestal = true, bool clickable = true)
        {
            Definition = def;
            _phase = 0f;   // every character starts facing the camera
            _accent = def.Accent;
            transform.position = groundPosition;

            if (pedestal)
            {
                GameObject baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                baseMesh.name = "Pedestal";
                baseMesh.transform.SetParent(transform, false);
                baseMesh.transform.localScale = new Vector3(2.4f, 0.16f, 2.4f);
                baseMesh.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                Paint(baseMesh, VisualEntityFactory.CreateSolidMaterial(new Color(0.05f, 0.05f, 0.08f)));

                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "NeonRing";
                ring.transform.SetParent(transform, false);
                ring.transform.localScale = new Vector3(2.6f, 0.035f, 2.6f);
                ring.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                _ring = ring.GetComponent<Renderer>();
                _ring.sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(_accent, 2.4f);
                Destroy(ring.GetComponent<Collider>());
            }

            GameObject pivotObject = new GameObject("SpinPivot");
            pivotObject.transform.SetParent(transform, false);
            pivotObject.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            _pivot = pivotObject.transform;
            pivotObject.AddComponent<SlowBob>();

            SpawnModel(def, def.kartModel, _pivot, Vector3.zero, Quaternion.Euler(0f, 180f, 0f), 3.8f, true);
            // Pilot FBX faces +Z (like the kart nose); the kart is turned 180 deg, so the pilot must be too.
            SpawnModel(def, def.pilotModel, _pivot, def.pilotOffset, Quaternion.Euler(0f, 180f, 0f), 1.9f * Mathf.Max(0.2f, def.pilotScale), false);

            _rim = StageFactory.AddRimLight(transform, new Vector3(0f, 3.2f, -0.4f), _accent, 2.4f, 9f);
            StageFactory.AddRimLight(transform, new Vector3(-1.6f, 1.2f, 1.6f), Color.white, 0.5f, 6f);

            if (clickable)
            {
                GameObject hit = new GameObject("ClickArea");
                hit.transform.SetParent(transform, false);
                hit.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                BoxCollider box = hit.AddComponent<BoxCollider>();
                box.size = new Vector3(3.2f, 2.6f, 3.2f);
                ClickProxy proxy = hit.AddComponent<ClickProxy>();
                proxy.Owner = this;
            }
        }

        private void SpawnModel(CharacterDefinition def, string modelName, Transform parent, Vector3 localPosition, Quaternion localRotation, float targetSize, bool useTexture)
        {
            if (string.IsNullOrEmpty(modelName)) return;

            GameObject model = Resources.Load<GameObject>($"NitroRhythm/Models/{modelName}");
            if (model == null) return;

            GameObject instance = ModelSanitizer.Strip(Instantiate(model, parent));
            instance.name = modelName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;

            ModelNormalizer normalizer = instance.AddComponent<ModelNormalizer>();
            normalizer.TargetSize = targetSize;
            normalizer.InheritParentScale = true;   // kart and pilot scale together with the showcase

            Texture2D texture = useTexture ? VisualEntityFactory.LoadTexture(def.texture) : null;
            Material material = VisualEntityFactory.CreateModelMaterial(def.Color, texture);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        private static void Paint(GameObject go, Material material)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            _scaleTarget = selected ? 1.0f : 0.88f;
            _swayAmplitude = selected ? 16f : 10f;
            _swaySpeed = selected ? 1.1f : 0.7f;

            if (_ring != null)
            {
                _ring.sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(_accent, selected ? 4.5f : 1.6f);
            }
            if (_rim != null)
            {
                _rim.intensity = selected ? 4.5f : 1.8f;
            }
        }

        private void Update()
        {
            if (_pivot != null)
            {
                _clock += Time.unscaledDeltaTime;
                float yaw = BaseYaw + (Definition != null ? Definition.modelYawOffset : 0f) + Mathf.Sin(_clock * _swaySpeed + _phase) * _swayAmplitude;
                _pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }

            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (_scaleTarget * LayoutScale), 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }

        internal void RaiseClicked() => Clicked?.Invoke(this);

        /// <summary>Forwards mouse clicks from the collider child to the display.</summary>
        public class ClickProxy : MonoBehaviour
        {
            public CharacterDisplay Owner;

            private void OnMouseDown() => Owner?.RaiseClicked();
        }
    }

    /// <summary>Gentle vertical bob for showcase models.</summary>
    public class SlowBob : MonoBehaviour
    {
        private Vector3 _origin;
        private float _phase;

        private void Start()
        {
            _origin = transform.localPosition;
            _phase = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            transform.localPosition = _origin + Vector3.up * (Mathf.Sin(Time.unscaledTime * 1.6f + _phase) * 0.04f);
        }
    }
}
