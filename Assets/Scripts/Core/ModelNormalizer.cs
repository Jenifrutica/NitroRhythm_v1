using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Normalizes a downloaded/generated model to a target world size and keeps
    /// it constant regardless of the parent kart's scale. Makes the pipeline
    /// robust to models of any source scale (Hunyuan, Blender, asset packs).
    /// </summary>
    public class ModelNormalizer : MonoBehaviour
    {
        /// <summary>Largest dimension the model should occupy in world units.</summary>
        public float TargetSize = 4f;

        /// <summary>Extra uniform multiplier applied after normalization.</summary>
        public float Multiplier = 1f;

        /// <summary>
        /// When enabled, <see cref="WorldOffset"/> is the model's offset in WORLD units
        /// relative to the parent's origin (the parent kart is scaled non-uniformly, so a
        /// plain localPosition would be stretched).
        /// </summary>
        public bool UseWorldOffset;

        /// <summary>
        /// When true the model simply follows its parent's scale (used by the character showcase, whose parent is
        /// scaled to fit the screen layout). Default false: the model keeps a constant world size.
        /// </summary>
        public bool InheritParentScale;
        public Vector3 WorldOffset;

        private Vector3 _factor = Vector3.one;

        private void Start()
        {
            ComputeFactor();
        }

        private void ComputeFactor()
        {
            Bounds bounds = ComputeLocalBounds();
            float max = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (max <= 0.0001f) max = 1f;
            _factor = Vector3.one * (TargetSize / max) * Mathf.Max(0.0001f, Multiplier);
        }

        private Bounds ComputeLocalBounds()
        {
            bool initialized = false;
            Bounds result = new Bounds();
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

            // Works for both static meshes and skinned meshes (renderer.bounds
            // already accounts for skinning/bind pose).
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                Bounds worldBounds = renderer.bounds;
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = center + new Vector3(
                        (i & 1) == 0 ? -extents.x : extents.x,
                        (i & 2) == 0 ? -extents.y : extents.y,
                        (i & 4) == 0 ? -extents.z : extents.z);
                    Vector3 point = worldToLocal.MultiplyPoint3x4(corner);

                    if (!initialized)
                    {
                        result = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        result.Encapsulate(point);
                    }
                }
            }

            return result;
        }

        private void LateUpdate()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            if (InheritParentScale)
            {
                transform.localScale = _factor;
                return;
            }

            Vector3 scale = parent.lossyScale;
            transform.localScale = new Vector3(
                _factor.x / Mathf.Max(0.0001f, scale.x),
                _factor.y / Mathf.Max(0.0001f, scale.y),
                _factor.z / Mathf.Max(0.0001f, scale.z));

            if (UseWorldOffset)
            {
                transform.localPosition = new Vector3(
                    WorldOffset.x / Mathf.Max(0.0001f, scale.x),
                    WorldOffset.y / Mathf.Max(0.0001f, scale.y),
                    WorldOffset.z / Mathf.Max(0.0001f, scale.z));
            }
        }
    }
}
