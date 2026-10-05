using NitroRhythm.Audio;
using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>Gentle life for scenery: spin, bob, drift and a bass-reactive pulse (scale + emission).</summary>
    public class PropAnimator : MonoBehaviour
    {
        public Vector3 Spin;                  // degrees per second (local)
        public float BobAmplitude;
        public float BobSpeed = 0.7f;
        public Vector3 Drift;                 // units per second (world)
        public float ScalePulse;              // extra scale at full bass (0.1 = +10 %)
        public float EmissionPulse;           // extra emission multiplier at full bass
        public Renderer[] GlowRenderers = new Renderer[0];

        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private float _phase;
        private Color[] _baseEmission;
        private Material[] _materials;

        private void Start()
        {
            _basePosition = transform.position;
            _baseScale = transform.localScale;
            _phase = Random.value * 6.28f;

            if (EmissionPulse > 0f && GlowRenderers != null)
            {
                _materials = new Material[GlowRenderers.Length];
                _baseEmission = new Color[GlowRenderers.Length];
                for (int i = 0; i < GlowRenderers.Length; i++)
                {
                    if (GlowRenderers[i] == null) continue;
                    _materials[i] = GlowRenderers[i].material;
                    _baseEmission[i] = _materials[i].HasProperty("_EmissionColor") ? _materials[i].GetColor("_EmissionColor") : Color.black;
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (Spin != Vector3.zero) transform.Rotate(Spin * dt, Space.Self);

            // Only own the world position when bobbing/drifting; otherwise a parent (hazard root) can move us.
            if (BobAmplitude > 0f || Drift != Vector3.zero)
            {
                if (Drift != Vector3.zero) _basePosition += Drift * dt;
                Vector3 position = _basePosition;
                if (BobAmplitude > 0f) position.y += Mathf.Sin(Time.time * BobSpeed + _phase) * BobAmplitude;
                transform.position = position;
            }

            float bass = AudioReactiveMusicController.Instance != null ? AudioReactiveMusicController.Instance.Bass : 0f;
            if (ScalePulse > 0f) transform.localScale = _baseScale * (1f + bass * ScalePulse);

            if (_materials != null)
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    if (_materials[i] == null) continue;
                    _materials[i].SetColor("_EmissionColor", _baseEmission[i] * (1f + bass * EmissionPulse));
                }
            }
        }
    }
}
