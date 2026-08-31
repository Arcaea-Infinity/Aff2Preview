using System.Numerics;

using AffTools.AffReader;
using AffTools.MyGraphics;

namespace AffTools.AffAnalyzer;

internal class Analyzer
{
    private List<NoteRaw> _noteRaws = new();

    public List<TimingNote> Notes { get; private set; } = new();

    private readonly ArcaeaAffReader _affReader;

    public readonly List<float> SegmentTimings = new();

    public float totalTime;
    public float realTotalTime;
    public float baseBpm;
    public float baseBpl;
    public float baseTimePerSegment;
    public int segmentCountInBaseBpm;
    
    public readonly Dictionary<int, int> timingCombos = new();
    public readonly Dictionary<int, int> timingTaps = new();

    public int Tap = 0;
    public int Hold = 0;
    public readonly List<int> Arc = new() { 0, 0, 0, 0 };
    public int ArcTap = 0;
    public int Total = 0;
    public int TapTotal => Tap + ArcTap;
    public int ArcTotal => Arc.Sum();

    public Analyzer(ArcaeaAffReader affReader, float? baseBpmOverride = null)
    {
        _affReader = affReader;

        var globalTimingGroup = affReader.Events
            .OfType<ArcaeaAffTiming>()
            .FirstOrDefault(timing => timing.TimingGroup == 0);
        if (globalTimingGroup is null)
            throw new InvalidOperationException("谱面缺少主 TimingGroup 的 timing 事件");

        baseBpm = baseBpmOverride is > 0
            ? baseBpmOverride.Value
            : Math.Abs(globalTimingGroup.Bpm);
        baseBpl = globalTimingGroup.BeatsPerLine;
        if (baseBpm <= 0 || baseBpl <= 0)
            throw new InvalidOperationException("谱面的基准 BPM 和 BeatsPerLine 必须大于 0");

        baseTimePerSegment = 60 * 1000 * baseBpl / baseBpm;

        foreach (var ev in affReader.Events)
        {
            if (IsGroupNoInput(ev.TimingGroup))
                continue;

            totalTime = ev switch
            {
                ArcaeaAffTap       => MathF.Max(totalTime, ev.Timing),
                ArcaeaAffArc arc   => MathF.Max(totalTime, arc.EndTiming),
                ArcaeaAffHold hd   => MathF.Max(totalTime, hd.EndTiming),
                ArcaeaAffTiming tm => MathF.Max(totalTime, tm.Timing),
                _                  => totalTime
            };
        }

        realTotalTime = totalTime;
        totalTime += baseTimePerSegment / 4;

        for (double i = 0; i < totalTime; i += baseTimePerSegment)
        {
            segmentCountInBaseBpm++;
        }
    }

    /// <summary>
    /// To pair the scenecontrol statement
    /// </summary>
    /// <returns></returns>
    public List<(ArcaeaAffSceneControl, ArcaeaAffSceneControl)> GetPairEnwidenLanes()
    {
        var list = new List<ArcaeaAffSceneControl>();
        var result = new List<(ArcaeaAffSceneControl, ArcaeaAffSceneControl)>();

        foreach (var affEvent in _affReader.Events)
        {
            if (affEvent.Type != EventType.SceneControl) continue;
            if ((affEvent as ArcaeaAffSceneControl)?.SceneControlTypeName != "enwidenlanes") continue;

            list.Add((ArcaeaAffSceneControl)affEvent);
        }

        if (list.Count % 2 != 0) // Pair the enwidenlane
        {
            var end = new ArcaeaAffSceneControl
            {
                Timing = _affReader.Events.Last().Timing,
                Type = EventType.SceneControl,
                Parameters = new List<object>() { 0, 0 },
                SceneControlTypeName = "enwidenlanes"
            };
            list.Add(end);
        }

        for (int i = 0; i < list.Count - 1; i++)
        {
            var statement = list[i];
            if (Convert.ToInt32(statement.Parameters[1]) != 1) continue;
            if (Convert.ToInt32(list[i + 1].Parameters[1]) == 0) result.Add((statement, list[i + 1]));
        }
        return result;
    }

    public List<(int, int)> Get4LaneInterval(int? end)
    {
        var list = new List<ArcaeaAffSceneControl>();
        var result = new List<(int, int)>();

        foreach (var affEvent in _affReader.Events)
        {
            if (affEvent.Type != EventType.SceneControl) continue;
            if ((affEvent as ArcaeaAffSceneControl)?.SceneControlTypeName != "enwidenlanes") continue;

            list.Add((ArcaeaAffSceneControl)affEvent);
        }


        if (list.Count == 0)
        {
            result.Add((0, end ?? _affReader.Events.Last().Timing));
            return result;
        }

        var startSegment = 0;
        var pair = GetPairEnwidenLanes();
        foreach (var statement in list)
        {
            if (pair.TrueForAll(x => x.Item1.Timing != startSegment && x.Item2.Timing != statement.Timing))
                result.Add((startSegment, statement.Timing));
            startSegment = statement.Timing;
        }

        if (Convert.ToInt32(list.Last().Parameters[1]) == 0)
        {
            result.Add((list.Last().Timing, end ?? _affReader.Events.Last().Timing));
        }

        return result;
    }

    public void AnalyzeNotes()
    {
        _noteRaws.Clear();

        Dictionary<(int TimingGroup, int Color), List<ArcaeaAffArc>> arcColors = new();

        foreach (var ev in _affReader.Events)
        {
            if (_affReader.TimingGroupProperties[ev.TimingGroup].NoInput)
                continue;

            switch (ev)
            {
                case ArcaeaAffTap evTap:
                    _noteRaws.Add(new(ev.Timing, 0, ev.TimingGroup));
                    break;
                case ArcaeaAffHold evHold:
                    _noteRaws.Add(new(ev.Timing, evHold.EndTiming - evHold.Timing, ev.TimingGroup));
                    break;
                case ArcaeaAffArc evArc:
                {
                    if (!evArc.IsVoid)
                    {
                        var key = (evArc.TimingGroup, evArc.Color);
                        if (!arcColors.TryGetValue(key, out var arcs))
                        {
                            arcs = new List<ArcaeaAffArc>();
                            arcColors.Add(key, arcs);
                        }
                        arcs.Add(evArc);
                    }

                    if (evArc.ArcTaps is not null)
                    {
                        foreach (var at in evArc.ArcTaps)
                        {
                            _noteRaws.Add(new(at, 0, ev.TimingGroup));
                        }
                    }

                    break;
                }
            }
        }

        foreach (var (_, arcList) in arcColors)
        {
            for (var i = arcList.Count - 1; i > 0; i--)
            {
                var arc = arcList[i];
                var prev = arcList[i - 1];
                if (arc.Timing != prev.EndTiming)
                {
                    _noteRaws.Add(new(arc.Timing, 0, arc.TimingGroup));
                }
            }
            if (arcList.Any())
                _noteRaws.Add(new(arcList[0].Timing, 0, arcList[0].TimingGroup));
        }

        _noteRaws = _noteRaws.OrderBy(x => x.TimePoint).ToList();

        Notes.Clear();
        for (var i = 0; i < _noteRaws.Count - 1; i++)
        {
            var dt = _noteRaws[i + 1].TimePoint - _noteRaws[i].TimePoint;
            if (dt <= 3)
                continue;

            var currBpm = Math.Abs(GetCurrentTiming(
                _noteRaws[i].TimePoint,
                _noteRaws[i].TimingGroup).Bpm);
            TimingNote n = new();
            if (!n.Analyze(_noteRaws[i].TimePoint, dt, currBpm))
                n.Analyze(_noteRaws[i].TimePoint, dt, baseBpm);
            Notes.Add(n);
        }

    }

    private ArcaeaAffTiming GetCurrentTiming(int timing, int timingGroup)
    {
        var timings = _affReader.Events
          .OfType<ArcaeaAffTiming>()
          .Where(ev => ev.TimingGroup == timingGroup);

        return timings.Last(x => x.Timing <= timing);
    }

    public bool IsGroupNoInput(int timingGroup)
    {
        return _affReader.TimingGroupProperties[timingGroup].NoInput;
    }

    public int CalcSingleHold(int start, int end, bool hasHead, float bpm, float tpdf)
    {
        if (start >= end) return 0;

        // Do NOT check "Code Optimization" in the Project Properties!!!
        // I HATE FLOATING POINT ERROR...
        float d = end - start;
        float unit = bpm >= 256 ? 60000 : 30000;
        unit /= bpm;
        unit /= tpdf;
        float cf = d / unit;
        var ci = (int)cf;
        return ci <= 1 ? 1 : hasHead ? ci - 1 : ci;
    }

    public int GetCombo(int timing)
    {
        if (timingCombos.ContainsKey(timing))
            return timingCombos[timing];

        try
        {
            var t = timingCombos.Last(x => x.Key <= timing);
            return t.Value;
        }
        catch
        {
            return 0;
        }
    }

    public float GetTapDensity(int timing, int threshold)
    {
        try
        {
            var t = timingTaps.Where(x => (timing - threshold <= x.Key && x.Key < timing + threshold)).ToList();
            if (!t.Any())
                return 0;
            float density = (float)t.Sum(x => x.Value) * 1000 / (threshold * 2);
            return density;
        }
        catch
        {
            return 0;
        }
    }

    public void CountNotes()
    {
        Tap = 0;
        Hold = 0;
        ArcTap = 0;
        Total = 0;
        timingCombos.Clear();
        timingTaps.Clear();
        for (var color = 0; color < Arc.Count; color++)
            Arc[color] = 0;

        List<ArcaeaAffArc> arcs = _affReader.Events
            .OfType<ArcaeaAffArc>()
            .Where(arc => !arc.IsVoid && !IsGroupNoInput(arc.TimingGroup))
            .ToList();

        foreach (var arc in arcs)
            arc.HasHead = true;

        arcs.Sort((a, b) => a.Timing.CompareTo(b.Timing));
        ArcaeaAffArc[] scra = arcs.ToArray();
        Array.Sort(scra, (a, b) => a.EndTiming.CompareTo(b.EndTiming));
        int m = scra.Length;
        int i = 0;

        List<int> timingNotePoints = new();

        foreach (ArcaeaAffArc evArc in arcs)
        {
            for (var j = i; j < m; ++j)
            {
                ArcaeaAffArc prev = scra[j];
                if (prev.EndTiming <= evArc.Timing - 10)
                {
                    i = j;
                }
                else if (prev.EndTiming >= evArc.Timing + 10)
                {
                    break;
                }
                else if (evArc != prev && evArc.YStart == prev.YEnd && Math.Abs(evArc.XStart - prev.XEnd) < 0.1)
                {
                    evArc.HasHead = false;
                }
            }
        }

        List<int> tapTimings = new();

        foreach (var ev in _affReader.Events)
        {
            if (IsGroupNoInput(ev.TimingGroup))
                continue;

            switch (ev)
            {
                case ArcaeaAffTap:
                    timingNotePoints.Add(ev.Timing);
                    Tap++;
                    Total++;
                    tapTimings.Add(ev.Timing);
                    break;
                case ArcaeaAffHold evHold:
                {
                    var timing = GetCurrentTiming(evHold.Timing, evHold.TimingGroup);
                    var t = CalcSingleHold(evHold.Timing, evHold.EndTiming, true, timing.Bpm, _affReader.TimingPointDensityFactor);
                    for (var tx = 0; tx < t; tx++)
                    {
                        timingNotePoints.Add(evHold.Timing + (evHold.EndTiming - evHold.Timing) * tx / t);
                    }
                    Hold += t;
                    Total += t;
                    break;
                }
                case ArcaeaAffArc evArc:
                {
                    ArcTap += evArc.ArcTaps?.Count ?? 0;

                    for (var x = 0; x < evArc.ArcTaps?.Count; x++)
                    {
                        timingNotePoints.Add(evArc.ArcTaps[x]);
                        Total++;
                        tapTimings.Add(evArc.ArcTaps[x]);
                    }

                    if (evArc.IsVoid)
                        continue;

                    var timing = GetCurrentTiming(evArc.Timing, evArc.TimingGroup);
                    var t = CalcSingleHold(evArc.Timing, evArc.EndTiming, evArc.HasHead, timing.Bpm, _affReader.TimingPointDensityFactor);
                    for (var tx = 0; tx < t; tx++)
                    {
                        timingNotePoints.Add(evArc.Timing + (evArc.EndTiming - evArc.Timing) * tx / t);
                    }
                    Arc[evArc.Color] += t;
                    Total += t;
                    break;
                }
            }
        }

        tapTimings = tapTimings.OrderBy(x => x).ToList();

        foreach (var timing in tapTimings)
        {
            if (timingTaps.ContainsKey(timing))
                timingTaps[timing]++;

            else
                timingTaps[timing] = 1;
        }

        timingNotePoints = timingNotePoints.OrderBy(x => x).ToList();
        int total = 0;
        foreach (var timing in timingNotePoints)
        {
            if (timingCombos.ContainsKey(timing))
            {
                total++;
                timingCombos[timing]++;
            }
            else
            {
                total++;
                timingCombos[timing] = total;
            }
        }

        Console.WriteLine($"F{Tap} L{Hold} A{ArcTotal} (blue{Arc[0]} red{Arc[1]} green{Arc[2]}) S{ArcTap} t:{Total}");
    }

    public void AnalyzeSegments()
    {
        SegmentTimings.Clear();

        List<ArcaeaAffTiming> Timings = _affReader.Events
          .OfType<ArcaeaAffTiming>()
          .Where(ev => ev.TimingGroup == 0)
          .ToList();

        for (int i = 0; i < Timings.Count - 1; ++i)
        {
            float segment = Timings[i].Bpm == 0 ?
                Timings[i + 1].Timing - Timings[i].Timing :
                60000 / Math.Abs(Timings[i].Bpm) * Timings[i].BeatsPerLine;
            if (segment == 0) continue;
            int n = 0;
            while (true)
            {
                float j = Timings[i].Timing + n++ * segment;
                if (j >= Timings[i + 1].Timing)
                    break;
                SegmentTimings.Add(j);
            }
        }

        if (Timings.Count >= 1)
        {
            float segmentRemain = Timings[^1].Bpm == 0 ? totalTime - Timings[^1].Timing
                : 60000 / Math.Abs(Timings[^1].Bpm) * Timings[^1].BeatsPerLine;
            if (segmentRemain != 0)
            {
                int n = 0;
                float j = Timings[^1].Timing;
                while (j < totalTime)
                {
                    j = Timings[^1].Timing + n++ * segmentRemain;
                    SegmentTimings.Add(j);
                }
            }
        }

        if (Timings.Count >= 1 && Timings[0].Bpm != 0 && Timings[0].BeatsPerLine != 0)
        {
            float t = 0;
            float delta = 60000 / Math.Abs(Timings[0].Bpm) * Timings[0].BeatsPerLine;
            int n = 0;
            if (delta != 0)
            {
                while (t >= -3000)
                {
                    n++;
                    t = -n * delta;
                    SegmentTimings.Insert(0, t);
                }
            }
        }
    }


    public Dictionary<int, int> GetChartQuality(int threshold)
    {
        List<(int, ArcaeaAffEvent)> timingList = new();

        foreach (var ev in _affReader.Events)
        {
            if (IsGroupNoInput(ev.TimingGroup))
                continue;

            switch (ev)
            {
                case ArcaeaAffTap evTap:
                    timingList.Add((evTap.Timing, ev));
                    break;
                case ArcaeaAffHold evHold:
                    timingList.Add((evHold.Timing, ev));
                    break;
                case ArcaeaAffArc evArc:
                {
                    if (evArc.ArcTaps is not null)
                    {
                        timingList.AddRange(evArc.ArcTaps.Select(v => (v, ev)));
                    }

                    break;
                }

            }
        }

        Dictionary<int, int> msec = new();
        for (int i = -threshold; i <= threshold; i++)
            msec.Add(i, 0);

        for (int i = 0; i < timingList.Count; i++)
        {
            var (timing, ev) = timingList[i];

            for (int t = 0; t < timingList.Count; t++)
            {
                var (timingt, evt) = timingList[t];
                if (evt == ev)
                    continue;

                var dt = timingt - timing;
                if (Math.Abs(dt) <= threshold)
                    msec[dt]++;
            }
        }

        return msec;
    }

    /// <summary>
    /// Analyze charts for double-tap alignment problem
    /// </summary>
    /// <param name="affFolder"></param>
    public static void OutputAllChartDoubleTapAnalyze(string affFolder)
    {
        var d = new DirectoryInfo(affFolder);
        int total = 0;
        int totalTwin = 0;
        int ptotal = 0;
        int ptotalTwin = 0;
        int threshold = 5;

        Dictionary<int, int> dtList = new();
        for (int i = 0; i <= threshold; i++)
            dtList.Add(i, 0);

        foreach (var f in d.GetDirectories())
        {
            foreach (var f2 in f.GetFiles("*.aff"))
            {
                ArcaeaAffReader affReader = new();
                affReader.Parse(f2.FullName);

                Analyzer analyzer = new(affReader);
                var result = analyzer.GetChartQuality(threshold);

                totalTwin += result[0];

                if (result[1] > 0)
                {
                    ptotal++;
                    for (int i = 1; i <= threshold; i++)
                        ptotalTwin += result[i];

                    for (int i = 0; i <= threshold; i++)
                        dtList[i] += result[i];

                    Console.WriteLine(f2.FullName);
                    Console.WriteLine($"dt: " +
                        string.Join(" ",
                        result.Where(k => k.Key >= 0 && k.Value > 0).
                        Select(k => $"[{k.Key},{k.Value}]")
                        ));
                }

                total++;
            }
        }

        Console.WriteLine($"total problem charts: {ptotal}/{total}");
        Console.WriteLine($"total problem doubles: {ptotalTwin}/{totalTwin + ptotalTwin}");
        for (int i = 1; i <= threshold; i++)
            Console.WriteLine($"total {i}ms: {dtList[i]}");
    }

}
