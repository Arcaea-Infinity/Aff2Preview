using System.Numerics;

namespace AffTools.AffReader
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
        public string? Reason;

        public ArcaeaAffFormatException(string? reason) : base(reason)
        {
            Reason = reason;
        }

        public ArcaeaAffFormatException(string content, int line)
        {
            Console.WriteLine($"在第 {line} 行发生读取错误。\n{content}");
        }

        public ArcaeaAffFormatException(EventType type, string content, int line)
        {
            Console.WriteLine($"在第 {line} 行处的 {type} 型事件中发生读取错误。\n{content}");
        }

        public ArcaeaAffFormatException(EventType type, string content, int line, string? reason)
        {
            Console.WriteLine($"在第 {line} 行处的 {type} 型事件中发生读取错误。\n{content}\n{reason}");
        }
    }

    public class ArcaeaAffEvent
    {
        public int Timing;

        public EventType Type;

        public int TimingGroup;
    }

    public class ArcaeaAffArc : ArcaeaAffEvent
    {
        public int EndTiming;

        public float XStart;

        public float XEnd;

        public string LineType = null!;

        public float YStart;

        public float YEnd;

        public int Color;

        public bool IsVoid;

        public List<int>? ArcTaps;

        public bool NoInput;

        public bool HasHead = true;

        public static ArcLineType ToArcLineType(string type)
        {
            return type switch
            {
                "b" => ArcLineType.B,
                "s" => ArcLineType.S,
                "si" => ArcLineType.Si,
                "so" => ArcLineType.So,
                "sisi" => ArcLineType.SiSi,
                "siso" => ArcLineType.SiSo,
                "sosi" => ArcLineType.SoSi,
                "soso" => ArcLineType.SoSo,
                _ => ArcLineType.S
            };
        }
    }

    public class ArcaeaAffCamera : ArcaeaAffEvent
    {
        public Vector3 Move;

        public Vector3 Rotate;

        public string CameraType = null!;

        public int Duration;
    }

    public class ArcaeaAffHold : ArcaeaAffEvent
    {
        public int EndTiming;

        public int Track;

        public bool NoInput;
    }

    public class ArcaeaAffSceneControl : ArcaeaAffEvent
    {
        public string SceneControlTypeName = null!;

        public List<object> Parameters = null!;
    }

    public class ArcaeaAffTap : ArcaeaAffEvent
    {
        public int Track;

        public bool NoInput;
    }

    public class ArcaeaAffTiming : ArcaeaAffEvent
    {
        public float Bpm;

        public float BeatsPerLine;
    }

    public enum EventType
    {
        Timing,
        Tap,
        Hold,
        Arc,
        Camera,
        Unknown,
        SceneControl,
        TimingGroup,
        TimingGroupEnd
    }

    public class TimingGroupProperties
    {
        public bool NoInput;
    }
}