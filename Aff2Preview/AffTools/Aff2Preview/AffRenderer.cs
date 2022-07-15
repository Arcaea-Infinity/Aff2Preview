using System.Numerics;
using System.Text.RegularExpressions;

using AffTools.AffReader;
using AffTools.MyGraphics;

namespace AffTools.Aff2Preview;

internal class AffRenderer
{
    private abstract class DrawObjectBase
    {
        public Vector3 Location { get; init; }
        public string? Property { get; init; }
        public bool IsEnwiden { get; init; }
        public abstract void Draw(GraphicsAdapter g);
    }

    private class ArcSegment : DrawObjectBase
    {
        public Vector3 End { get; init; }
        public int ColorId { get; init; }
        public bool IsVoid { get; init; }
        public override void Draw(GraphicsAdapter g)
        {
            float drawX = IsEnwiden ? Location.X * 4 / 6 + 0.25f * Config.DrawingTrackWidth * 4 / 6 : Location.X;
            float endX = IsEnwiden ? End.X * 4 / 6 + 0.25f * Config.DrawingTrackWidth * 4 / 6 : End.X;
            ColorDesc color = new();
            float w;
            if (IsVoid)
            {
                w = Config.ArcVoidWidth;
                color.SetColor(Config.GetArcVoidColor());
            }
            else
            {
                w = Config.ArcWidth;
                color.SetColor(Config.GetArcColor(ColorId));
            }
            color.SetColorA((byte)Math.Clamp((int)(47 + (230 - 47) * Location.Y), 0, 255));
            g.SetColor(color);
            g.DrawLine(w, drawX, Config.TimingToY(Location.Z), endX, Config.TimingToY(End.Z));
        }
    }

    private class ArcTap : DrawObjectBase
    {
        public override void Draw(GraphicsAdapter g)
        {
            var airTap = Property switch
            {
                "voice_wav" => Config.Side == 1 ? Config.SfxDTap : Config.SfxLTap,
                "glass_wav" => Config.Side == 1 ? Config.SfxDTap : Config.SfxLTap,
                _ => Config.AirTap
            };
            int drawX = IsEnwiden ? (int)(Location.X * 4 / 6 + Config.EnwidenTrackWidth / 2 + 3) :
                (int)(Location.X - Config.SingleTrackWidth / 2 + 1);

            g.DrawImageScaled(
                airTap,
                drawX,
                (int)(Config.TimingToY(Location.Z) - Config.SkyNoteHeight / 4) - (IsEnwiden ? 1 : 0),
                IsEnwiden ? Config.EnwidenTrackWidth - 2 : Config.SingleTrackWidth - 2,
                Config.SkyNoteHeight / 4,
                Math.Clamp(0.3f + Location.Y * 0.7f, 0, 1)
                );
        }
    }

    private class ConnectLine : DrawObjectBase
    {
        public Vector3 End { get; init; }
        public override void Draw(GraphicsAdapter g)
        {
            g.DrawLine(
                ColorDesc.FromArgb(Config.GetConnectLineColor()),
                2f,
                Location.X, Config.TimingToY(Location.Z),
                End.X, Config.TimingToY(Location.Z));
        }
    }

    private class TextObject : DrawObjectBase
    {
        public string Text { get; set; } = "";
        public FontDesc Font { get; set; }
        public ColorDesc Color { get; set; }
        public override void Draw(GraphicsAdapter g)
        {
            g.DrawString(Text, Color, Font, Location.X, Config.TimingToY(Location.Z));
        }
    }

    internal class ChartConfig
    {
        public ImageDesc Background = new GdiImage();
        public ImageDesc Cover = new GdiImage();

        public ImageDesc Tap = new GdiImage();
        public ImageDesc Hold = new GdiImage();
        public ImageDesc AirTap = new GdiImage();

        public readonly ImageDesc SfxDTap = new GdiImage();
        public readonly ImageDesc SfxLTap = new GdiImage();

        public int NoteHeight { get; set; } = 64;
        public int SkyNoteHeight { get; set; } = 61;
        public float ArcWidth { get; set; } = 20f;
        public float ArcVoidWidth { get; set; } = 3f;

        public int TotalTrackWidth { get; set; } = 248;
        public int DrawingTrackWidth => TotalTrackWidth - 4;
        public int SingleTrackWidth => DrawingTrackWidth / 4;
        public int EnwidenTrackWidth => DrawingTrackWidth / 6;

        public int TimingScale { get; set; } = 5;

        public int Cols { get; set; } = 1;
        public int ColWidth { get; set; } = 0;
        public int Rows { get; set; } = 1;
        public float SegmentLengthInBaseBpm { get; set; } = 0;

        /// <summary>
        /// 0:hikari 1:conflict 2:finale
        /// </summary>
        public int Side { get; set; } = 0;

        public uint TrackColor
            => Side switch
            {
                0 => 0xffffffff,
                1 => 0xff382a47,
                2 => 0xffffffff,
                _ => 0
            };

        public uint TrackLineColor
            => Side switch
            {
                0 => 0xffd3d3d3,
                1 => 0xff2e1f3c,
                2 => 0xffd3d3d3,
                _ => 0
            };

        public uint TrackSegmentLineColor
            => Side switch
            {
                0 => 0xa0d3d3d3,
                1 => 0xc02e1f3c,
                2 => 0xa0d3d3d3,
                _ => 0
            };

        public uint TrackStripColor
            => Side switch
            {
                0 => 0x0f808080,
                1 => 0x08f0f8ff,
                2 => 0x0f808080,
                _ => 0
            };

        public uint GetArcVoidColor()
            => Side switch
            {
                0 => 0xffd3d3d3,
                1 => 0xffa9a9a9,
                2 => 0xffd3d3d3,
                _ => 0,
            };

        public uint GetArcColor(int type)
            => Side switch
            {
                0 => type switch
                {
                    0 => 0xff31dae7,
                    1 => 0xffff69b4,
                    _ => 0,
                },
                1 => type switch
                {
                    0 => 0xff00ced1,
                    1 => 0xffff1493,
                    _ => 0,
                },
                2 => type switch
                {
                    0 => 0xff31dae7,
                    1 => 0xffff69b4,
                    _ => 0,
                },
                _ => 0xff31dae7,
            };

        public uint GetConnectLineColor()
            => Side switch
            {
                0 => 0xdc90ee90,
                1 => 0xdcff1493,
                2 => 0xdc90ee90,
                _ => 0,
            };

        public int TotalTrackLength { get; set; } = 0;

        public float TimingToY(float timing)
            => (TotalTrackLength - timing) / TimingScale - 3;
    }

    private readonly ArcaeaAffReader _affReader = new();

    public static ChartConfig Config = new();

    private AffAnalyzer.Analyzer _affAnalyzer;

    public AffRenderer(string affFile)
    {
        AffFile = affFile;
    }

    public string AffFile { get; set; } = "";
    public string Title { get; set; } = "";
    public float Rating { get; set; } = 0f;
    public int Notes { get; set; } = 0;
    public int Side { get; set; } = 0;
    public float ChartBpm { get; set; } = 0f;
    public string Artist { get; set; } = "";
    public string Charter { get; set; } = "";
    public int Difficulty { get; set; } = 0;
    public bool IsMirror { get; set; } = false;
    private List<(int, int)> Interval4K { get; set; } = new();
    public string DiffStr => Difficulty switch
    {
        0 => "Past",
        1 => "Present",
        2 => "Future",
        3 => "Beyond",
        _ => ""
    };

    public void LoadResource(string tap, string hold, string airTap, string bg, string cover)
    {
        Config.Background.FromFile(bg);

        Config.Cover.FromFile(cover);

        Config.Tap.FromFile(tap);
        Config.Hold.FromFile(hold);
        Config.AirTap.FromFile(airTap);
    }

    private void MirrorAff()
    {
        foreach (var affEvent in _affReader.Events)
        {
            switch (affEvent)
            {
                case ArcaeaAffTap tap:
                    tap.Track = 5 - tap.Track;
                    break;
                case ArcaeaAffHold hold:
                    hold.Track = 5 - hold.Track;
                    break;
                case ArcaeaAffArc arc:
                    arc.XStart = 1f - arc.XStart;
                    arc.XEnd = 1f - arc.XEnd;
                    arc.Color = 1 - arc.Color;
                    break;
            }
        }
    }

    private void LoadAff()
    {
        _affReader.Parse(AffFile);

        _affAnalyzer = new(_affReader);
        _affAnalyzer.AnalyzeSegments();
        _affAnalyzer.AnalyzeNotes();

        Config.TotalTrackLength = (int)_affAnalyzer.totalTime;
    }

    public ImageDesc Draw()
    {
        Config.Side = Side;

        LoadAff();
        if (IsMirror) MirrorAff();

        _affAnalyzer.CalcNotes();
        Interval4K = _affAnalyzer.Get4LaneInterval(Config.TotalTrackLength);

        var trackImg = DrawTrackObjects();

        float segmentLengthInBaseBpm = _affAnalyzer.baseTimePerSegment / Config.TimingScale;
        int rows = _affAnalyzer.segmentCountInBaseBpm;
        int cols = 1;
        int colWidth = Config.TotalTrackWidth + 75;

        for (; rows > 0; rows--)
        {
            cols = _affAnalyzer.segmentCountInBaseBpm / rows + (_affAnalyzer.segmentCountInBaseBpm % rows == 0 ? 0 : 1);
            double w = cols * colWidth;
            double h = rows * segmentLengthInBaseBpm;
            if (w / h >= 4f / 3f)
                break;
        }

        Config.Cols = cols;
        Config.Rows = rows;
        Config.ColWidth = colWidth;
        Config.SegmentLengthInBaseBpm = segmentLengthInBaseBpm;

        int outputWidth = Config.Cols * colWidth + 100;
        int outputHeight = Config.Rows * (int)segmentLengthInBaseBpm + 200 + 25 + 25;

        ColorDesc? rectColor = ColorDesc.FromArgb(Config.Side switch
        {
            0 => 0xc8f0f0f0,
            1 => 0xc8202020,
            2 => 0xc8f0f0f0,
        });

        GraphicsAdapter g = new GdiPlusAdapter();
        g.BeginContext(outputWidth, outputHeight);
        g.Fill(rectColor);

        int bw = Config.Background.GetWidth();
        int bh = Config.Background.GetHeight();
        if ((float)outputWidth / bw > (float)outputHeight / bh)
        {
            int dw = outputWidth;
            float rate = (float)dw / bw;
            float dh = bh * rate;
            g.DrawImageScaled(Config.Background, 0, -Math.Abs(outputHeight - dh) / 2, dw, dh);
        }
        else
        {
            int dh = outputHeight;
            float rate = (float)dh / bh;
            float dw = bw * rate;
            g.DrawImageScaled(Config.Background, -Math.Abs(outputWidth - dw) / 2, 0, dw, dh);
        }

        g.FillRectangle(rectColor,
            25, 25, Config.Cols * Config.ColWidth + 50, Config.Rows * (int)Config.SegmentLengthInBaseBpm + 50 + 25);

        for (int x = 0; x < Config.Cols; x++)
        {
            double y = trackImg.GetHeight() - (x + 1) * Config.Rows * Config.SegmentLengthInBaseBpm;
            g.DrawImageCliped(trackImg, x * colWidth + colWidth - Config.TotalTrackWidth + 50, 50,
                0, (int)y, Config.TotalTrackWidth, (int)(Config.Rows * Config.SegmentLengthInBaseBpm));
        }

        DrawComboNumber(g);
        //DrawSegmentNumber(g);
        DrawSegmentBpm(g);
        DrawNoteLength(g);

        DrawFooter(g);

        return g.EndContext();
    }

    public void DrawTrack(GraphicsAdapter g)
    {
        foreach (var (initial, end) in Interval4K)
        {
            for (int i = 0; i < 5; i++)
            {
                g.DrawLine(ColorDesc.FromArgb(Config.TrackLineColor), i is > 0 and < 4 ? 2f : 4f,
                    Config.DrawingTrackWidth / 4 * i + 2, (Config.TotalTrackLength - initial) / Config.TimingScale,
                    Config.DrawingTrackWidth / 4 * i + 2, (Config.TotalTrackLength - end) / Config.TimingScale);
            }
        }

        var widen = _affAnalyzer.GetPairEnwidenLanes();
        foreach (var (initial, end) in widen)
        {
            for (int i = 0; i < 7; i++)
            {
                g.DrawLine(ColorDesc.FromArgb(Config.TrackLineColor), i is > 0 and < 6 ? 2f : 4f,
                    Config.DrawingTrackWidth * i / 6 + 2, (Config.TotalTrackLength - initial.Timing) / Config.TimingScale,
                    Config.DrawingTrackWidth * i / 6 + 2, (Config.TotalTrackLength - end.Timing) / Config.TimingScale);
            }
        }

        g.SetColor(ColorDesc.FromArgb(Config.TrackSegmentLineColor));
        foreach (float t in _affAnalyzer.SegmentTimings)
        {
            if (t >= 0)
                g.DrawLine(3f,
                    0, Config.TimingToY(t),
                    Config.TotalTrackWidth, Config.TimingToY(t));
        }

        g.SetColor(ColorDesc.FromArgb(Config.TrackStripColor));
        for (int i = 0; i < Config.TotalTrackLength; i += 45)
        {
            g.DrawLine(57f, -20, i, 300, i - 200);
        }

        foreach (var note in _affAnalyzer.Notes)
        {
            if (note.TimePoint > Config.TotalTrackLength)
                break;

            float density = note.InvDuration - 6;

            if (density < 0) continue;

            density = density * density * 0.5f;

            g.SetColor(ColorDesc.FromArgb((byte)density, 255, 0, 0));
            g.FillRectangle(ColorDesc.FromArgb((byte)density, 255, 0, 0),
                0, Config.TimingToY(note.TimePoint + note.Duration) - 1,
                Config.TotalTrackWidth, (note.Duration + 3) / Config.TimingScale);
        }
    }

    public ImageDesc DrawTrackObjects()
    {
        GraphicsAdapter g = new GdiPlusAdapter();
        g.BeginContext(Config.TotalTrackWidth, Config.TotalTrackLength / Config.TimingScale);

        g.Fill(ColorDesc.FromArgb(Config.TrackColor));

        DrawTrack(g);
        DrawFloorNotes(g);
        DrawAirObjects(g);

        return g.EndContext();
    }

    void DrawFloorNotes(GraphicsAdapter g)
    {
        foreach (var ev in _affReader.Events)
        {
            if (ev is ArcaeaAffTap tap)
            {
                if (Interval4K.Any(x => tap.Timing > x.Item1 && tap.Timing < x.Item2))
                {
                    float x = Config.DrawingTrackWidth * (tap.Track - 1) / 4;
                    float y = Config.TimingToY(tap.Timing);
                    g.DrawImageScaled(Config.Tap,
                        x + 3, y - Config.NoteHeight / 4,
                        Config.SingleTrackWidth - 2, Config.NoteHeight / 4, 0);
                }
                else
                {
                    float x = Config.DrawingTrackWidth * (tap.Track) / 6;
                    float y = Config.TimingToY(tap.Timing);
                    g.DrawImageScaled(Config.Tap,
                        x + 3, y - Config.NoteHeight / 4,
                        Config.EnwidenTrackWidth - 2, Config.NoteHeight / 4, 0);
                }
            }
            else if (ev is ArcaeaAffHold hold)
            {
                if (Interval4K.Any(x => hold.Timing > x.Item1 && hold.Timing < x.Item2))
                {
                    float x = Config.DrawingTrackWidth * (hold.Track - 1) / 4;
                    float ys = Config.TimingToY(hold.Timing);
                    float ye = Config.TimingToY(hold.EndTiming);
                    g.DrawImageScaled(Config.Hold, x + 3, ye, Config.SingleTrackWidth - 2, ys - ye, 0);
                }
                else
                {
                    float x = Config.DrawingTrackWidth * (hold.Track) / 6;
                    float ys = Config.TimingToY(hold.Timing);
                    float ye = Config.TimingToY(hold.EndTiming);
                    g.DrawImageScaled(Config.Hold, x + 3, ye, Config.EnwidenTrackWidth - 2, ys - ye, 0);
                }
            }
        }
    }

    public void DrawAirObjects(GraphicsAdapter g)
    {
        List<DrawObjectBase> airObjects = new();
        List<DrawObjectBase> airTaps = new();

        var AddDoubleTip = (float x, float z, bool isEnwiden) =>
        {
            int sw = isEnwiden ? Config.EnwidenTrackWidth : Config.SingleTrackWidth;
            float sx = x <= Config.DrawingTrackWidth / 2 ? x + sw / 2 + 5 : x - sw / 2 - 20;
            airObjects.Add(new TextObject()
            {
                Text = "x2",
                Location = new Vector3(sx, 1.5f, z + 80),
                Color = Config.Side == 1 ? ColorDesc.FromArgb(0xddffffff) : ColorDesc.FromArgb(0xff000000),
                Font = new FontDesc("exo", 10f, FontDescStyle.Bold)
            });
        };

        foreach (var ev in _affReader.Events)
        {
            if (ev is not ArcaeaAffArc t) continue;

            int duration = t.EndTiming - t.Timing;
            bool isEnwiden = !Interval4K.Any(inv => t.Timing > inv.Item1 && t.Timing < inv.Item2);

            int segSize = duration / (duration < 1000 ? 14 : 7);
            int segmentCount = (segSize == 0 ? 0 : duration / segSize) + 1;

            List<Vector3> segments = new();

            Vector3 start = new();
            Vector3 end = new((t.XStart + 0.5f) * Config.DrawingTrackWidth / 2 + 3, t.YStart, t.Timing);
            segments.Add(end);

            for (int i = 0; i < segmentCount - 1; i++)
            {
                start = end;
                float x = ArcAlgorithm.X(t.XStart, t.XEnd, (i + 1f) * segSize / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                float y = ArcAlgorithm.Y(t.YStart, t.YEnd, (i + 1f) * segSize / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                end = new Vector3((x + 0.5f) * Config.DrawingTrackWidth / 2 + 3,
                    y,
                    t.Timing + segSize * (i + 1));
                segments.Add(end);
            }

            // last segment
            {
                start = end;
                end = new Vector3((t.XEnd + 0.5f) * Config.DrawingTrackWidth / 2 + 3,
                    t.YEnd,
                    t.EndTiming);
                segments.Add(end);
            }

            for (int i = 0; i < segments.Count - 1; i++)
            {
                var st = segments[i];
                var ed = segments[i + 1];

                airObjects.Add(new ArcSegment()
                {
                    IsVoid = t.IsVoid,
                    ColorId = t.Color,
                    Location = st,
                    End = ed,
                    IsEnwiden = isEnwiden,
                });
            }

            if (t.ArcTaps is null)
                continue;

            foreach (int airTapTiming in t.ArcTaps)
            {
                float tm = airTapTiming - t.Timing;
                float x = ArcAlgorithm.X(t.XStart, t.XEnd, tm / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                float y = ArcAlgorithm.Y(t.YStart, t.YEnd, tm / duration, ArcaeaAffArc.ToArcLineType(t.LineType));

                bool isInEnwiden = !Interval4K.Any(inv => airTapTiming > inv.Item1 && airTapTiming < inv.Item2);

                x = (x + 0.5f) * Config.DrawingTrackWidth / 2;

                airTaps.Add(new ArcTap()
                {
                    Location = new Vector3(x + 2, y, airTapTiming),
                    Property = (ev as ArcaeaAffArc)?.Fx,
                    IsEnwiden = isInEnwiden,
                });

                if (isEnwiden)
                    x = x * 4 / 6 + Config.EnwidenTrackWidth + 3;

                // detect underneath notes
                foreach (var evOther in _affReader.Events)
                {
                    if (evOther is ArcaeaAffTap evAt)
                    {
                        if (Math.Abs(evAt.Timing - airTapTiming) > 3) continue;

                        float x_t = Config.DrawingTrackWidth * (evAt.Track - 1) / 4 + Config.SingleTrackWidth / 2;

                        if (!Interval4K.Any(inv => evAt.Timing > inv.Item1 && evAt.Timing < inv.Item2))
                            x_t = Config.DrawingTrackWidth * evAt.Track / 6 + Config.EnwidenTrackWidth / 2;

                        float y_t = Config.TimingToY(evAt.Timing);

                        airObjects.Add(new ConnectLine()
                        {
                            Location = new Vector3(x_t + 3, y, airTapTiming + 6),
                            End = new Vector3(x + 3, 0, airTapTiming + 6)
                        });

                        if (Math.Abs(x - x_t) <= 5)
                            AddDoubleTip(x, airTapTiming, isEnwiden);
                    }
                    else if (!t.Equals(evOther) && evOther is ArcaeaAffArc evArc)
                    {
                        if (evArc.ArcTaps is null)
                            continue;

                        foreach (int arcT in evArc.ArcTaps)
                        {
                            if (Math.Abs(arcT - airTapTiming) > 3) continue;

                            float arc_t_x = ArcAlgorithm.X(evArc.XStart, evArc.XEnd, evArc.Timing / duration, ArcaeaAffArc.ToArcLineType(evArc.LineType));
                            float arc_t_y = ArcAlgorithm.Y(evArc.YStart, evArc.YEnd, evArc.Timing / duration, ArcaeaAffArc.ToArcLineType(evArc.LineType));
                            arc_t_x = (arc_t_x + 0.5f) * Config.DrawingTrackWidth / 2;
                            if (!Interval4K.Any(inv => arcT > inv.Item1 && arcT < inv.Item2))
                                arc_t_x = arc_t_x * 4 / 6 + Config.EnwidenTrackWidth + 3;

                            if (Math.Abs(arc_t_x - x) <= 5 && arc_t_y < y)
                                AddDoubleTip(x, airTapTiming, isEnwiden);
                        }

                    }
                }

            }
        }

        airObjects.OrderBy(x => x.Location.Y).ToList().ForEach(x => x.Draw(g));
        airTaps.ForEach(x => x.Draw(g));
    }

    public void DrawSegmentNumber(GraphicsAdapter g)
    {
        ColorDesc c = ColorDesc.FromArgb(Config.Side == 1 ? 0xffffffff : 0xff000000);
        c.SetColorA(240);
        g.SetColor(c);
        g.SetFont("exo", 10f, FontDescStyle.Bold);

        for (int i = 0, t = 1; i < _affAnalyzer.SegmentTimings.Count; i++)
        {
            int segmentTiming = (int)_affAnalyzer.SegmentTimings[i];

            if (segmentTiming > _affAnalyzer.totalTime)
                break;

            if (segmentTiming < 0)
                continue;

            int next = i + 1;
            if (next < _affAnalyzer.SegmentTimings.Count)
            {
                int segmentTiming2 = (int)_affAnalyzer.SegmentTimings[next];
                if (segmentTiming2 - segmentTiming < 100)
                    continue;
            }

            float colHeight = Config.Rows * Config.SegmentLengthInBaseBpm;
            int sy = segmentTiming / Config.TimingScale;

            float y = colHeight - sy % colHeight;
            float x = Config.ColWidth * (sy / (int)colHeight);
            if (y <= 5)
                y = Config.Rows * Config.SegmentLengthInBaseBpm;

            g.DrawString(t.ToString(), x + Config.ColWidth - 220 - 25, y + 37);
            t++;
        }
    }

    public void DrawComboNumber(GraphicsAdapter g)
    {
        ColorDesc c = ColorDesc.FromArgb(Config.Side == 1 ? 0xffffffff : 0xff000000);
        c.SetColorA(240);
        g.SetColor(c);
        g.SetFont("exo", 10f, FontDescStyle.Bold);

        int prevCombo = -1;
        float endTime = _affAnalyzer.realTotalTime;
        int fullCombo = _affAnalyzer.Total;
        float colHeight = Config.Rows * Config.SegmentLengthInBaseBpm;

        for (int i = 0; i < _affAnalyzer.SegmentTimings.Count; i++)
        {
            int segmentTiming = (int)_affAnalyzer.SegmentTimings[i];

            if (segmentTiming > endTime - 10)
                break;

            if (segmentTiming < 0)
                continue;

            int next = i + 1;
            if (next < _affAnalyzer.SegmentTimings.Count)
            {
                int segmentTimingNext = (int)_affAnalyzer.SegmentTimings[next];
                if (segmentTimingNext - segmentTiming < 100)
                    continue;
            }

            int combo = _affAnalyzer.GetCombo(segmentTiming);
            if (combo == prevCombo)
                continue;

            prevCombo = combo;

            int sy = segmentTiming / Config.TimingScale;

            float y = colHeight - sy % colHeight;
            float x = Config.ColWidth * (sy / (int)colHeight);
            if (y <= 5)
                y = Config.Rows * Config.SegmentLengthInBaseBpm;

            g.DrawString(combo.ToString(), x + Config.ColWidth - 220 - 29, y + 37);
        }
        {
            int sy = (int)endTime / Config.TimingScale;

            float y = colHeight - sy % colHeight;
            float x = Config.ColWidth * (sy / (int)colHeight);
            if (y <= 5)
                y = Config.Rows * Config.SegmentLengthInBaseBpm;

            g.DrawString(fullCombo.ToString(), x + Config.ColWidth - 220 - 29, y + 37);
        }
    }

    public void DrawSegmentBpm(GraphicsAdapter g)
    {
        ColorDesc c = ColorDesc.FromArgb(0xffff7f50);

        g.SetColor(c);
        g.SetFont("exo", 10f, FontDescStyle.Bold);

        float lastBpl = 0;
        float lastBpm = 0;

        foreach (var ev in _affReader.Events)
        {
            if (ev is not ArcaeaAffTiming t) continue;

            if (t.Timing > Config.TotalTrackLength)
                break;

            if (t.Timing == 0 && t.TimingGroup != 0)
                continue;

            if (t.Bpm is 0 or > 1000)
                continue;

            float colHeight = Config.Rows * Config.SegmentLengthInBaseBpm;
            float rate = t.Bpm / _affAnalyzer.baseBpm;
            float y = colHeight - t.Timing / Config.TimingScale % colHeight;
            float x = Config.ColWidth * (t.Timing / Config.TimingScale / (int)colHeight);
            if (y <= 5)
                y = (int)colHeight;

            if (lastBpm != t.Bpm)
            {
                g.DrawStringLayoutLTRB($"{(int)t.Bpm}",
                    x + Config.ColWidth - 220 - 20 - 60, y + 37,
                    x + Config.ColWidth - 220 - 20 - 6, y + 60,
                    StringAdapterAlignment.Far);

                lastBpm = t.Bpm;
            }

            if (lastBpl == t.BeatsPerLine) continue;
            float bpl = t.BeatsPerLine;
            string? bplText = "";

            if ((int)(bpl * 100) % 100 == 0)
                bplText = $"{(int)bpl}/4";
            else if ((int)(bpl * 200) % 100 == 0)
            {
                bplText = $"{(int)(bpl * 2)}/8";
            }
            else if ((int)(bpl * 400) % 100 == 0)
            {
                bplText = $"{(int)(bpl * 2)}/16";
            }
            if (t.Bpm > 0)
                g.DrawStringLayoutLTRB($"{bplText}",
                    x + Config.ColWidth - 220 - 20 - 60, y + 52,
                    x + Config.ColWidth - 220 - 20 - 6, y + 80,
                    StringAdapterAlignment.Far);

            lastBpl = t.BeatsPerLine;
        }
    }

    public void DrawNoteLength(GraphicsAdapter g)
    {
        ColorDesc c = ColorDesc.FromArgb(0xff008b8b);

        g.SetColor(c);
        g.SetFont("exo", 10f, FontDescStyle.Bold);

        foreach (var note in _affAnalyzer.Notes)
        {
            if (note.TimePoint > Config.TotalTrackLength)
                break;

            float colHeight = Config.Rows * Config.SegmentLengthInBaseBpm;
            float y = colHeight - note.TimePoint / Config.TimingScale % colHeight;
            float x = Config.ColWidth * (note.TimePoint / Config.TimingScale / (int)colHeight);
            if (y <= 1)
                y = (int)colHeight;

            string? s =
                (note.Divide > 0 ? note.Divide.ToString() : "-") +
                (note.hasDot ? "." : "") +
                (note.beyondFull ? "-" : "");

            g.DrawString(s, x + Config.ColWidth - 218, y + 37);
        }
    }

    public void DrawFooter(GraphicsAdapter g)
    {
        int footerX = 25;
        int footerY = Config.Rows * (int)Config.SegmentLengthInBaseBpm + 200 - 100 + 25;
        int footerW = Config.Cols * Config.ColWidth + 50;
        int footerH = 100;

        ColorDesc? rectColor = ColorDesc.FromArgb(Config.Side switch
        {
            0 => 0xc8f0f0f0,
            1 => 0xc8202020,
            2 => 0xc8f0f0f0,
            _ => 0,
        });

        g.FillRectangle(rectColor, footerX, footerY, footerW, footerH);

        g.SetColor(ColorDesc.FromArgb(Config.Side == 1 ? 0xffffffff : 0xff000000));

        bool hasCover = Config.Cover.InnerImage is not null;
        if (hasCover)
            g.DrawImageScaled(Config.Cover, footerX, footerY, 100, 100);

        Regex enRegex = new(@"^[A-Za-z\d_\s/\(\)\+\=\-\.\[\]:\(\)&']+$");

        string? title = Title +
            $"   [ {DiffStr} {Rating:F1} ]" +
            $"    Tap{_affAnalyzer.Tap}   " +
            $"Hold{_affAnalyzer.Hold}   " +
            $"Arc{_affAnalyzer.Arc[0] + _affAnalyzer.Arc[1]}   " +
            $"[ Blue{_affAnalyzer.Arc[0]} Red{_affAnalyzer.Arc[1]} ]   " +
            $"ArcTap{_affAnalyzer.ArcTap}   " +
            $"Total{_affAnalyzer.Total}";

        string? secondLine = (ChartBpm > 0 ? $"Bpm {ChartBpm}     " : "") + $"{Artist} / {Charter}";

        g.SetColor(ColorDesc.FromArgb(Config.Side == 1 ? 0xffffffff : 0xff000000));
        //g.SetFont(enRegex.IsMatch(title) || enRegex.IsMatch(secondLine) ? "GeosansLight" : "Kazesawa Regular", 26f, FontDescStyle.Regular);
        g.SetFont("GeosansLight", 26f, FontDescStyle.Regular);
        g.DrawString(title, footerX + 15 + (hasCover ? 100 : 0), footerY + 10);

        g.DrawString(secondLine, footerX + 15 + (hasCover ? 100 : 0), footerY + 50);

        g.SetColor(ColorDesc.FromArgb(0xffff7f50));
        g.SetFont("exo", 18f, FontDescStyle.Bold);

        g.DrawStringLayout("Generate by AffTools.Aff2Preview 2.1 ",
            footerX + footerW - 500, footerY + footerH - 30,
            500, 30,
            StringAdapterAlignment.Far);
    }

}
