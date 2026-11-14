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

            // Beat-locked layout: one beat = the distance the kart covers in one beat at cruise
            // speed, so every gap/jump edge is reached exactly on a beat of the music and the level
            // lasts as long as its section of the song.
            float beatDistance = plan.cruiseSpeed * analysis.SecondsPerBeat;

            // Smooth difficulty curve (hallazgo P9): the first playable domains are
            // forgiving (smaller gaps, fewer hazards, longer safe runway) and ramp up
            // to the full challenge by the last domain.
            float ramp = level != null ? Mathf.Clamp01((level.difficulty - 1) / 6f) : 1f;
            float gapScale = Mathf.Lerp(0.6f, 1f, ramp);
            float trebleGate = Mathf.Lerp(0.5f, 0.35f, ramp);
            int warmupSegments = Mathf.RoundToInt(Mathf.Lerp(3f, 1f, ramp));

            int maxSegments = 48;
            // Long sections (player songs, fast tempos) fold several beats into one segment so the
            // whole section shapes the track instead of only its first 48 beats.
            int segmentTotal = Mathf.Min(maxSegments, analysis.beatCount);
            float beatsPerSegment = analysis.beatCount / (float)Mathf.Max(1, segmentTotal);
            int segmentIndex = 0;
            float x = levelStartX;
            for (int b = 0; b <= analysis.beatCount; b++) plan.beatXs.Add(levelStartX + b * beatDistance);

            while (segmentIndex < segmentTotal)
            {
                int beat = segmentIndex;   // pattern counter (pads every 4th, extra hazards every 3rd)
                int firstBeat = Mathf.FloorToInt(segmentIndex * beatsPerSegment);
                int endBeat = Mathf.Max(firstBeat + 1, Mathf.FloorToInt((segmentIndex + 1) * beatsPerSegment));
                float bass = 0f, treble = 0f;
                for (int b = firstBeat; b < endBeat && b < analysis.beatCount; b++)
                {
                    bass = Mathf.Max(bass, analysis.Normalized(analysis.GetBass(b)));
                    treble = Mathf.Max(treble, analysis.Normalized(analysis.GetTreble(b)));
                }
                bool kick = bass > 0.45f;

                TrackSegment segment = new TrackSegment
                {
                    startX = x,
                    length = spacing,
                    speedMultiplier = bpmFactor
                };

                // Low band => gaps and vertical impulses.
                if (kick)
                {
                    segment.gap = Mathf.Max(2.5f, Mathf.Lerp(4f, 12f, bass) * gapScale);
                    segment.hasJumpPad = bass > 0.75f;
                    segment.hasRamp = bass > 0.9f;
                }
                else
                {
                    segment.gap = Mathf.Max(2.5f, Mathf.Lerp(3f, 6f, bass) * gapScale);
                }

                // High band => hazards and speed pads.
                if (treble > trebleGate)
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

                // Safe runway at the start of every domain: no hazards, short gaps.
                if (segmentIndex < warmupSegments)
                {
                    segment.gap = Mathf.Min(segment.gap, 3f);
                    segment.obstacleKind = ObstacleKind.None;
                    segment.obstacleCount = 0;
                    segment.hasRamp = false;
                }

                // Fit the platform + gap exactly into its beats: the gap opens on the next beat.
                float span = beatDistance * Mathf.Max(1, endBeat - firstBeat);
                segment.gap = Mathf.Min(segment.gap, span * 0.55f);
                segment.length = Mathf.Max(3.5f, span - segment.gap);
                segment.gap = Mathf.Max(0f, span - segment.length);

                plan.segments.Add(segment);

                x = levelStartX + beatDistance * endBeat;
                segmentIndex++;
            }

            plan.levelLength = Mathf.Max(60f, x - levelStartX);

            return plan;
        }
    }
}
