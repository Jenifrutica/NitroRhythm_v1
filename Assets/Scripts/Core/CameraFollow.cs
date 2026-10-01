using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Smooth third-person follow camera. The offset is applied in the target's local
    /// space (behind-above the kart) and the camera smoothly looks at the target.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 3.5f, -6f);
        public float Smoothing = 6f;
        public bool LookAtTarget = true;
        public float LookHeightOffset = 1f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 desiredPosition = Target.position + Target.rotation * Offset;
            float smoothTime = 1f / Mathf.Max(0.01f, Smoothing);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothTime);

            if (LookAtTarget)
            {
                Vector3 lookPoint = Target.position + Vector3.up * LookHeightOffset;
                transform.LookAt(lookPoint);
            }
        }

        public void SetTarget(Transform target)
        {
            Target = target;
        }
    }
}