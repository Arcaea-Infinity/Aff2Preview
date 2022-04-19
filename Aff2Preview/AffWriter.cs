
using AimuBotCS.Modules.Arcaea.Aff2Preview.Advanced;

namespace AimuBotCS.Modules.Arcaea.Aff2Preview
{
    public class AffWriter : StreamWriter
    {
        public AffWriter(Stream stream, int audioOffset) : base(stream)
        {
            WriteLine($"AudioOffset:{audioOffset}");
            WriteLine("-");
        }
        public void WriteEvent(ArcaeaAffEvent affEvent)
        {
            switch (affEvent.Type)
            {
                case EventType.Timing:
                    ArcaeaAffTiming timing = affEvent as ArcaeaAffTiming;
                    WriteLine($"timing({timing.Timing},{timing.Bpm:f2},{timing.BeatsPerLine:f2});");
                    break;
                case EventType.Tap:
                    ArcaeaAffTap tap = affEvent as ArcaeaAffTap;
                    WriteLine($"({tap.Timing},{tap.Track});");
                    break;
                case EventType.Hold:
                    ArcaeaAffHold hold = affEvent as ArcaeaAffHold;
                    WriteLine($"hold({hold.Timing},{hold.EndTiming},{hold.Track});");
                    break;
                case EventType.Arc:
                    ArcaeaAffArc arc = affEvent as ArcaeaAffArc;
                    string arcStr = $"arc({arc.Timing},{arc.EndTiming},{arc.XStart:f2},{arc.XEnd:f2}";
                    arcStr += $",{arc.LineType},{arc.YStart:f2},{arc.YEnd:f2},{arc.Color},none,{((arc.ArcTaps == null || arc.ArcTaps.Count == 0) ? arc.IsVoid.ToString().ToLower() : "true")})";
                    if (arc.ArcTaps != null && arc.ArcTaps.Count != 0)
                    {
                        arcStr += "[";
                        for (int i = 0; i < arc.ArcTaps.Count; ++i)
                        {
                            arcStr += $"arctap({arc.ArcTaps[i]})";
                            if (i != arc.ArcTaps.Count - 1) arcStr += ",";
                        }
                        arcStr += "]";
                    }
                    arcStr += ";";
                    WriteLine(arcStr);
                    break;
                case EventType.Camera:
                    ArcaeaAffCamera cam = affEvent as ArcaeaAffCamera;
                    WriteLine($"camera({cam.Timing},{cam.Move.X:f2},{cam.Move.Y:f2},{cam.Move.Z:f2},{cam.Rotate.X:f2},{cam.Rotate.Y:f2},{cam.Rotate.Z:f2},{cam.CameraType},{cam.Duration});");
                    break;
                case EventType.Special:
                    ArcadeAffSpecial spe = affEvent as ArcadeAffSpecial;
                    string type = "error";
                    switch (spe.SpecialType)
                    {
                        case SpecialType.Fade:
                            type = "fade";
                            WriteLine($"special({spe.Timing},{type},{spe.param1 ?? "null"});");
                            break;
                        case SpecialType.TextArea:
                            type = "text";
                            if (spe.param1 == "in")
                                WriteLine($"special({spe.Timing},{type},{spe.param1 ?? "null"},{(spe.param2 == null ? "null" : spe.param2.Replace("\n", "<br>"))});");
                            else
                                WriteLine($"special({spe.Timing},{type},{spe.param1 ?? "null"});");
                            break;
                    }
                    break;
            }
        }
        public void WriteTimingGroupStart()
        {
            WriteLine("timinggroup(){");
        }
        public void WriteTimingGroupEnd()
        {
            WriteLine("};");
        }
    }
}
