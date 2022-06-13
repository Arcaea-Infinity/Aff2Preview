using AffTools.AffReader;

namespace AffTools.AffAnalyzer;

internal class Analyzer
{
    private List<NoteRaw> _noteRaws = new();

    public List<Note> Notes { get; private set; } = new();

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

    public Analyzer(ArcaeaAffReader affReader)
    {
        _affReader = affReader;

        var globalTimingGroup = affReader.Events[0] as ArcaeaAffTiming;
        if (globalTimingGroup is not null)
        {
            baseBpm = globalTimingGroup.Bpm;
            baseBpl = globalTimingGroup.BeatsPerLine;
            baseTimePerSegment = 60 * 1000 * (int)baseBpl / baseBpm;
        }

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

    public void AnalyzeNotes()
    {
        _noteRaws.Clear();

        Dictionary<int, List<ArcaeaAffArc>> arcColors = new();
        arcColors.Add(0, new());
        arcColors.Add(1, new());
        arcColors.Add(2, new());
        arcColors.Add(3, new());

        foreach (var ev in _affReader.Events)
        {
            if (_affReader.TimingGroupProperties[ev.TimingGroup].NoInput)
                continue;

            switch (ev)
            {
                case ArcaeaAffTap evTap:
                    _noteRaws.Add(new(ev.Timing, 0));
                    break;
                case ArcaeaAffHold evHold:
                    _noteRaws.Add(new(ev.Timing, evHold.EndTiming - evHold.Timing));
                    break;
                case ArcaeaAffArc evArc:
                {
                    if (!evArc.IsVoid)
                        arcColors[evArc.Color].Add(evArc);

                    if (evArc.ArcTaps is not null)
                    {
                        foreach (var at in evArc.ArcTaps)
                        {
                            _noteRaws.Add(new(at, 0));
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
                    _noteRaws.Add(new(arc.Timing, 0));
                }
            }
            if (arcList.Any())
                _noteRaws.Add(new(arcList[0].Timing, 0));
        }

        _noteRaws = _noteRaws.OrderBy(x => x.TimePoint).ToList();

        Notes.Clear();
        for (var i = 0; i < _noteRaws.Count - 1; i++)
        {
            var dt = _noteRaws[i + 1].TimePoint - _noteRaws[i].TimePoint;
            if (dt <= 3)
                continue;

            var currBpm = GetCurrentTiming(_noteRaws[i].TimePoint, 0).Bpm;
            Note n = new();
            if (!n.Analyze(_noteRaws[i].TimePoint, dt, currBpm))
                n.Analyze(_noteRaws[i].TimePoint, dt, baseBpm);
            Notes.Add(n);
        }

    }

    private ArcaeaAffTiming GetCurrentTiming(int timing, int timingGroup)
    {
        var timings = _affReader.Events
          .Where(ev => ev is ArcaeaAffTiming && ev.TimingGroup == timingGroup)
          .Select(x => x as ArcaeaAffTiming);

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

    public void CalcNotes()
    {
        List<ArcaeaAffArc> arcs =
            _affReader.Events.Where(x => x is ArcaeaAffArc a && !a.IsVoid && !IsGroupNoInput(x.TimingGroup))
            .Select(x => x as ArcaeaAffArc).ToList();

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

        Console.WriteLine($"F{Tap} L{Hold} A{Arc[0] + Arc[1]} (blue{Arc[0]} red{Arc[1]}) S{ArcTap} t:{Tap + Hold + Arc[0] + Arc[1] + ArcTap}");
    }

    public void AnalyzeSegments()
    {
        List<ArcaeaAffTiming> Timings = _affReader.Events
          .Where(ev => ev is ArcaeaAffTiming && ev.TimingGroup == 0)
          .Select(x => x as ArcaeaAffTiming).ToList();

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
