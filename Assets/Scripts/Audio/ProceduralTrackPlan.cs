using System.Collections.Generic;

namespace NitroRhythm.Audio
{
    public enum ObstacleKind
    {
        None,
        SpinningBar,
        MovingHazard,
        StaticBarrier
    }

    /// <summary>One procedurally generated platform of the rhythm track.</summary>
    public class TrackSegment
    {
        public float startX;
        public float length = 8f;
        public float gap = 4f;
        public bool hasJumpPad;
        public bool hasSpeedPad;
        public bool hasRamp;
        public ObstacleKind obstacleKind = ObstacleKind.None;
        public int obstacleCount;
        public float obstacleZ;
        public float speedMultiplier = 1f;
    }

    /// <summary>Complete generated layout consumed by LevelManager.</summary>
    public class ProceduralTrackPlan
    {
        public float bpm = 120f;
        public float trackHalfWidth = 7f;
        public float levelLength = 160f;
        public readonly List<TrackSegment> segments = new List<TrackSegment>();

        /// <summary>Speed the layout is timed for: at this speed every platform edge is reached on a beat.</summary>
        public float cruiseSpeed = 34f;

        /// <summary>World X of every beat of the section (beat lines on the track).</summary>
        public readonly List<float> beatXs = new List<float>();
    }
}
