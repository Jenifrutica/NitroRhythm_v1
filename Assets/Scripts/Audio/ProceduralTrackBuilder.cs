using System;
using NitroRhythm.Data;
using UnityEngine;

namespace NitroRhythm.Audio
{
    /// <summary>
    /// Translates a music analysis into a playable rhythm track:
    /// - Low frequencies (kicks / sub-bass) carve gaps and place jump pads.
    /// - Mid/high frequencies (hats / synths) place rotating and moving hazards
    ///   and drive the villain's fire cadence.
    /// - Global BPM controls spacing, platform width and base speed.
    /// </summary>
    public static class ProceduralTrackBuilder
    {
        public static ProceduralTrackPlan Build(AudioAnalysisResult analysis, LevelDefinition level, float levelStartX)
        {
            if (analysis == null || !analysis.IsValid)
            {
                analysis = AudioAnalysisResult.CreateFlat();
            }

            ProceduralTrackPlan plan = new ProceduralTrackPlan
            {
                bpm = analysis.bpm,
                levelLength = Mathf.Clamp(analysis.duration > 1f ? analysis.duration : 40f, 40f, 220f)
            };

            // BPM scales base spacing: faster songs make tighter, denser tracks.
            float bpmFactor = Mathf.Clamp(analysis.bpm / 120f, 0.7f, 1.8f);
            float spacing = Mathf.Lerp(9f, 6f, Mathf.InverseLerp(90f, 180f, analysis.bpm));
            float width = Mathf.Clamp(level != null ? level.trackWidth : 12f, 7f, 14f);

            plan.trackHalfWidth = width * 0.5f;

            int maxSegments = 48;
            int beats = Mathf.Min(analysis.beatCount, maxSegments * 2);
            int segmentIndex = 0;
            float x = levelStartX;

            for (int beat = 0; beat < beats && segmentIndex < maxSegments; beat++)
            {
                float bass = analysis.Normalized(analysis.GetBass(beat));
                float treble = analysis.Normalized(analysis.GetTreble(beat));
                bool kick = bass > 0.45f;

                TrackSegment segment = new TrackSegment
                {
                    startX = x,
                    length = spacing * Mathf.Lerp(1.15f, 0.8f, bass),
                    speedMultiplier = bpmFactor
                };

                // Low band => gaps and vertical impulses.
                if (kick)
                {
                    segment.gap = Mathf.Lerp(4f, 12f, bass);
                    segment.hasJumpPad = bass > 0.75f;
                    segment.hasRamp = bass > 0.9f;
                }
                else
                {
                    segment.gap = Mathf.Lerp(3f, 6f, bass);
                }

                // High band => hazards and speed pads.
                if (treble > 0.35f)
                {
                    segment.obstacleKind = treble > 0.7f ? ObstacleKind.StaticBarrier
                        : (treble > 0.5f ? ObstacleKind.MovingHazard : ObstacleKind.SpinningBar);
                    segment.obstacleCount = treble > 0.7f ? 2 : 1;
                    segment.obstacleZ = 0f;
                }

                if (treble > 0.45f && beat % 4 == 0)
                {
                    segment.hasSpeedPad = true;
                }

                // Difficulty adds extra hazards on later levels.
                if (level != null && level.difficulty >= 5 && beat % 3 == 0)
                {
                    segment.obstacleCount = Mathf.Max(segment.obstacleCount, 1);
                    if (segment.obstacleKind == ObstacleKind.None)
                    {
                        segment.obstacleKind = ObstacleKind.MovingHazard;
                    }
                }

                plan.segments.Add(segment);

                x += segment.length + segment.gap;
                segmentIndex++;
            }

            plan.levelLength = Mathf.Max(60f, x - levelStartX);

            return plan;
        }
    }
}
