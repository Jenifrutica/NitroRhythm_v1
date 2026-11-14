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

        /// <summary>Looks this far ahead of the kart: the kart sits lower in the frame and more track is visible.</summary>
        public float LookAhead = 0f;

        /// <summary>Widens the field of view with speed and turbo (sense of speed).</summary>
        public bool DynamicFov;
        public float BaseFov = 60f;
        public float SpeedFovBoost = 9f;
        public float SpeedForMaxFov = 42f;

        private Rigidbody _targetBody;
        private Camera _camera;

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
                Vector3 ahead = Vector3.ProjectOnPlane(Target.forward, Vector3.up).normalized * LookAhead;
                Vector3 lookPoint = Target.position + ahead + Vector3.up * LookHeightOffset;
                transform.LookAt(lookPoint);
            }

            if (DynamicFov)
            {
                if (_camera == null) _camera = GetComponent<Camera>();
                if (_targetBody == null || _targetBody.transform != Target) _targetBody = Target.GetComponent<Rigidbody>();
                if (_camera != null)
                {
                    float speed = _targetBody != null ? _targetBody.linearVelocity.magnitude : 0f;
                    float boost = 0f;
                    NitroRhythm.Player.PlayerKartController kart = Target.GetComponent<NitroRhythm.Player.PlayerKartController>();
                    if (kart != null && kart.IsBoosting) boost = 5f;
                    float target = BaseFov + SpeedFovBoost * Mathf.Clamp01(speed / SpeedForMaxFov) + boost;
                    _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, target, 1f - Mathf.Exp(-4f * Mathf.Min(Time.unscaledDeltaTime, 0.05f)));
                }
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