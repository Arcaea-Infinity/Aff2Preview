using System.Numerics;
using AimuBotCS.Modules.Arcaea.Aff2Preview.Advanced;

namespace AimuBotCS.Modules.Arcaea.Aff2Preview
{
    public class AffReader
    {
        public int TotalTimingGroup = 0;
        public int CurrentTimingGroup = 0;
        public int AudioOffset;
        public List<ArcaeaAffEvent> Events = new List<ArcaeaAffEvent>();

        public AffReader()
        {
        }

        public AffReader(string path)
        {
            ParseFile(path);
        }

        private EventType DetermineType(string line)
        {
            if (line.StartsWith("(")) return EventType.Tap;
            else if (line.StartsWith("timing(")) return EventType.Timing;
            else if (line.StartsWith("hold(")) return EventType.Hold;
            else if (line.StartsWith("arc(")) return EventType.Arc;
            else if (line.StartsWith("camera(")) return EventType.Camera;
            else if (line.StartsWith("special(")) return EventType.Special;
            else if (line.StartsWith("timinggroup(")) return EventType.TimingGroup;
            else if (line.StartsWith("};")) return EventType.TimingGroupEnd;
            return EventType.Unknown;
        }

        private void ParseTiming(string line)
        {
            try
            {
                AffStringParser s = new AffStringParser(line);
                s.Skip(7);
                int tick = s.ReadInt(",");
                float bpm = s.ReadFloat(",");
                float beatsPerLine = s.ReadFloat(")");
                Events.Add(new ArcaeaAffTiming()
                {
                    Timing = tick,
                    BeatsPerLine = beatsPerLine,
                    Bpm = bpm,
                    Type = EventType.Timing,
                    TimingGroup = CurrentTimingGroup
                });
                if (MathF.Abs(bpm) >= double.MaxValue) throw new ArcaeaAffFormatException("");
                if (beatsPerLine < 0) throw new ArcaeaAffFormatException("");
                if (tick == 0 && bpm == 0) throw new ArcaeaAffFormatException("");
            }
            catch (ArcaeaAffFormatException Ex)
            {
                throw Ex;
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException("");
            }
        }

        private void ParseTap(string line)
        {
            try
            {
                AffStringParser s = new AffStringParser(line);
                s.Skip(1);
                int tick = s.ReadInt(",");
                int track = s.ReadInt(")");
                Events.Add(new ArcaeaAffTap()
                {
                    Timing = tick,
                    Track = track,
                    Type = EventType.Tap,
                    TimingGroup = CurrentTimingGroup
                });
                if (track <= 0 || track >= 5) throw new ArcaeaAffFormatException("");
            }
            catch (ArcaeaAffFormatException Ex)
            {
                throw Ex;
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException("");
            }
        }

        private void ParseHold(string line)
        {
            try
            {
                AffStringParser s = new AffStringParser(line);
                s.Skip(5);
                int tick = s.ReadInt(",");
                int endtick = s.ReadInt(",");
                int track = s.ReadInt(")");
                Events.Add(new ArcaeaAffHold()
                {
                    Timing = tick,
                    EndTiming = endtick,
                    Track = track,
                    Type = EventType.Hold,
                    TimingGroup = CurrentTimingGroup
                });
                if (track <= 0 || track >= 5) throw new ArcaeaAffFormatException("");
                if (endtick < tick) throw new ArcaeaAffFormatException("");
            }
            catch (ArcaeaAffFormatException Ex)
            {
                throw Ex;
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException("");
            }
        }

        private void ParseArc(string line)
        {
            try
            {
                AffStringParser s = new AffStringParser(line);
                s.Skip(4);
                int tick = s.ReadInt(",");
                int endtick = s.ReadInt(",");
                float startx = s.ReadFloat(",");
                float endx = s.ReadFloat(",");
                string linetype = s.ReadString(",");
                float starty = s.ReadFloat(",");
                float endy = s.ReadFloat(",");
                int color = s.ReadInt(",");
                s.ReadString(",");
                bool isvoid = s.ReadBool(")");
                List<int> arctap = null;
                if (s.Current != ";")
                {
                    arctap = new List<int>();
                    isvoid = true;
                    while (true)
                    {
                        s.Skip(8);
                        arctap.Add(s.ReadInt(")"));
                        if (s.Current != ",") break;
                    }
                }
                Events.Add(new ArcaeaAffArc()
                {
                    Timing = tick,
                    EndTiming = endtick,
                    XStart = startx,
                    XEnd = endx,
                    LineType = linetype,
                    YStart = starty,
                    YEnd = endy,
                    Color = color,
                    IsVoid = isvoid,
                    Type = EventType.Arc,
                    ArcTaps = arctap,
                    TimingGroup = CurrentTimingGroup
                });
                if (endtick < tick) throw new ArcaeaAffFormatException("");
            }
            catch (ArcaeaAffFormatException Ex)
            {
                throw Ex;
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException("");
            }
        }

        private void ParseCamera(string line)
        {
            try
            {
                AffStringParser s = new AffStringParser(line);
                s.Skip(7);
                int tick = s.ReadInt(",");
                Vector3 move = new Vector3(s.ReadFloat(","), s.ReadFloat(","), s.ReadFloat(","));
                Vector3 rotate = new Vector3(s.ReadFloat(","), s.ReadFloat(","), s.ReadFloat(","));
                string type = s.ReadString(",");
                int duration = s.ReadInt(")");
                Events.Add(new ArcaeaAffCamera()
                {
                    Timing = tick,
                    Duration = duration,
                    Move = move,
                    Rotate = rotate,
                    CameraType = type,
                    Type = EventType.Camera
                });
                if (duration < 0) throw new ArcaeaAffFormatException("");
            }
            catch (ArcaeaAffFormatException Ex)
            {
                throw Ex;
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException("");
            }
        }

        private void ParseSpecial(string line)
        {
            try
            {
                AffStringParser s = new AffStringParser(line);
                s.Skip(8);
                int tick = s.ReadInt(",");
                string type = s.ReadString(",");
                SpecialType specialType = SpecialType.Unknown;
                string param1 = null, param2 = null, param3 = null;
                switch (type)
                {
                    case "text":
                        specialType = SpecialType.TextArea;
                        param1 = s.Peek(2);
                        if (param1 == "in")
                        {
                            param1 = s.ReadString(",");
                            param2 = s.ReadString(")");
                            param2 = param2.Replace("<br>", "\n");
                        }
                        else
                        {
                            param1 = s.ReadString(")");
                        }
                        break;
                    case "fade":
                        specialType = SpecialType.Fade;
                        param1 = s.ReadString(")");
                        break;
                }
                Events.Add(new ArcadeAffSpecial()
                {
                    Timing = tick,
                    Type = EventType.Special,
                    param1 = param1,
                    param2 = param2,
                    param3 = param3,
                    SpecialType = specialType
                });
            }
            catch (ArcaeaAffFormatException Ex)
            {
                throw Ex;
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException("");
            }
        }

        public void ParseFile(string path)
        {
            TotalTimingGroup = 0;
            CurrentTimingGroup = 0;
            string[] lines = File.ReadAllLines(path);
            try
            {
                AudioOffset = int.Parse(lines[0].Replace("AudioOffset:", ""));
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException(lines[0], 1);
            }
            try
            {
                ParseTiming(lines[2]);
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException(EventType.Timing, lines[2], 3);
            }
            for (int i = 3; i < lines.Length; ++i)
            {
                string line = lines[i].Trim();
                EventType type = DetermineType(line);
                try
                {
                    switch (type)
                    {
                        case EventType.Timing:
                            ParseTiming(line);
                            break;
                        case EventType.Tap:
                            ParseTap(line);
                            break;
                        case EventType.Hold:
                            ParseHold(line);
                            break;
                        case EventType.Arc:
                            ParseArc(line);
                            break;
                        case EventType.Camera:
                            ParseCamera(line);
                            break;
                        case EventType.Special:
                            ParseSpecial(line);
                            break;
                        case EventType.TimingGroup:
                            TotalTimingGroup++;
                            CurrentTimingGroup = TotalTimingGroup;
                            break;
                        case EventType.TimingGroupEnd:
                            CurrentTimingGroup = 0;
                            break;
                    }
                }
                catch (ArcaeaAffFormatException Ex)
                {
                    throw new ArcaeaAffFormatException(type, line, i + 1, Ex.Reason);
                }
                catch (Exception)
                {
                    throw new ArcaeaAffFormatException(type, line, i + 1);
                }
            }
            Events.Sort((ArcaeaAffEvent a, ArcaeaAffEvent b) => { return a.Timing.CompareTo(b.Timing); });
            if (CurrentTimingGroup != 0)
            {
                throw new ArcaeaAffFormatException("");
            }
        }
    }
}
