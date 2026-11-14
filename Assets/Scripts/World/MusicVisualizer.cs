using NitroRhythm.Audio;
using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>
    /// Makes the domain dance with the music that is playing (the player's song or the domain track):
    /// - two equalizer walls of neon bars beside the track, driven by the live spectrum and following
    ///   the race leader;
    /// - the key light and the bloom breathe with the bass;
    /// - the neon edge strips of the track flash on every kick.
    /// </summary>
    public class MusicVisualizer : MonoBehaviour
    {
        private const int BarsPerWall = 30;
        private const float BarSpacing = 3.4f;
        private const float WallBehind = 24f;      // the wall starts this far behind the follow target
        private const float MaxBarHeight = 16f;
        private const float WallDistance = 34f;    // from the track edge
        private const int FirstBin = 2;
        private const int LastBin = 420;

        private Transform _follow;
        private Transform[] _bars;
        private float[] _levels;
        private int[] _bandStart;
        private Material[] _barMaterials;
        private Color _lowColor, _highColor;

        private Light _sun;
        private float _sunBase;
        private readonly System.Collections.Generic.List<Material> _edgeMaterials = new System.Collections.Generic.List<Material>();
        private readonly System.Collections.Generic.List<Color> _edgeBases = new System.Collections.Generic.List<Color>();
        private float _kick;

        public static MusicVisualizer Create(DomainTheme theme, Transform follow, float trackHalfWidth, Transform parent)
        {
            GameObject go = new GameObject("MusicVisualizer");
            go.transform.SetParent(parent, false);
            MusicVisualizer visualizer = go.AddComponent<MusicVisualizer>();
            visualizer.Build(theme, follow, trackHalfWidth);
            return visualizer;
        }

        private void Build(DomainTheme theme, Transform follow, float trackHalfWidth)
        {
            _follow = follow;
            _lowColor = theme.platformEmission;
            _highColor = theme.accent;

            int total = BarsPerWall * 2;
            _bars = new Transform[total];
            _levels = new float[BarsPerWall];
            _barMaterials = new Material[BarsPerWall];

            // Log-spaced bands: bass on the left of each wall, treble on the right.
            _bandStart = new int[BarsPerWall + 1];
            for (int b = 0; b <= BarsPerWall; b++)
            {
                float t = b / (float)BarsPerWall;
                _bandStart[b] = Mathf.RoundToInt(FirstBin * Mathf.Pow(LastBin / (float)FirstBin, t));
            }

            for (int b = 0; b < BarsPerWall; b++)
            {
                Color c = Color.Lerp(_lowColor, _highColor, b / (float)(BarsPerWall - 1));
                _barMaterials[b] = DomainMaterials.Glow(c * 0.25f, c, 1.8f);
            }

            float z = trackHalfWidth + WallDistance;
            for (int side = 0; side < 2; side++)
            {
                for (int b = 0; b < BarsPerWall; b++)
                {
                    GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.name = "EqBar";
                    Destroy(bar.GetComponent<Collider>());
                    Renderer r = bar.GetComponent<Renderer>();
                    r.sharedMaterial = _barMaterials[b];
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    bar.transform.SetParent(transform, false);
                    bar.transform.localPosition = new Vector3(-WallBehind + b * BarSpacing, -3f, side == 0 ? z : -z);
                    bar.transform.localScale = new Vector3(BarSpacing * 0.7f, 1f, 1.4f);
                    _bars[side * BarsPerWall + b] = bar.transform;
                }
            }

            _sun = RenderSettings.sun;
            _sunBase = _sun != null ? _sun.intensity : 1f;

            // Every platform has its own edge material: collect them all (once per level).
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.name != "EdgeStrip" && r.name != "BeatLine" && r.name != "BeatLineBar") continue;
                Material m = r.sharedMaterial;
                if (m == null || !m.HasProperty("_EmissionColor") || _edgeMaterials.Contains(m)) continue;
                _edgeMaterials.Add(m);
                _edgeBases.Add(m.GetColor("_EmissionColor"));
            }
        }

        private void LateUpdate()
        {
            if (_follow != null)
            {
                transform.position = new Vector3(_follow.position.x, 0f, 0f);
            }

            AudioReactiveMusicController music = AudioReactiveMusicController.Instance;
            float[] spectrum = music != null ? music.Spectrum : null;
            bool playing = music != null && music.Source != null && music.Source.isPlaying;
            float dt = Time.deltaTime;

            for (int b = 0; b < BarsPerWall; b++)
            {
                float target = 0f;
                if (playing && spectrum != null)
                {
                    int from = _bandStart[b];
                    int to = Mathf.Max(from + 1, _bandStart[b + 1]);
                    float sum = 0f;
                    for (int i = from; i < to && i < spectrum.Length; i++) sum += spectrum[i];
                    float avg = sum / (to - from);
                    // Treble bins carry far less energy: tilt the curve so the whole wall moves.
                    float tilt = Mathf.Lerp(18f, 260f, b / (float)(BarsPerWall - 1));
                    target = Mathf.Clamp01(Mathf.Sqrt(avg * tilt));
                }
                // Fast attack, slow release (classic VU feel).
                float rate = target > _levels[b] ? 18f : 3.5f;
                _levels[b] = Mathf.Lerp(_levels[b], target, 1f - Mathf.Exp(-rate * dt));
            }

            for (int side = 0; side < 2; side++)
            {
                for (int b = 0; b < BarsPerWall; b++)
                {
                    // Mirror the right wall so bass sits at the same end on both sides of the camera.
                    float level = _levels[side == 0 ? b : BarsPerWall - 1 - b];
                    Transform bar = _bars[side * BarsPerWall + b];
                    float h = 0.6f + level * MaxBarHeight;
                    Vector3 s = bar.localScale;
                    bar.localScale = new Vector3(s.x, h, s.z);
                    Vector3 p = bar.localPosition;
                    bar.localPosition = new Vector3(p.x, -3f + h * 0.5f, p.z);
                }
            }

            float bass = music != null ? music.Bass : 0f;
            _kick = Mathf.Max(bass, _kick - dt * 3f);

            if (_sun != null) _sun.intensity = _sunBase * (0.85f + 0.35f * _kick);
            NeonPostFx.SetBloomPulse(_kick);
            float edge = 0.55f + 1.3f * _kick;
            for (int i = 0; i < _edgeMaterials.Count; i++)
            {
                if (_edgeMaterials[i] != null) _edgeMaterials[i].SetColor("_EmissionColor", _edgeBases[i] * edge);
            }
        }

        private void OnDestroy()
        {
            if (_sun != null) _sun.intensity = _sunBase;
            for (int i = 0; i < _edgeMaterials.Count; i++)
            {
                if (_edgeMaterials[i] != null) _edgeMaterials[i].SetColor("_EmissionColor", _edgeBases[i]);
            }
            NeonPostFx.SetBloomPulse(0f);
        }
    }
}
