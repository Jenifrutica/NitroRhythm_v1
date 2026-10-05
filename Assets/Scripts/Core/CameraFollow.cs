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
        private Vector3 _shakeOffset;
        private float _shakeTime;
        private float _shakeDuration;
        private float _shakeMagnitude;

        /// <summary>Short positional shake used for impact feedback.</summary>
        public void Shake(float magnitude, float duration)
        {
            _shakeMagnitude = Mathf.Max(_shakeMagnitude * Mathf.Clamp01(_shakeTime / Mathf.Max(0.01f, _shakeDuration)), magnitude);
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTime = duration;
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            // Remove last frame's shake so it never feeds back into the smoothing.
            transform.position -= _shakeOffset;

            Vector3 desiredPosition = Target.position + Target.rotation * Offset;
            float smoothTime = 1f / Mathf.Max(0.01f, Smoothing);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothTime, Mathf.Infinity, Mathf.Min(Time.unscaledDeltaTime, 0.05f));

            if (LookAtTarget)
            {
                Vector3 lookPoint = Target.position + Vector3.up * LookHeightOffset;
                transform.LookAt(lookPoint);
            }

            _shakeOffset = Vector3.zero;
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.unscaledDeltaTime;
                float falloff = Mathf.Clamp01(_shakeTime / _shakeDuration);
                _shakeOffset = Random.insideUnitSphere * (_shakeMagnitude * falloff);
                transform.position += _shakeOffset;
            }
        }

        public void SetTarget(Transform target)
        {
            Target = target;
        }
    }
}