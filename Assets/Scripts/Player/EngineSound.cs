using NitroRhythm.Audio;
using UnityEngine;

namespace NitroRhythm.Player
{
    /// <summary>Looping engine hum whose pitch and volume follow the kart's speed.</summary>
    [RequireComponent(typeof(PlayerKartController))]
    public class EngineSound : MonoBehaviour
    {
        [SerializeField] private float _maxVolume = 0.16f;
        [SerializeField] private float _maxSpeed = 44f;

        private AudioSource _source;
        private PlayerKartController _kart;

        private void Awake()
        {
            _kart = GetComponent<PlayerKartController>();
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = SfxLibrary.EngineLoop;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
        }

        private void OnEnable()
        {
            if (_source != null && !_source.isPlaying) _source.Play();
        }

        private void Update()
        {
            float t = Mathf.Clamp01(Mathf.Abs(_kart.CurrentSpeed) / _maxSpeed);
            float boost = _kart.IsBoosting ? 0.25f : 0f;
            _source.pitch = Mathf.Lerp(0.7f, 1.9f, t) + boost;
            _source.volume = Mathf.Lerp(0.25f, 1f, t) * _maxVolume * SfxPlayer.MasterVolume;
        }
    }
}
