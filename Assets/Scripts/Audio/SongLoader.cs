using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Loads a local song chosen by the player and turns it into a <see cref="UserSong"/>.
    /// - WebGL: the browser's file picker; the browser decodes the file (MP3/WAV/OGG/M4A/FLAC…)
    ///   and hands mono PCM to C# (see Assets/Plugins/WebGL/NitroSongPicker.jslib).
    /// - Desktop/Editor: a path from the in-game file browser, decoded with UnityWebRequestMultimedia.
    /// The analysis runs on a worker thread where threads exist.
    /// </summary>
    public class SongLoader : MonoBehaviour
    {
        public enum LoadState { Idle, WaitingForFile, Decoding, Analysing, Done, Failed }

        /// <summary>Browser decode rate: enough fidelity for music, half the memory of 44.1 kHz stereo.</summary>
        public const int WebSampleRate = 32000;

        public static readonly string[] DesktopExtensions = { ".mp3", ".ogg", ".wav", ".aif", ".aiff" };

        private static SongLoader _instance;

        public LoadState State { get; private set; } = LoadState.Idle;
        public string Error { get; private set; }
        public UserSong Result { get; private set; }

        public event Action<SongLoader> Changed;

        public static SongLoader Instance
        {
            get
            {
                if (_instance != null) return _instance;
                GameObject go = new GameObject("NitroSongLoader");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SongLoader>();
                return _instance;
            }
        }

        public static bool UsesBrowserPicker
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public bool Busy => State == LoadState.WaitingForFile || State == LoadState.Decoding || State == LoadState.Analysing;

        public static bool IsSupportedFile(string path)
        {
            string ext = Path.GetExtension(path ?? "").ToLowerInvariant();
            return Array.IndexOf(DesktopExtensions, ext) >= 0;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void SetState(LoadState state, string error = null)
        {
            State = state;
            Error = error;
            Changed?.Invoke(this);
        }

        /// <summary>Resets a finished/failed load so the UI goes back to idle.</summary>
        public void Cancel()
        {
            StopAllCoroutines();
#if UNITY_WEBGL && !UNITY_EDITOR
            NitroSong_Reset();
#endif
            Result = null;
            SetState(LoadState.Idle);
        }

        // ------------------------------------------------------------------ browser

        /// <summary>Opens the browser file picker (WebGL only). Must be called from a click.</summary>
        public void OpenBrowserPicker()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StopAllCoroutines();
            Result = null;
            NitroSong_Open(Mathf.RoundToInt(UserSong.MaxSeconds), WebSampleRate);
            SetState(LoadState.WaitingForFile);
            StartCoroutine(PollBrowser());
#else
            SetState(LoadState.Failed, "El selector del navegador solo existe en la versión web.");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void NitroSong_Open(int maxSeconds, int sampleRate);
        [DllImport("__Internal")] private static extern int NitroSong_State();
        [DllImport("__Internal")] private static extern int NitroSong_SampleCount();
        [DllImport("__Internal")] private static extern int NitroSong_SampleRate();
        [DllImport("__Internal")] private static extern string NitroSong_Name();
        [DllImport("__Internal")] private static extern string NitroSong_Error();
        [DllImport("__Internal")] private static extern void NitroSong_Copy(float[] buffer, int length);
        [DllImport("__Internal")] private static extern void NitroSong_Reset();

        private IEnumerator PollBrowser()
        {
            // JS states: 1 = picking, 2 = decoding, 3 = ready, -1 = error, -2 = cancelled.
            while (true)
            {
                int js = NitroSong_State();
                if (js == 2 && State != LoadState.Decoding) SetState(LoadState.Decoding);
                if (js == 3) break;
                if (js == -2) { NitroSong_Reset(); SetState(LoadState.Idle); yield break; }
                if (js == -1)
                {
                    string error = NitroSong_Error();
                    NitroSong_Reset();
                    SetState(LoadState.Failed, "El navegador no pudo leer el audio: " + error);
                    yield break;
                }
                yield return null;
            }

            int count = NitroSong_SampleCount();
            int rate = NitroSong_SampleRate();
            string name = NitroSong_Name();
            float[] samples = new float[Mathf.Max(0, count)];
            if (count > 0) NitroSong_Copy(samples, count);
            NitroSong_Reset();

            if (count < rate * 15)
            {
                SetState(LoadState.Failed, "La canción es demasiado corta (mínimo 15 segundos).");
                yield break;
            }

            yield return AnalyseRoutine(name, samples, 1, rate);
        }
#endif

        // ------------------------------------------------------------------ desktop

        /// <summary>Loads a local file (desktop/Editor). Supported: MP3, OGG, WAV, AIFF.</summary>
        public void LoadFile(string path)
        {
            StopAllCoroutines();
            Result = null;
            StartCoroutine(LoadFileRoutine(path));
        }

        /// <summary>Coroutine form, used by the gameplay scene when only a path is known.</summary>
        public IEnumerator LoadFileRoutine(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SetState(LoadState.Failed, "No se eligió ningún archivo.");
                yield break;
            }

            bool isUrl = path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("file://");
            if (!isUrl && !File.Exists(path))
            {
                SetState(LoadState.Failed, "No existe el archivo: " + path);
                yield break;
            }

            SetState(LoadState.Decoding);

            string url = isUrl ? path : new Uri(Path.GetFullPath(path)).AbsoluteUri;
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, GuessType(path)))
            {
                if (request.downloadHandler is DownloadHandlerAudioClip handler)
                {
                    handler.streamAudio = false;
                    handler.compressed = false;   // decompress on load so GetData works
                }

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetState(LoadState.Failed, $"No se pudo abrir el audio ({request.error}). Prueba con WAV u OGG.");
                    yield break;
                }

                AudioClip clip = null;
                try { clip = DownloadHandlerAudioClip.GetContent(request); }
                catch (Exception e) { SetState(LoadState.Failed, "Formato no soportado: " + e.Message); yield break; }

                if (clip == null || clip.samples <= 0)
                {
                    SetState(LoadState.Failed, "El archivo no contiene audio legible. Prueba con WAV u OGG.");
                    yield break;
                }

                while (clip.loadState == AudioDataLoadState.Loading) yield return null;

                int channels = Mathf.Max(1, clip.channels);
                int rate = clip.frequency;
                long frames = Math.Min(clip.samples, (long)(UserSong.MaxSeconds * rate));
                float[] pcm = new float[frames * channels];
                bool ok = clip.GetData(pcm, 0);
                Destroy(clip);

                if (!ok)
                {
                    SetState(LoadState.Failed, "No se pudieron leer las muestras del audio.");
                    yield break;
                }

                if (frames < rate * 15)
                {
                    SetState(LoadState.Failed, "La canción es demasiado corta (mínimo 15 segundos).");
                    yield break;
                }

                yield return AnalyseRoutine(Path.GetFileName(path), pcm, channels, rate);
            }
        }

        private static AudioType GuessType(string path)
        {
            switch (Path.GetExtension(path ?? "").ToLowerInvariant())
            {
                case ".mp3": return AudioType.MPEG;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".wav": return AudioType.WAV;
                case ".aif":
                case ".aiff": return AudioType.AIFF;
                default: return AudioType.UNKNOWN;
            }
        }

        // ------------------------------------------------------------------ analysis

        private IEnumerator AnalyseRoutine(string name, float[] pcm, int channels, int rate)
        {
            SetState(LoadState.Analysing);
            yield return null;   // let the UI show "Analizando…" before the heavy work

            UserSong song = null;
            string error = null;

#if UNITY_WEBGL && !UNITY_EDITOR
            try { song = UserSong.FromPcm(name, pcm, channels, rate); }
            catch (Exception e) { error = e.Message; }
#else
            Task<UserSong> task = Task.Run(() => UserSong.FromPcm(name, pcm, channels, rate));
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) error = task.Exception?.GetBaseException().Message;
            else song = task.Result;
#endif

            if (song == null)
            {
                SetState(LoadState.Failed, "Falló el análisis de la canción: " + error);
                yield break;
            }

            Result = song;
            Debug.Log($"[SongLoader] '{song.Name}' — {UserSong.FormatTime(song.Duration)}, {song.Analysis.bpm:F1} BPM, {song.Analysis.beatCount} beats.");
            SetState(LoadState.Done);
        }
    }
}
