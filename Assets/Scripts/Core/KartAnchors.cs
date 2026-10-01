using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Standard replacement anchors for a kart prefab. The factory/controller
    /// never depend on a fixed geometry: swapping in a Blender FBX only requires
    /// mapping these transforms on the <c>RootTransform</c>.
    /// </summary>
    public class KartAnchors : MonoBehaviour
    {
        public Transform RootTransform;
        public Transform CenterOfMass;
        public Transform FiringPoint;
        public Transform[] WheelMeshes = new Transform[0];
        public Transform HoverEffect;

        /// <summary>Creates the anchor hierarchy under a kart root.</summary>
        public static KartAnchors Attach(GameObject kart, Vector3 scale)
        {
            KartAnchors anchors = kart.GetComponent<KartAnchors>();
            if (anchors == null)
            {
                anchors = kart.AddComponent<KartAnchors>();
            }

            anchors.RootTransform = kart.transform;

            anchors.CenterOfMass = EnsureChild(kart.transform, "CenterOfMass", new Vector3(0f, -0.35f, 0f));
            anchors.FiringPoint = EnsureChild(kart.transform, "FiringPoint", new Vector3(0f, 0.4f, 0.9f * scale.z));
            anchors.HoverEffect = EnsureChild(kart.transform, "HoverEffect", new Vector3(0f, -0.5f, 0f));

            anchors.WheelMeshes = new[]
            {
                EnsureChild(kart.transform, "Wheel_FL", new Vector3(-0.5f * scale.x, -0.4f, 0.5f * scale.z)),
                EnsureChild(kart.transform, "Wheel_FR", new Vector3(0.5f * scale.x, -0.4f, 0.5f * scale.z)),
                EnsureChild(kart.transform, "Wheel_RL", new Vector3(-0.5f * scale.x, -0.4f, -0.5f * scale.z)),
                EnsureChild(kart.transform, "Wheel_RR", new Vector3(0.5f * scale.x, -0.4f, -0.5f * scale.z)),
            };

            return anchors;
        }

        private static Transform EnsureChild(Transform parent, string name, Vector3 localPosition)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                existing.localPosition = localPosition;
                return existing;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child.transform;
        }
    }
}
