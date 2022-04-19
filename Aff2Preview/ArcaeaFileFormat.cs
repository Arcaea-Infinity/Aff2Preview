using System.Numerics;

namespace AimuBotCS.Modules.Arcaea.Aff2Preview
{
    public enum ArcLineType
    {
        B,
        S,
        Si,
        So,
        SiSi,
        SiSo,
        SoSi,
        SoSo
    }

    namespace Advanced
    {
        public enum SpecialType
        {
            Unknown,
            TextArea,
            Fade
        }

        public class ArcadeAffSpecial : ArcaeaAffEvent
        {
            public SpecialType SpecialType;
            public string param1;
            public string param2;
            public string param3;
        }
    }

    public class ArcaeaAffFormatException : Exception
    {
        public string Reason;

        public ArcaeaAffFormatException(string reason)
            : base(reason)
        {
            Reason = reason;
        }

        public ArcaeaAffFormatException(string content, int line)
               : base(string.Format("", line, content))
        {

        }

        public ArcaeaAffFormatException(EventType type, string content, int line)
            : base(string.Format("", line, type, content))
        {

        }

        public ArcaeaAffFormatException(EventType type, string content, int line, string reason)
            : base(string.Format("", line, type, content, reason))
        {

        }
    }

    public enum EventType
    {
        Timing,
        Tap,
        Hold,
        Arc,
        Camera,
        Unknown,
        Special,
        //v3.0.0
        TimingGroup,
        TimingGroupEnd
    }

    public class ArcaeaAffEvent
    {
        public int Timing;
        public EventType Type;

        //v3.0.0
        public int TimingGroup;
    }

    public class ArcaeaAffTiming : ArcaeaAffEvent
    {
        public float Bpm;
        public float BeatsPerLine;
    }

    public class ArcaeaAffTap : ArcaeaAffEvent
    {
        public int Track;
    }

    public class ArcaeaAffHold : ArcaeaAffEvent
    {
        public int EndTiming;
        public int Track;
    }

    public class ArcaeaAffArc : ArcaeaAffEvent
    {
        public int EndTiming;
        public float XStart;
        public float XEnd;
        public string LineType;
        public float YStart;
        public float YEnd;
        public int Color;
        public bool IsVoid;
        public List<int>? ArcTaps;

        public static ArcLineType ToArcLineType(string type)
        {
            switch (type)
            {
                case "b": return ArcLineType.B;
                case "s": return ArcLineType.S;
                case "si": return ArcLineType.Si;
                case "so": return ArcLineType.So;
                case "sisi": return ArcLineType.SiSi;
                case "siso": return ArcLineType.SiSo;
                case "sosi": return ArcLineType.SoSi;
                case "soso": return ArcLineType.SoSo;
                default: return ArcLineType.S;
            }
        }
    }

    public class ArcaeaAffCamera : ArcaeaAffEvent
    {
        public Vector3 Move, Rotate;
        public string CameraType;
        public int Duration;
    }

}
