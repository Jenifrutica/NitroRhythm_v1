using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace NitroRhythm.Player
{
    /// <summary>
    /// Plays the pilot's skeletal clips (made by Docs/Tools/blender/rig_models.py; the FBX is imported
    /// as Generic): Idle, SteerLeft, SteerRight, Boost, Celebrate, Hit and KO, mixed with the Playables
    /// API (no AnimatorController asset needed). Gameplay code only sets the state (<see cref="Steer"/>,
    /// <see cref="Boosting"/>, <see cref="Down"/>) or fires one-shots (<see cref="PlayHit"/>,
    /// <see cref="Celebrate"/>).
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class PilotAnimator : MonoBehaviour
    {
        private static readonly string[] Order = { "Idle", "SteerLeft", "SteerRight", "Boost", "Celebrate", "Hit", "KO" };
        private static readonly bool[] Loops = { true, true, true, true, true, false, true };

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private readonly AnimationClipPlayable[] _playables = new AnimationClipPlayable[Order.Length];
        private readonly AnimationClip[] _clips = new AnimationClip[Order.Length];
        private readonly float[] _weights = new float[Order.Length];
        private bool _ready;

        private float _celebrateTimer;
        private float _hitTimer;

        // Rest positions of every bone: the FBX takes also bake bone positions (in Blender armature
        // space), which would slide the whole body away from the seat; only rotations are kept.
        private Transform[] _bones = new Transform[0];
        private Vector3[] _restPositions = new Vector3[0];

        /// <summary>-1 (full right) … +1 (full left).</summary>
        public float Steer { get; set; }
        public bool Boosting { get; set; }
        public bool Down { get; set; }

        public bool HasClips => _ready;

        /// <summary>Attaches the driver to a pilot instance of the FBX <paramref name="modelName"/> (Resources/NitroRhythm/Models).</summary>
        public static PilotAnimator Attach(Transform pilotRoot, string modelName)
        {
            if (pilotRoot == null || string.IsNullOrEmpty(modelName)) return null;
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"NitroRhythm/Models/{modelName}");
            if (clips == null || clips.Length == 0) return null;

            PilotAnimator pilot = pilotRoot.GetComponent<PilotAnimator>();
            if (pilot == null) pilot = pilotRoot.gameObject.AddComponent<PilotAnimator>();
            pilot.Setup(clips);
            return pilot;
        }

        private void Setup(AnimationClip[] clips)
        {
            if (_ready) return;
            foreach (AnimationClip clip in clips)
            {
                string shortName = clip.name.Contains("|") ? clip.name.Substring(clip.name.LastIndexOf('|') + 1) : clip.name;
                int index = System.Array.IndexOf(Order, shortName);
                if (index >= 0) _clips[index] = clip;
            }
            if (_clips[0] == null) return;   // needs at least Idle

            _bones = GetComponentsInChildren<Transform>(true);
            _restPositions = new Vector3[_bones.Length];
            for (int i = 0; i < _bones.Length; i++) _restPositions[i] = _bones[i].localPosition;

            Animator animator = GetComponent<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            _graph = PlayableGraph.Create($"Pilot_{name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);   // keeps cheering during pauses/cutscenes
            _mixer = AnimationMixerPlayable.Create(_graph, Order.Length);
            for (int i = 0; i < Order.Length; i++)
            {
                if (_clips[i] == null) continue;
                _playables[i] = AnimationClipPlayable.Create(_graph, _clips[i]);
                _graph.Connect(_playables[i], 0, _mixer, i);
            }
            _weights[0] = 1f;
            ApplyWeights();
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Pilot", animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
            _ready = true;
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        /// <summary>Flinch when hit by the villain.</summary>
        public void PlayHit()
        {
            int hit = System.Array.IndexOf(Order, "Hit");
            if (!_ready || Down || _clips[hit] == null) return;
            _hitTimer = _clips[hit].length;
            _playables[hit].SetTime(0);
            _weights[hit] = 1f;
        }

        /// <summary>Cheers for <paramref name="seconds"/> (goal, podium, being picked).</summary>
        public void Celebrate(float seconds = 2.5f)
        {
            _celebrateTimer = Mathf.Max(_celebrateTimer, seconds);
        }

        /// <summary>Current weight of a clip (tests / debugging).</summary>
        public float Weight(string clip)
        {
            int i = System.Array.IndexOf(Order, clip);
            return i >= 0 ? _weights[i] : 0f;
        }

        private void Update()
        {
            if (!_ready) return;
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-10f * dt);
            _celebrateTimer = Mathf.Max(0f, _celebrateTimer - dt);
            _hitTimer = Mathf.Max(0f, _hitTimer - dt);

            float steer = Down ? 0f : Mathf.Clamp(Steer, -1f, 1f);
            float[] target =
            {
                0f,
                Mathf.Max(0f, steer),
                Mathf.Max(0f, -steer),
                Boosting && !Down ? 0.8f : 0f,
                _celebrateTimer > 0f && !Down ? 1f : 0f,
                _hitTimer > 0f ? 1f : 0f,
                Down ? 1f : 0f,
            };
            float others = 0f;
            for (int i = 1; i < Order.Length; i++)
            {
                if (_clips[i] == null) { _weights[i] = 0f; continue; }
                _weights[i] = i == 5 && _hitTimer > 0f ? 1f : Mathf.Lerp(_weights[i], target[i], k);
                others += _weights[i];
            }
            // Normalise: Idle fills whatever the other states leave.
            if (others > 1f) for (int i = 1; i < Order.Length; i++) _weights[i] /= others;
            _weights[0] = Mathf.Max(0f, 1f - Mathf.Min(1f, others));
            ApplyWeights();

            // Loop the looping clips by hand (generic clips are imported without loop time).
            for (int i = 0; i < Order.Length; i++)
            {
                if (_clips[i] == null || !Loops[i]) continue;
                double len = Mathf.Max(0.05f, _clips[i].length);
                double t = _playables[i].GetTime();
                if (t > len) _playables[i].SetTime(t % len);
            }
        }

        private void ApplyWeights()
        {
            for (int i = 0; i < Order.Length; i++)
            {
                if (_clips[i] != null) _mixer.SetInputWeight(i, _weights[i]);
            }
        }

        private void LateUpdate()
        {
            for (int i = 1; i < _bones.Length; i++)   // [0] is this root: positioned by the kart
            {
                if (_bones[i] != null) _bones[i].localPosition = _restPositions[i];
            }
        }
    }
}
