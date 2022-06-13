using System.Numerics;

namespace AffTools.AffReader;

public class ArcaeaAffReader
{
    public int TotalTimingGroup = 1;

    public int CurrentTimingGroup;

    public int AudioOffset;
    public float TimingPointDensityFactor = 1;

    public List<ArcaeaAffEvent> Events = new List<ArcaeaAffEvent>();

    public List<TimingGroupProperties> TimingGroupProperties = new List<TimingGroupProperties>();

    private static EventType DetermineType(string line)
    {
        if (line.StartsWith("("))
            return EventType.Tap;
        if (line.StartsWith("timing("))
            return EventType.Timing;
        if (line.StartsWith("hold("))
            return EventType.Hold;
        if (line.StartsWith("arc("))
            return EventType.Arc;
        if (line.StartsWith("camera("))
            return EventType.Camera;
        if (line.StartsWith("scenecontrol("))
            return EventType.SceneControl;
        if (line.StartsWith("timinggroup("))
            return EventType.TimingGroup;
        if (line.StartsWith("};"))
            return EventType.TimingGroupEnd;
        return EventType.Unknown;
    }

    private void ParseTiming(string line)
    {
        try
        {
            var stringParser = new AffStringParser(line);
            stringParser.Skip(7);
            var num = stringParser.ReadInt(",");
            var num2 = stringParser.ReadFloat(",");
            var num3 = stringParser.ReadFloat(")");
            Events.Add(new ArcaeaAffTiming
            {
                Timing = num,
                BeatsPerLine = num3,
                Bpm = num2,
                Type = EventType.Timing,
                TimingGroup = CurrentTimingGroup
            });
            if (num3 < 0f)
                throw new ArcaeaAffFormatException("节拍线密度小于0");
            if (num == 0 && num2 == 0f)
                throw new ArcaeaAffFormatException("基准BPM为0（Timing为0的timing事件BPM不能为0）");
        }
        catch (ArcaeaAffFormatException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArcaeaAffFormatException("符号错误");
        }
    }

    private void ParseTap(string line, bool noInput)
    {
        try
        {
            var stringParser = new AffStringParser(line);
            stringParser.Skip(1);
            var timing = stringParser.ReadInt(",");
            var num = stringParser.ReadInt(")");
            if (!noInput) Events.Add(new ArcaeaAffTap
            {
                Timing = timing,
                Track = num,
                Type = EventType.Tap,
                TimingGroup = CurrentTimingGroup,
                NoInput = noInput
            });
            if (num is <= 0 or >= 5)
                throw new ArcaeaAffFormatException("轨道错误");
        }
        catch (ArcaeaAffFormatException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArcaeaAffFormatException("符号错误");
        }
    }

    private void ParseHold(string line, bool noInput)
    {
        try
        {
            var stringParser = new AffStringParser(line);
            stringParser.Skip(5);
            var num = stringParser.ReadInt(",");
            var num2 = stringParser.ReadInt(",");
            var num3 = stringParser.ReadInt(")");
            if (!noInput) Events.Add(new ArcaeaAffHold
            {
                Timing = num,
                EndTiming = num2,
                Track = num3,
                Type = EventType.Hold,
                TimingGroup = CurrentTimingGroup,
                NoInput = noInput
            });
            if (num3 is <= 0 or >= 5)
                throw new ArcaeaAffFormatException("轨道错误");
            if (num2 < num)
                throw new ArcaeaAffFormatException("持续时间小于0");
        }
        catch (ArcaeaAffFormatException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArcaeaAffFormatException("符号错误");
        }
    }

    private void ParseArc(string line, bool noInput)
    {
        try
        {
            var stringParser = new AffStringParser(line);
            stringParser.Skip(4);
            var num = stringParser.ReadInt(",");
            var num2 = stringParser.ReadInt(",");
            var xStart = stringParser.ReadFloat(",");
            var xEnd = stringParser.ReadFloat(",");
            var lineType = stringParser.ReadString(",");
            var yStart = stringParser.ReadFloat(",");
            var yEnd = stringParser.ReadFloat(",");
            var color = stringParser.ReadInt(",");
            stringParser.ReadString(",");
            var isVoid = stringParser.ReadBool(")");
            List<int>? list = null;
            if (stringParser.Current != ";")
            {
                list = new List<int>();
                isVoid = true;
                do
                {
                    stringParser.Skip(8);
                    list.Add(stringParser.ReadInt(")"));
                }
                while (!(stringParser.Current != ","));
            }
            if (!noInput) Events.Add(new ArcaeaAffArc
            {
                Timing = num,
                EndTiming = num2,
                XStart = xStart,
                XEnd = xEnd,
                LineType = lineType,
                YStart = yStart,
                YEnd = yEnd,
                Color = color,
                IsVoid = isVoid,
                Type = EventType.Arc,
                ArcTaps = list,
                TimingGroup = CurrentTimingGroup,
                NoInput = noInput
            });
            if (num2 < num)
                throw new ArcaeaAffFormatException("持续时间小于0");
        }
        catch (ArcaeaAffFormatException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArcaeaAffFormatException("符号错误");
        }
    }

    private void ParseCamera(string line)
    {
        try
        {
            var stringParser = new AffStringParser(line);
            stringParser.Skip(7);
            var timing = stringParser.ReadInt(",");
            var move = new Vector3(stringParser.ReadFloat(","), stringParser.ReadFloat(","), stringParser.ReadFloat(","));
            var rotate = new Vector3(stringParser.ReadFloat(","), stringParser.ReadFloat(","), stringParser.ReadFloat(","));
            var cameraType = stringParser.ReadString(",");
            var num = stringParser.ReadInt(")");
            Events.Add(new ArcaeaAffCamera
            {
                Timing = timing,
                Duration = num,
                Move = move,
                Rotate = rotate,
                CameraType = cameraType,
                Type = EventType.Camera
            });
            if (num < 0)
                throw new ArcaeaAffFormatException("持续时间小于0");
        }
        catch (ArcaeaAffFormatException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArcaeaAffFormatException("符号错误");
        }
    }

    private void ParseSceneControl(string line)
    {
        try
        {
            try
            {
                var stringParser = new AffStringParser(line);
                stringParser.Skip(13);
                var timing = stringParser.ReadInt(",");
                var sceneControlTypeName = stringParser.ReadString(",").Trim();
                var text = stringParser.ReadString();
                text = text.Substring(0, text.LastIndexOf(')'));
                var array = text.Split(',');
                var list = new List<object>();
                var text2 = "";
                var array2 = array;
                foreach (var text3 in array2)
                {
                    var num = text3[0] == '\'';
                    var flag = text2 != "";
                    var flag2 = text3[^1] == '\'';
                    if (num || flag || flag2)
                        text2 += text3;
                    if (flag2)
                    {
                        list.Add(text2.Substring(1, text2.Length - 2));
                        text2 = "";
                    }
                    if (!num && !flag && !flag2)
                        list.Add(float.Parse(text3));
                }
                Events.Add(new ArcaeaAffSceneControl
                {
                    Timing = timing,
                    Type = EventType.SceneControl,
                    Parameters = list,
                    SceneControlTypeName = sceneControlTypeName,
                    TimingGroup = CurrentTimingGroup
                });
            }
            catch (Exception)
            {
                var stringParser2 = new AffStringParser(line);
                stringParser2.Skip(13);
                var timing2 = stringParser2.ReadInt(",");
                var sceneControlTypeName2 = stringParser2.ReadString(")");
                var parameters = new List<object>();
                Events.Add(new ArcaeaAffSceneControl
                {
                    Timing = timing2,
                    Type = EventType.SceneControl,
                    Parameters = parameters,
                    SceneControlTypeName = sceneControlTypeName2,
                    TimingGroup = CurrentTimingGroup
                });
            }
        }
        catch (ArcaeaAffFormatException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArcaeaAffFormatException("符号错误");
        }
    }

    private static bool NoInputGroup(string line)
    {
        try
        {
            var stringParser = new AffStringParser(line);
            stringParser.Skip(12);
            return stringParser.ReadString(")") == "noinput";
        }
        catch (ArcaeaAffFormatException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Parse(string path)
    {
        TotalTimingGroup = 1;
        CurrentTimingGroup = 0;
        TimingGroupProperties.Add(new TimingGroupProperties());
        var noInput = false;
        var array = File.ReadAllLines(path);
        try
        {
            AudioOffset = int.Parse(array[0].Replace("AudioOffset:", ""));
        }
        catch (Exception)
        {
            throw new ArcaeaAffFormatException(array[0], 1);
        }
        int i;
        if (array[1].Contains("TimingPointDensityFactor"))
        {
            try
            {
                TimingPointDensityFactor = float.Parse(array[1].Replace("TimingPointDensityFactor:", ""));
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException(array[1], 2);
            }
            try
            {
                ParseTiming(array[3]);
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException(EventType.Timing, array[3], 4);
            }
            i = 4;
        }
        else
        {
            try
            {
                ParseTiming(array[2]);
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException(EventType.Timing, array[2], 3);
            }
            i = 3;
        }

        for (; i < array.Length; i++)
        {
            var text = array[i].Trim();
            var eventType = DetermineType(text);
            try
            {
                switch (eventType)
                {
                    case EventType.Timing:
                        ParseTiming(text);
                        break;
                    case EventType.Tap:
                        ParseTap(text, noInput);
                        break;
                    case EventType.Hold:
                        ParseHold(text, noInput);
                        break;
                    case EventType.Arc:
                        ParseArc(text, noInput);
                        break;
                    case EventType.Camera:
                        ParseCamera(text);
                        break;
                    case EventType.SceneControl:
                        ParseSceneControl(text);
                        break;
                    case EventType.TimingGroup:
                        TotalTimingGroup++;
                        CurrentTimingGroup = TotalTimingGroup - 1;
                        noInput = NoInputGroup(text);
                        while (TimingGroupProperties.Count <= CurrentTimingGroup)
                        {
                            TimingGroupProperties.Add(new TimingGroupProperties());
                        }
                        TimingGroupProperties[CurrentTimingGroup] = new TimingGroupProperties
                        {
                            NoInput = noInput,
                        };
                        break;
                    case EventType.TimingGroupEnd:
                        CurrentTimingGroup = 0;
                        noInput = false;
                        break;
                    case EventType.Unknown:
                        break;
                }
            }
            catch (ArcaeaAffFormatException ex3)
            {
                throw new ArcaeaAffFormatException(eventType, text, i + 1, ex3.Reason);
            }
            catch (Exception)
            {
                throw new ArcaeaAffFormatException(eventType, text, i + 1);
            }
        }
        Events.Sort((a, b) => a.Timing.CompareTo(b.Timing));
        if (CurrentTimingGroup == 0) return;
        throw new ArcaeaAffFormatException("Timing Group { 与 } 数量不匹配，请确保\ntiminggroup(){\n与\n};\n各占一行");
    }
}
