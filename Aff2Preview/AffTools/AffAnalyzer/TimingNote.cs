namespace AffTools.AffAnalyzer;

public struct NoteRaw
{
    public int TimePoint = 0;
    public int Duration = 0;
    public int TimingGroup = 0;

    public NoteRaw(int timePoint, int duration, int timingGroup = 0)
    {
        TimePoint = timePoint;
        Duration = duration;
        TimingGroup = timingGroup;
    }
}

public class TimingNote
{
    public int TimePoint { get; set; } = 0;
    public int Duration { get; set; } = 0;
    public int Divide { get; set; } = -1;

    public bool beyondFull = false;
    public bool hasDot = false;
    public bool isTriplet = false;

    public float InvDuration
    {
        get
        {
            if (Duration == 0) return 0;
            return 1000 / Duration;
        }
    }

    private static bool isDoubleEqual(double a, double b, double e) => Math.Abs(a - b) <= e;

    public TimingNote(int timeing, int length, double bpm)
    {
        Analyze(timeing, length, bpm);
    }

    public TimingNote()
    {
    }

    public bool Analyze(int timing, int length, double bpm)
    {
        var threshold = 3.5;

        Duration = length;
        TimePoint = timing;
        var time_full_note = 60 * 1000 * 4 / bpm;
        if (length > time_full_note)
        {
            beyondFull = true;
            return true;
        }

        if (isDoubleEqual(length, time_full_note, threshold))
        {
            Divide = 1;
            return true;
        }

        for (var i = 2; i <= 64;)
        {
            var t_len = time_full_note / i;
            var t_dot_len = t_len * 1.5;

            if (isDoubleEqual(length, t_len, threshold))
            {
                Divide = i;
                return true;
            }

            if (isDoubleEqual(length, t_dot_len, threshold))
            {
                Divide = i;
                hasDot = true;
                return true;
            }

            i += i switch
            {
                < 4   => 1,
                < 28  => 2,
                < 32  => 4,
                <= 64 => 8,
                _     => 1
            };
        }

        return false;
    }
}
