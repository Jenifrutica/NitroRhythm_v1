using System;
using System.Collections;
using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Owns the gameplay music source. It obtains an AudioClip (user supplied
    /// song section or the domain's synthesized track), pre-analyses it into an
    /// <see cref="AudioAnalysisResult"/> and exposes live band energies for
    /// reactive visuals and on-beat gameplay events.
    /// </summary>
    public class AudioReactiveMusicController : MonoBehaviour
    {
        public static AudioReactiveMusicController Instance { get; private set; }

        [SerializeField] private float _demoBpm = 128f;
        [SerializeField] private int _demoBars = 16;

        private readonly float[] _spectrum = new float[1024];
        private AudioClip _clip;
        private bool _ownsClip;

        /// <summary>"Song name · 0:31–1:02" while a user song plays; null with the built-in music.</summary>
        public string SectionLabel { get; private set; }

        public AudioSource Source { get; private set; }
        public AudioAnalysisResult Analysis { get; private set; }
        public bool IsReady { get; private set; }

        /// <summary>Live FFT magnitudes (1024 bins, Blackman window) of the playing music.</summary>
        public float[] Spectrum => _spectrum;

        /// <summary>Live normalized low-band (kick/sub-bass) energy 0..1.</summary>
        public float Bass { get; private set; }
        /// <summary>Live normalized high-band (hat/synth) energy 0..1.</summary>
        public float Treble { get; private set; }
        /// <summary>Live overall energy 0..1.</summary>
        public float Energy { get; private set; }

        /// <summary>Raised once the clip has been analysed and playback started.</summary>
        public event Action<AudioAnalysisResult> AnalysisReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            Source = GetComponent<AudioSource>();
            if (Source == null)
            {
                Source = gameObject.AddComponent<AudioSource>();
            }

            Source.loop = true;
            Source.playOnAwake = false;
            Source.spatialBlend = 0f;
            Source.volume = GameSession.Instance != null ? GameSession.Instance.MusicVolume : 0.8f;
        }

        private void Start()
        {
            StartCoroutine(PrepareRoutine());
        }

        private void Update()
        {
            if (Source == null || !Source.isPlaying) return;

            Source.GetSpectrumData(_spectrum, 0, FFTWindow.Blackman);

            int bassBins = 32;
            int trebleStart = 128;
            int trebleBins = 256;

            float bassSum = 0f;
            for (int i = 0; i < bassBins; i++) bassSum += _spectrum[i];
            Bass = Mathf.Clamp01(bassSum / bassBins * 40f);

            float trebleSum = 0f;
            for (int i = trebleStart; i < trebleStart + trebleBins && i < _spectrum.Length; i++) trebleSum += _spectrum[i];
            Treble = Mathf.Clamp01(trebleSum / trebleBins * 120f);

            float total = 0f;
            for (int i = 0; i < _spectrum.Length; i++) total += _spectrum[i];
            Energy = Mathf.Clamp01(total / _spectrum.Length * 55f);
        }

        private IEnumerator PrepareRoutine()
        {
            GameSession session = GameSession.EnsureExists();

            _clip = null;
            _ownsClip = false;

            // Legacy/test entry point: a path or URL set in the session becomes the user song.
            if (!session.HasUserSong && !string.IsNullOrWhiteSpace(session.AudioTrackPath))
            {
                SongLoader loader = SongLoader.Instance;
                yield return loader.LoadFileRoutine(session.AudioTrackPath);
                if (loader.Result != null) session.UserSong = loader.Result;
                else Debug.LogWarning($"[AudioReactive] Could not load '{session.AudioTrackPath}': {loader.Error}");
            }

            NitroRhythm.Data.LevelDefinition[] playable = NitroRhythm.Data.PrototypeData.Instance.PlayableLevels;
            int count = Mathf.Max(1, playable.Length);
            int index = Mathf.Clamp(session.CurrentLevelIndex - 1, 0, count - 1);

            if (session.HasUserSong)
            {
                // The player's song: this domain races its own section of it.
                UserSong song = session.UserSong;
                _clip = song.CreateSectionClip(index, count);
                _ownsClip = true;
                Analysis = song.SectionAnalysis(index, count);
                song.GetSectionTime(index, count, out float start, out float length);
                SectionLabel = $"{song.Name} · {UserSong.FormatTime(start)}–{UserSong.FormatTime(start + length)}";
                Debug.Log($"[AudioReactive] User song '{song.Name}', section {index + 1}/{count} ({SectionLabel}).");
            }
            else
            {
                // Every domain has its own synthesized track (tempo, key, groove, texture).
                string domainId = playable.Length > 0 ? playable[index].id : "default";
                _clip = DomainMusic.Create(domainId);   // cached by DomainMusic: not ours to destroy
                Analysis = AudioAnalysisService.Analyze(_clip);
                SectionLabel = null;
                Debug.Log($"[AudioReactive] Using the synthesized track of domain '{domainId}'.");
            }

            Debug.Log($"[AudioReactive] Analysed '{_clip.name}' — BPM {Analysis.bpm:F1}, beats {Analysis.beatCount}.");

            Source.clip = _clip;
            Source.Play();

            IsReady = true;
            AnalysisReady?.Invoke(Analysis);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_ownsClip && _clip != null) Destroy(_clip);
        }

        public void SetVolume(float volume)
        {
            if (Source != null) Source.volume = Mathf.Clamp01(volume);
        }
    }
}
