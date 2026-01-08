namespace AffTools.AffAnalyzer;

internal struct NoteRaw(int timePoint, int duration)
{
    public int TimePoint = timePoint;
    public int Duration = duration;
}

internal class Note
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

    private static bool IsDoubleEqual(double a, double b, double e) => Math.Abs(a - b) <= e;

    public Note(int timeing, int length, double bpm)
    {
        Analyze(timeing, length, bpm);
    }

    public Note()
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

        if (IsDoubleEqual(length, time_full_note, threshold))
        {
            Divide = 1;
            return true;
        }

        for (var i = 2; i <= 64;)
        {
            var t_len = time_full_note / i;
            var t_dot_len = t_len * 1.5;

            if (IsDoubleEqual(length, t_len, threshold))
            {
                Divide = i;
                return true;
            }

            if (IsDoubleEqual(length, t_dot_len, threshold))
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