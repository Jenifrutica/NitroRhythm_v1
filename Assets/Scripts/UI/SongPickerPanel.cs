using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NitroRhythm.UI
{
    /// <summary>
    /// "TU CANCIÓN" overlay: the player picks a local song and the whole campaign is built from it
    /// (each domain races its own section). On the web build the browser's file picker is used; on
    /// desktop/Editor an in-game file browser lists folders and audio files (MP3/OGG/WAV/AIFF).
    /// </summary>
    public class SongPickerPanel : MonoBehaviour
    {
        private const string LastFolderKey = "NitroRhythm.SongFolder";
        private const int MaxEntries = 400;

        public static bool IsOpen { get; private set; }

        /// <summary>Raised when the chosen song changes (set or cleared).</summary>
        public event Action SongChanged;

        private GameObject _root;
        private CanvasGroup _group;
        private Coroutine _fade;

        private TextMeshProUGUI _songTitle;
        private TextMeshProUGUI _songDetails;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _pathLabel;
        private GameObject _browser;
        private RectTransform _listContent;
        private ScrollRect _scroll;
        private Button _pickButton;
        private Button _clearButton;

        private string _folder;
        private SongLoader _loader;

        public static SongPickerPanel Create(Transform canvas)
        {
            GameObject go = new GameObject("SongPickerPanel", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            SongPickerPanel panel = go.AddComponent<SongPickerPanel>();
            panel.Build(go.transform);
            return panel;
        }

        private void Build(Transform parent)
        {
            Image dim = UIFactory.CreatePanel(parent, "SongDim", new Color(0.01f, 0.01f, 0.04f, 0.86f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root = dim.gameObject;
            _group = _root.AddComponent<CanvasGroup>();

            bool web = SongLoader.UsesBrowserPicker;
            Vector2 cardSize = web ? new Vector2(1120f, 640f) : new Vector2(1180f, 900f);
            Image card = UIFactory.CreateCard(dim.transform, "SongCard", new Color(0.04f, 0.05f, 0.12f, 0.97f), UIFactory.NeonPink,
                cardSize, Vector2.zero);
            Transform c = card.transform;
            float top = cardSize.y * 0.5f;

            TextMeshProUGUI icon = UIFactory.CreateIcon(c, UIIcons.Library, 50f, UIFactory.NeonPink);
            icon.rectTransform.anchoredPosition = new Vector2(-200f, top - 62f);
            TextMeshProUGUI title = UIFactory.CreateTitle(c, "Title", "TU CANCIÓN", 48f, Color.white, UIFactory.NeonPink);
            title.rectTransform.anchoredPosition = new Vector2(40f, top - 62f);
            title.rectTransform.sizeDelta = new Vector2(600f, 64f);

            TextMeshProUGUI intro = UIFactory.CreateText(c, "Intro",
                "La canción se reparte en los 7 dominios: cada pantalla se arma con su tramo. Graves → huecos y saltos · agudos → obstáculos · BPM → ritmo de la pista y ataques del villano.",
                26f, UIFactory.TextSoft, TextAlignmentOptions.Center);
            intro.enableWordWrapping = true;
            intro.rectTransform.anchoredPosition = new Vector2(0f, top - 140f);
            intro.rectTransform.sizeDelta = new Vector2(cardSize.x - 120f, 70f);

            // ---- current song
            Image songBox = UIFactory.CreatePanel(c, "SongBox", new Color(0.12f, 0.03f, 0.1f, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            songBox.sprite = UIFactory.RoundedSprite;
            songBox.type = Image.Type.Sliced;
            Place(songBox.rectTransform, new Vector2(0f, top - 240f), new Vector2(cardSize.x - 120f, 104f));
            TextMeshProUGUI note = UIFactory.CreateIcon(songBox.transform, UIIcons.Music, 46f, UIFactory.NeonPink);
            note.rectTransform.anchorMin = note.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            note.rectTransform.anchoredPosition = new Vector2(52f, 0f);
            _songTitle = UIFactory.CreateLabel(songBox.transform, "SongTitle", "", 34f, Color.white, TextAlignmentOptions.Left);
            Stretch(_songTitle.rectTransform, new Vector2(100f, 46f), new Vector2(-24f, -8f));
            _songTitle.enableWordWrapping = false;
            _songTitle.overflowMode = TextOverflowModes.Ellipsis;
            _songDetails = UIFactory.CreateText(songBox.transform, "SongDetails", "", 24f, UIFactory.TextSoft, TextAlignmentOptions.Left);
            Stretch(_songDetails.rectTransform, new Vector2(100f, 8f), new Vector2(-24f, -58f));
            _songDetails.enableWordWrapping = false;
            _songDetails.overflowMode = TextOverflowModes.Ellipsis;

            // ---- actions
            float actionsY = top - 350f;
            _pickButton = UIFactory.CreateButton(c, web ? "ELEGIR ARCHIVO" : "BUSCAR CANCIÓN", new Vector2(-250f, actionsY), new Vector2(440f, 70f),
                UIFactory.NeonPink, OnPickClicked, UIIcons.Upload);
            _clearButton = UIFactory.CreateButton(c, "MÚSICA DEL JUEGO", new Vector2(250f, actionsY), new Vector2(440f, 70f),
                UIFactory.NeonCyan, ClearSong, UIIcons.Replay);

            _status = UIFactory.CreateText(c, "Status", "", 26f, UIFactory.NeonGold, TextAlignmentOptions.Center);
            _status.enableWordWrapping = true;
            Place(_status.rectTransform, new Vector2(0f, actionsY - 72f), new Vector2(cardSize.x - 120f, 60f));

            if (!web) BuildBrowser(c, actionsY - 120f, cardSize);
            else
            {
                TextMeshProUGUI formats = UIFactory.CreateText(c, "Formats",
                    $"Formatos: MP3, WAV, OGG, M4A, FLAC (según el navegador) · máximo {Mathf.RoundToInt(UserSong.MaxSeconds / 60f)} minutos · se reproduce en mono.",
                    22f, new Color(0.65f, 0.7f, 0.85f), TextAlignmentOptions.Center);
                formats.enableWordWrapping = true;
                Place(formats.rectTransform, new Vector2(0f, -top + 150f), new Vector2(cardSize.x - 140f, 60f));
            }

            UIFactory.CreateButton(c, "LISTO", new Vector2(0f, -top + 60f), new Vector2(360f, 72f), UIFactory.NeonGreen, Hide, UIIcons.Check);

            _loader = SongLoader.Instance;
            _loader.Changed += OnLoaderChanged;

            RefreshSong();
            _root.SetActive(false);
            _group.alpha = 0f;
        }

        private void BuildBrowser(Transform card, float topY, Vector2 cardSize)
        {
            _browser = new GameObject("Browser", typeof(RectTransform));
            _browser.transform.SetParent(card, false);
            RectTransform browserRect = (RectTransform)_browser.transform;
            float width = cardSize.x - 120f;
            float height = topY - (-cardSize.y * 0.5f + 120f);
            Place(browserRect, new Vector2(0f, topY - height * 0.5f), new Vector2(width, height));

            // Quick locations + "up" + current path.
            float rowY = height * 0.5f - 30f;
            UIFactory.CreateButton(_browser.transform, "SUBIR", new Vector2(-width * 0.5f + 95f, rowY), new Vector2(170f, 52f), UIFactory.NeonGold, GoUp, UIIcons.ArrowUp);
            UIFactory.CreateButton(_browser.transform, "MÚSICA", new Vector2(-width * 0.5f + 280f, rowY), new Vector2(180f, 52f), UIFactory.NeonGold,
                () => OpenFolder(MusicFolder()), UIIcons.Music);
            UIFactory.CreateButton(_browser.transform, "DESCARGAS", new Vector2(-width * 0.5f + 485f, rowY), new Vector2(210f, 52f), UIFactory.NeonGold,
                () => OpenFolder(DownloadsFolder()), UIIcons.Folder);
            UIFactory.CreateButton(_browser.transform, "INICIO", new Vector2(-width * 0.5f + 685f, rowY), new Vector2(170f, 52f), UIFactory.NeonGold,
                () => OpenFolder(HomeFolder()), UIIcons.Home);

            _pathLabel = UIFactory.CreateText(_browser.transform, "Path", "", 22f, UIFactory.TextSoft, TextAlignmentOptions.Left);
            _pathLabel.enableWordWrapping = false;
            _pathLabel.overflowMode = TextOverflowModes.Ellipsis;
            Place(_pathLabel.rectTransform, new Vector2(0f, rowY - 50f), new Vector2(width, 32f));

            // Scrollable list.
            float listTop = rowY - 74f;
            float listHeight = listTop + height * 0.5f;
            GameObject scrollGo = new GameObject("List", typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(_browser.transform, false);
            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.sprite = UIFactory.RoundedSprite;
            scrollBg.type = Image.Type.Sliced;
            scrollBg.color = new Color(0.02f, 0.03f, 0.08f, 0.95f);
            RectTransform scrollRect = (RectTransform)scrollGo.transform;
            Place(scrollRect, new Vector2(0f, listTop - listHeight * 0.5f), new Vector2(width, listHeight));

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollGo.transform, false);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            RectTransform viewRect = (RectTransform)viewport.transform;
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(10f, 10f);
            viewRect.offsetMax = new Vector2(-10f, -10f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            _listContent = (RectTransform)content.transform;
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.offsetMin = _listContent.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = scrollGo.GetComponent<ScrollRect>();
            _scroll.viewport = viewRect;
            _scroll.content = _listContent;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 45f;

            _browser.SetActive(false);
        }

        // ------------------------------------------------------------------ show / hide

        public void Show()
        {
            IsOpen = true;
            _root.SetActive(true);
            RefreshSong();
            StartFade(1f);
        }

        public void Hide()
        {
            IsOpen = false;
            if (_loader != null && _loader.State == SongLoader.LoadState.WaitingForFile) _loader.Cancel();
            if (!_root.activeSelf) return;
            StartFade(0f);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Hide();

            if (_loader != null && _loader.Busy)
            {
                int dots = 1 + (int)(Time.unscaledTime * 3f) % 3;
                _status.text = StatusText(_loader.State) + new string('.', dots);
            }
        }

        private void OnDestroy()
        {
            IsOpen = false;
            if (_loader != null) _loader.Changed -= OnLoaderChanged;
        }

        // ------------------------------------------------------------------ actions

        private void OnPickClicked()
        {
            if (_loader.Busy && _loader.State != SongLoader.LoadState.WaitingForFile) return;

            if (SongLoader.UsesBrowserPicker)
            {
                _loader.OpenBrowserPicker();
                return;
            }

            bool show = !_browser.activeSelf;
            _browser.SetActive(show);
            if (show && string.IsNullOrEmpty(_folder)) OpenFolder(InitialFolder());
        }

        private void ClearSong()
        {
            if (_loader.Busy) _loader.Cancel();
            GameSession session = GameSession.EnsureExists();
            session.UserSong = null;
            session.AudioTrackPath = "";
            _status.text = "Se usará la música propia de cada dominio.";
            RefreshSong();
            SongChanged?.Invoke();
        }

        private void OnLoaderChanged(SongLoader loader)
        {
            switch (loader.State)
            {
                case SongLoader.LoadState.Done:
                    GameSession.EnsureExists().UserSong = loader.Result;
                    _status.text = "¡Lista! La carrera se armará con esta canción.";
                    RefreshSong();
                    SongChanged?.Invoke();
                    break;
                case SongLoader.LoadState.Failed:
                    _status.text = loader.Error;
                    break;
                case SongLoader.LoadState.Idle:
                    _status.text = "";
                    break;
                default:
                    _status.text = StatusText(loader.State);
                    break;
            }

            bool busy = loader.Busy && loader.State != SongLoader.LoadState.WaitingForFile;
            if (_pickButton != null) _pickButton.interactable = !busy;
            if (_clearButton != null) _clearButton.interactable = !busy;
        }

        private static string StatusText(SongLoader.LoadState state)
        {
            switch (state)
            {
                case SongLoader.LoadState.WaitingForFile: return "Elige un archivo de audio en la ventana del navegador";
                case SongLoader.LoadState.Decoding: return "Leyendo el audio";
                case SongLoader.LoadState.Analysing: return "Analizando graves, agudos y BPM";
                default: return "";
            }
        }

        private void RefreshSong()
        {
            GameSession session = GameSession.EnsureExists();
            if (session.HasUserSong)
            {
                UserSong song = session.UserSong;
                int domains = Mathf.Max(1, NitroRhythm.Data.PrototypeData.Instance.PlayableCount);
                song.GetSectionTime(0, domains, out _, out float section);
                _songTitle.text = song.Name;
                _songDetails.text = $"{UserSong.FormatTime(song.Duration)} · {Mathf.RoundToInt(song.Analysis.bpm)} BPM · {domains} tramos de ~{UserSong.FormatTime(section)}";
            }
            else
            {
                _songTitle.text = "Música del juego";
                _songDetails.text = "Cada dominio tiene su propia pista sintetizada.";
            }
        }

        // ------------------------------------------------------------------ file browser (desktop)

        private void OpenFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) folder = HomeFolder();
            _folder = folder;
            try { PlayerPrefs.SetString(LastFolderKey, folder); } catch (Exception) { }
            _pathLabel.text = folder;

            for (int i = _listContent.childCount - 1; i >= 0; i--) Destroy(_listContent.GetChild(i).gameObject);

            List<string> dirs = new List<string>();
            List<string> files = new List<string>();
            try
            {
                foreach (string d in Directory.GetDirectories(folder))
                {
                    if (!Path.GetFileName(d).StartsWith(".")) dirs.Add(d);
                }
                foreach (string f in Directory.GetFiles(folder))
                {
                    if (SongLoader.IsSupportedFile(f)) files.Add(f);
                }
            }
            catch (Exception e)
            {
                _status.text = "No se puede abrir la carpeta: " + e.Message;
            }

            dirs.Sort(StringComparer.CurrentCultureIgnoreCase);
            files.Sort(StringComparer.CurrentCultureIgnoreCase);

            int shown = 0;
            foreach (string f in files)
            {
                if (shown++ >= MaxEntries) break;
                string path = f;
                AddRow(UIIcons.AudioFile, Path.GetFileName(f), FileSize(f), UIFactory.NeonPink, () => _loader.LoadFile(path));
            }
            foreach (string d in dirs)
            {
                if (shown++ >= MaxEntries) break;
                string path = d;
                AddRow(UIIcons.Folder, Path.GetFileName(d), "", UIFactory.NeonGold, () => OpenFolder(path));
            }

            if (dirs.Count == 0 && files.Count == 0)
            {
                AddRow(UIIcons.Close, "Sin canciones aquí (MP3, OGG, WAV, AIFF)", "", new Color(0.5f, 0.55f, 0.7f), null);
            }

            _scroll.verticalNormalizedPosition = 1f;
        }

        private void AddRow(string glyph, string label, string detail, Color accent, UnityEngine.Events.UnityAction onClick)
        {
            GameObject row = new GameObject("Row", typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(_listContent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 52f;

            Image bg = row.GetComponent<Image>();
            bg.sprite = UIFactory.RoundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.95f);

            Button button = row.GetComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(2.2f, 2.2f, 2.2f, 1f);
            colors.pressedColor = new Color(3f, 3f, 3f, 1f);
            colors.colorMultiplier = 1.5f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            else button.interactable = false;

            TextMeshProUGUI icon = UIFactory.CreateIcon(row.transform, glyph, 30f, accent);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(30f, 0f);

            TextMeshProUGUI text = UIFactory.CreateLabel(row.transform, "Name", label, 26f, Color.white, TextAlignmentOptions.Left);
            Stretch(text.rectTransform, new Vector2(62f, 0f), new Vector2(-150f, 0f));
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;

            if (!string.IsNullOrEmpty(detail))
            {
                TextMeshProUGUI size = UIFactory.CreateText(row.transform, "Detail", detail, 22f, UIFactory.TextSoft, TextAlignmentOptions.Right);
                Stretch(size.rectTransform, new Vector2(0f, 0f), new Vector2(-18f, 0f));
            }
        }

        private void GoUp()
        {
            if (string.IsNullOrEmpty(_folder)) { OpenFolder(HomeFolder()); return; }
            DirectoryInfo parent = Directory.GetParent(_folder.TrimEnd(Path.DirectorySeparatorChar));
            if (parent != null) OpenFolder(parent.FullName);
        }

        private static string FileSize(string path)
        {
            try
            {
                long bytes = new FileInfo(path).Length;
                return bytes >= 1024 * 1024 ? $"{bytes / (1024f * 1024f):F1} MB" : $"{bytes / 1024} KB";
            }
            catch (Exception) { return ""; }
        }

        private static string InitialFolder()
        {
            string last = null;
            try { last = PlayerPrefs.GetString(LastFolderKey, null); } catch (Exception) { }
            if (!string.IsNullOrEmpty(last) && Directory.Exists(last)) return last;
            string music = MusicFolder();
            return Directory.Exists(music) ? music : HomeFolder();
        }

        private static string HomeFolder()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrEmpty(home) ? Directory.GetCurrentDirectory() : home;
        }

        private static string MusicFolder()
        {
            string xdg = XdgFolder("XDG_MUSIC_DIR");
            if (xdg != null) return xdg;
            string music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (!string.IsNullOrEmpty(music) && Directory.Exists(music)) return music;
            foreach (string name in new[] { "Música", "Musica", "Music" })
            {
                string p = Path.Combine(HomeFolder(), name);
                if (Directory.Exists(p)) return p;
            }
            return HomeFolder();
        }

        private static string DownloadsFolder()
        {
            string xdg = XdgFolder("XDG_DOWNLOAD_DIR");
            if (xdg != null) return xdg;
            foreach (string name in new[] { "Descargas", "Downloads" })
            {
                string p = Path.Combine(HomeFolder(), name);
                if (Directory.Exists(p)) return p;
            }
            return HomeFolder();
        }

        /// <summary>Reads ~/.config/user-dirs.dirs (Linux) for localized folders like ~/Música.</summary>
        private static string XdgFolder(string key)
        {
            try
            {
                string file = Path.Combine(HomeFolder(), ".config", "user-dirs.dirs");
                if (!File.Exists(file)) return null;
                foreach (string line in File.ReadAllLines(file))
                {
                    if (!line.StartsWith(key + "=")) continue;
                    string value = line.Substring(key.Length + 1).Trim().Trim('"').Replace("$HOME", HomeFolder());
                    return Directory.Exists(value) ? value : null;
                }
            }
            catch (Exception) { }
            return null;
        }

        // ------------------------------------------------------------------ helpers

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void StartFade(float target)
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeRoutine(target));
        }

        private IEnumerator FadeRoutine(float target)
        {
            float start = _group.alpha;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, t / 0.18f);
                yield return null;
            }
            _group.alpha = target;
            if (target <= 0f) _root.SetActive(false);
        }
    }
}
