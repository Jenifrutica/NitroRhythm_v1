using System;
using System.Collections;
using NitroRhythm.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Owns the gameplay music source. It obtains an AudioClip (user supplied
    /// file/URL or the built-in generated loop), pre-analyses it into an
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

        public AudioSource Source { get; private set; }
        public AudioAnalysisResult Analysis { get; private set; }
        public bool IsReady { get; private set; }

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

            if (!string.IsNullOrWhiteSpace(session.AudioTrackPath))
            {
                yield return LoadExternalClip(session.AudioTrackPath);
            }

            if (_clip == null)
            {
                // Every domain has its own synthesized track (tempo, key, groove, texture).
                NitroRhythm.Data.LevelDefinition[] playable = NitroRhythm.Data.PrototypeData.Instance.PlayableLevels;
                int index = Mathf.Clamp(session.CurrentLevelIndex - 1, 0, Mathf.Max(0, playable.Length - 1));
                string domainId = playable.Length > 0 ? playable[index].id : "default";
                _clip = DomainMusic.Create(domainId);
                Debug.Log($"[AudioReactive] Using the synthesized track of domain '{domainId}'.");
            }

            // Pre-analysis (fast, offline) for deterministic track construction.
            Analysis = AudioAnalysisService.Analyze(_clip);
            Debug.Log($"[AudioReactive] Analysed '{_clip.name}' — BPM {Analysis.bpm:F1}, beats {Analysis.beatCount}.");

            Source.clip = _clip;
            Source.Play();

            IsReady = true;
            AnalysisReady?.Invoke(Analysis);
        }

        private IEnumerator LoadExternalClip(string path)
        {
            string url = path;
            if (!path.StartsWith("http://") && !path.StartsWith("https://") && !path.StartsWith("file://"))
            {
                url = "file://" + path;
            }

            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                _clip = DownloadHandlerAudioClip.GetContent(request);
            }
            else
            {
                Debug.LogWarning($"[AudioReactive] Could not load '{path}' ({request.error}). Falling back to demo loop.");
            }
        }

        public void SetVolume(float volume)
        {
            if (Source != null) Source.volume = Mathf.Clamp01(volume);
        }
    }
}
