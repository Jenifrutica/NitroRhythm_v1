using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Plays 2D sound effects through a small round-robin AudioSource pool and
    /// applies the SFX volume chosen in the options panel (<see cref="GameSession.SfxVolume"/>).
    /// </summary>
    public static class SfxPlayer
    {
        private const int PoolSize = 8;

        private static AudioSource[] _pool;
        private static int _next;

        public static float MasterVolume =>
            GameSession.Instance != null ? GameSession.Instance.SfxVolume : 0.8f;

        public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            EnsurePool();
            AudioSource source = _pool[_next];
            _next = (_next + 1) % PoolSize;

            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume) * MasterVolume;
            source.pitch = pitch;
            source.Play();
        }

        private static void EnsurePool()
        {
            if (_pool != null && _pool[0] != null) return;

            GameObject host = new GameObject("SfxPlayer");
            Object.DontDestroyOnLoad(host);

            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _pool[i] = source;
            }
        }
    }
}
