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
    }
}
