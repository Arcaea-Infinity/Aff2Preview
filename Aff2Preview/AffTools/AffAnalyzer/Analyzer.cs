using AffTools.AffReader;

namespace AffTools.AffAnalyzer;

class Analyzer
{
    private List<NoteRaw> _noteRaws = new();

    public List<Note> Notes { get; private set; } = new();


    private ArcaeaAffReader _affReader;

    public List<float> SegmentTimings = new();

    public float totalTime;
    public float baseBpm;
    public float baseBpl;
    public float baseTimePerSegment;
    public int segmentCountInBaseBpm;

    public Analyzer(ArcaeaAffReader affReader)
    {
        _affReader = affReader;

        var global_group = affReader.Events[0] as ArcaeaAffTiming;
        if (global_group is not null)
        {
            baseBpm = global_group.Bpm;
            baseBpl = global_group.BeatsPerLine;
            baseTimePerSegment = 60 * 1000 * (int)baseBpl / baseBpm;
        }

        foreach (var ev in affReader.Events)
        {
            if (_affReader.TimingGroupProperties[ev.TimingGroup].NoInput)
                continue;

            if (ev is ArcaeaAffTap)
                totalTime = MathF.Max(totalTime, ev.Timing);

            else if (ev is ArcaeaAffArc arc)
                totalTime = MathF.Max(totalTime, arc.EndTiming);

            else if (ev is ArcaeaAffHold hd)
                totalTime = MathF.Max(totalTime, hd.EndTiming);

            else if (ev is ArcaeaAffTiming tm)
                totalTime = MathF.Max(totalTime, tm.Timing);
        }

        totalTime += baseTimePerSegment / 4;

        for (double i = 0; i < totalTime; i += baseTimePerSegment)
        {
            segmentCountInBaseBpm++;
        }
    }

    public void AnalyzeNotes()
    {
        _noteRaws.Clear();

        Dictionary<int, List<ArcaeaAffArc>> arcs = new();
        arcs.Add(0, new());
        arcs.Add(1, new());
        arcs.Add(2, new());

        foreach (var ev in _affReader.Events)
        {
            if (_affReader.TimingGroupProperties[ev.TimingGroup].NoInput)
                continue;

            if (ev is ArcaeaAffTap ev_tap)
                _noteRaws.Add(new(ev.Timing, 0));
            else if (ev is ArcaeaAffHold ev_hold)
            {
                _noteRaws.Add(new(ev.Timing, ev_hold.EndTiming - ev_hold.Timing));
            }
            else if (ev is ArcaeaAffArc ev_arc)
            {
                if (!ev_arc.IsVoid)
                    arcs[ev_arc.Color].Add(ev_arc);
                if (ev_arc.ArcTaps is not null)
                {
                    foreach (var at in ev_arc.ArcTaps)
                    {
                        _noteRaws.Add(new(at, 0));
                    }
                }
            }
        }

        foreach (var (arc_color, arc_desc) in arcs)
        {
            for (int i = arc_desc.Count - 1; i > 0; i--)
            {
                if (arc_desc[i].Timing != arc_desc[i - 1].EndTiming)
                    _noteRaws.Add(new(arc_desc[i].Timing, 0));
            }
            if (arc_desc.Any())
                _noteRaws.Add(new(arc_desc[0].Timing, 0));
        }

        _noteRaws = _noteRaws.OrderBy(x => x.TimePoint).ToList();

        Notes.Clear();
        for (int i = 0; i < _noteRaws.Count - 1; i++)
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
            float segmentRemain = Timings[Timings.Count - 1].Bpm == 0 ? totalTime - Timings[Timings.Count - 1].Timing
          : 60000 / Math.Abs(Timings[Timings.Count - 1].Bpm) * Timings[Timings.Count - 1].BeatsPerLine;
            if (segmentRemain != 0)
            {
                int n = 0;
                float j = Timings[Timings.Count - 1].Timing;
                while (j < totalTime)
                {
                    j = Timings[Timings.Count - 1].Timing + n++ * segmentRemain;
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
        List<(int, ArcaeaAffEvent)> timing_list = new();

        foreach (var ev in _affReader.Events)
        {
            if (!_affReader.TimingGroupProperties[ev.TimingGroup].NoInput)
            {
                if (ev is ArcaeaAffTap ev_tap)
                    timing_list.Add((ev_tap.Timing, ev));
                else if (ev is ArcaeaAffHold ev_hold)
                {
                    timing_list.Add((ev_hold.Timing, ev));
                }
                else if (ev is ArcaeaAffArc ev_arc)
                {
                    if (ev_arc.ArcTaps is not null)
                    {
                        foreach (var v in ev_arc.ArcTaps)
                        {
                            timing_list.Add((v, ev));
                        }
                    }
                }
            }
        }

        Dictionary<int, int> msec = new();
        for (int i = -threshold; i <= threshold; i++)
            msec.Add(i, 0);

        for (int i = 0; i < timing_list.Count; i++)
        {
            var (timing, ev) = timing_list[i];

            for (int t = 0; t < timing_list.Count; t++)
            {
                var (timingt, evt) = timing_list[t];
                if (evt == ev)
                    continue;

                var dt = timingt - timing;
                if (Math.Abs(dt) <= threshold)
                    msec[dt]++;
            }
        }

        return msec;
    }

    public static void OutputAllChartDoubleTapAnalyze(string affFolder)
    {
        var d = new DirectoryInfo(affFolder);
        int total = 0;
        int total_twin = 0;
        int ptotal = 0;
        int ptotal_twin = 0;
        int threshold = 5;

        Dictionary<int, int> dt_list = new();
        for (int i = 0; i <= threshold; i++)
            dt_list.Add(i, 0);

        foreach (var f in d.GetDirectories())
        {
            foreach (var f2 in f.GetFiles("*.aff"))
            {
                ArcaeaAffReader affReader = new();
                affReader.Parse(f2.FullName);

                Analyzer analyzer = new(affReader);
                var result = analyzer.GetChartQuality(threshold);

                total_twin += result[0];

                if (result[1] > 0)
                {
                    ptotal++;
                    for (int i = 1; i <= threshold; i++)
                        ptotal_twin += result[i];

                    for (int i = 0; i <= threshold; i++)
                        dt_list[i] += result[i];

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
        Console.WriteLine($"total problem doubles: {ptotal_twin}/{total_twin + ptotal_twin}");
        for (int i = 1; i <= threshold; i++)
            Console.WriteLine($"total {i}ms: {dt_list[i]}");
    }

}
