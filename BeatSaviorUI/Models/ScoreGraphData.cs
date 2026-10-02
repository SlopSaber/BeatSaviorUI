using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatSaviorUI.Models
{
    internal sealed class ScoreGraphData
    {
        private const int MaxGraphSegments = 240;

        public readonly struct Segment
        {
            public readonly float StartTime;
            public readonly float StartValue;
            public readonly float EndTime;
            public readonly float EndValue;

            public Segment(float startTime, float startValue, float endTime, float endValue)
            {
                StartTime = startTime;
                StartValue = startValue;
                EndTime = endTime;
                EndValue = endValue;
            }
        }

        public readonly struct HorizontalLabel
        {
            public readonly float Value;
            public readonly float Red;
            public readonly float Green;
            public readonly float Blue;

            public HorizontalLabel(float value)
            {
                Value = value;
                Red = 1f;
                Green = 0.2f + value * 0.8f;
                Blue = Green;
            }
        }

        public readonly struct VerticalLabel
        {
            public readonly float Time;
            public readonly float Value;
            public readonly string Text;
            public readonly bool IsFailed;

            public VerticalLabel(float time, float value, string text, bool isFailed = false)
            {
                Time = time;
                Value = value;
                Text = text;
                IsFailed = isFailed;
            }
        }

        public bool HasData { get; private set; }
        public float ScoreOffset { get; private set; }
        public IReadOnlyList<Segment> Segments { get; private set; } = Array.Empty<Segment>();
        public IReadOnlyList<HorizontalLabel> HorizontalLabels { get; private set; } = Array.Empty<HorizontalLabel>();
        public IReadOnlyList<VerticalLabel> VerticalLabels { get; private set; } = Array.Empty<VerticalLabel>();
        public Exception? Error { get; private set; }

        public static ScoreGraphData Prepare(Dictionary<float, float> graph, IReadOnlyList<NoteSnapshot> notes, bool won)
        {
            var data = new ScoreGraphData();
            var segments = new List<Segment>();
            var horizontalLabels = new List<HorizontalLabel>();
            var verticalLabels = new List<VerticalLabel>();

            try
            {
                if (graph.Count == 0)
                    return data;

                var misses = new List<float>();
                foreach (var note in notes)
                {
                    if (note.IsAMiss && misses.Count < 4)
                        misses.Add((float)Math.Truncate(note.Time));
                }

                var scoreOffset = graph.Min(entry => entry.Value);
                if (scoreOffset >= 0.93f)
                    scoreOffset = 0.93f;
                else if (scoreOffset >= 0.85f)
                    scoreOffset = 0.8f;
                else
                    scoreOffset -= 0.1f;

                data.ScoreOffset = scoreOffset;
                data.HasData = true;

                var increment = 0.1f;
                if (scoreOffset == 0.93f)
                    increment = 0.01f;
                else if (scoreOffset == 0.8f)
                    increment = 0.05f;

                for (float value = 0; value <= 1 + increment - 0.001f; value += increment)
                {
                    if (value >= scoreOffset - 0.001f)
                        horizontalLabels.Add(new HorizontalLabel(value));
                }

                var sampleStep = Math.Max(1, (int)Math.Ceiling((graph.Count - 1f) / MaxGraphSegments));
                var pointIndex = 0;
                var lastGraphEntry = -1f;
                var lastDrawnValue = 0f;
                var lastKey = 0f;
                var lastValue = 0f;
                // Sampling and failure markers follow the source dictionary's enumeration order.
                foreach (var point in graph)
                {
                    lastKey = point.Key;
                    lastValue = point.Value;
                    if (pointIndex == 0)
                    {
                        lastGraphEntry = point.Key;
                        lastDrawnValue = point.Value;
                    }
                    else if (pointIndex % sampleStep == 0)
                    {
                        segments.Add(new Segment(lastGraphEntry, lastDrawnValue, point.Key, point.Value));
                        lastGraphEntry = point.Key;
                        lastDrawnValue = point.Value;
                    }
                    pointIndex++;
                }
                if (lastGraphEntry != lastKey)
                    segments.Add(new Segment(lastGraphEntry, lastDrawnValue, lastKey, lastValue));

                var drops = new List<float>();
                var labelsLeft = 3;
                if (misses.Count != labelsLeft)
                    drops = FindScoreDrops(graph, scoreOffset);

                if (misses.Count < 4)
                {
                    for (var i = 0; i < misses.Count; i++)
                    {
                        verticalLabels.Add(new VerticalLabel(misses[i], graph[misses[i]], "Missed"));
                        labelsLeft--;
                    }
                }

                for (var i = 0; i < drops.Count && labelsLeft > 0; i++)
                {
                    if (misses.Count < 4 && !misses.Contains(drops[i]) || misses.Count >= 4)
                    {
                        verticalLabels.Add(new VerticalLabel(drops[i], graph[drops[i]], "Score drop"));
                        labelsLeft--;
                    }
                }

                if (!won)
                    verticalLabels.Add(new VerticalLabel(graph.Last().Key, graph.Last().Value, "Failed", true));
            }
            catch (Exception error)
            {
                // Keep completed descriptors so an invalid marker does not discard the graph.
                data.Error = error;
            }

            data.Segments = segments.ToArray();
            data.HorizontalLabels = horizontalLabels.ToArray();
            data.VerticalLabels = verticalLabels.ToArray();
            return data;
        }

        private static List<float> FindScoreDrops(Dictionary<float, float> graph, float scoreOffset)
        {
            var dropWindow = 2;
            var firstBeat = (int)graph.First().Key;
            var lastBeat = (int)graph.Last().Key;
            var closestToDropWindow = firstBeat;
            var dropSpacing = 4;
            var dropDelta = -0.01f;
            var drops = new List<(float, float)>();
            var result = new List<float>();

            if (scoreOffset == 0.93f)
                dropDelta = 0.0025f;
            else if (scoreOffset == 0.8f)
                dropDelta = 0.005f;

            for (var i = firstBeat + dropWindow + 5; i < lastBeat; i++)
            {
                if (graph.ContainsKey(i - dropWindow))
                    closestToDropWindow = i - dropWindow;

                if (graph.ContainsKey(i))
                {
                    var diff = graph[i] - graph[closestToDropWindow];
                    if (diff < dropDelta)
                    {
                        drops.Add((i, diff));
                        i += dropSpacing;
                    }
                }
            }

            drops = drops.OrderBy(entry => entry.Item2).Take(3).ToList();
            while (result.Count < 3 && drops.Count > 0)
            {
                result.Add(drops.First().Item1);
                drops.RemoveAt(0);
            }

            return result;
        }
    }
}
