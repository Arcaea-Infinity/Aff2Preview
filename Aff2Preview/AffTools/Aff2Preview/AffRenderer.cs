﻿using System.Numerics;
using System.Text.RegularExpressions;
using AffTools.AffReader;
using AffTools.MyGraphics;

namespace AffTools.Aff2Preview;

internal class AffRenderer
{
    abstract class DrawObjectBase
    {
        public Vector3 Location { get; set; }
        public abstract void Draw(GraphicsAdapter g);
    }

    class ArcSegment : DrawObjectBase
    {
        public Vector3 End { get; set; }
        public int ColorId { get; set; }
        public bool IsVoid { get; set; }
        public override void Draw(GraphicsAdapter g)
        {
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
            g.DrawLine(w, Location.X, Config.TimingToY(Location.Z), End.X, Config.TimingToY(End.Z));
        }
    }

    class ArcTap : DrawObjectBase
    {
        public override void Draw(GraphicsAdapter g)
        {
            g.DrawImageScaled(
                Config.AirTap,
                (int)(Location.X - Config.SingleTrackWidth / 2 + 1),
                (int)(Config.TimingToY(Location.Z) - Config.SkyNoteHeight / 4),
                Config.SingleTrackWidth - 2,
                Config.SkyNoteHeight / 4,
                Math.Clamp(0.3f + Location.Y * 0.7f, 0, 1)
                );
        }
    }

    class ConnectLine : DrawObjectBase
    {
        public Vector3 End { get; set; }
        public override void Draw(GraphicsAdapter g)
        {
            g.DrawLine(
                ColorDesc.FromArgb(Config.GetConnectLineColor()),
                2f,
                Location.X, Config.TimingToY(Location.Z),
                End.X, Config.TimingToY(Location.Z));
        }
    }

    class TextObject : DrawObjectBase
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

        public int NoteHeight { get; set; } = 64;
        public int SkyNoteHeight { get; set; } = 61;
        public float ArcWidth { get; set; } = 20f;
        public float ArcVoidWidth { get; set; } = 3f;

        public int TotalTrackWidth { get; set; } = 248;
        public int DrawingTrackWidth => TotalTrackWidth - 4;
        public int SingleTrackWidth => DrawingTrackWidth / 4;

        public int TimingScale { get; set; } = 5;

        public int Cols { get; set; } = 1;
        public int ColWidth { get; set; } = 0;
        public int Rows { get; set; } = 1;
        public float SegmentLengthInBaseBpm { get; set; } = 0;

        /// <summary>
        /// 0:hikari 1:conflict 2:dynamix
        /// </summary>
        public int Side { get; set; } = 0;

        public uint TrackColor
            => Side switch
            {
                0 => 0xffffffff,
                1 => 0xff382a47,
                _ => 0
            };

        public uint TrackLineColor
            => Side switch
            {
                0 => 0xffd3d3d3,
                1 => 0xff2e1f3c,
                _ => 0
            };

        public uint TrackSegmentLineColor
            => Side switch
            {
                0 => 0xa0d3d3d3,
                1 => 0xc02e1f3c,
                _ => 0
            };

        public uint TrackStripColor
            => Side switch
            {
                0 => 0x08808080,
                1 => 0x08f0f8ff,
                _ => 0
            };

        public uint GetArcVoidColor()
            => Side switch
            {
                0 => 0xffd3d3d3,
                1 => 0xffa9a9a9,
                _ => 0,
            };

        public uint GetArcColor(int type)
            => Side == 0 ?
            type switch
            {
                0 => 0xff31dae7,
                1 => 0xffff69b4,
                2 => 0xff7cfc00,
                _ => 0,
            } :
            type switch
            {
                0 => 0xff00ced1,
                1 => 0xffff1493,
                2 => 0xff006400,
                _ => 0,
            };

        public uint GetConnectLineColor()
            => Side switch
            {
                0 => 0xdc90ee90,
                1 => 0xdcff1493,
                _ => 0,
            };

        public int TotalTrackLength { get; set; } = 0;

        public float TimingToY(float timing)
            => (TotalTrackLength - timing) / TimingScale - 3;
    }

    private readonly AffReader.ArcaeaAffReader affReader = new();

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
        foreach (var affEvent in affReader.Events)
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
        affReader.Parse(AffFile);

        _affAnalyzer = new(affReader);
        _affAnalyzer.AnalyzeSegments();
        _affAnalyzer.AnalyzeNotes();

        Config.TotalTrackLength = (int)_affAnalyzer.totalTime;
    }

    public ImageDesc Draw()
    {
        Config.Side = Side;

        LoadAff();
        if (IsMirror) MirrorAff();

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

        var outputWidth = Config.Cols * colWidth + 100;
        var outputHeight = Config.Rows * (int)segmentLengthInBaseBpm + 200 + 25 + 25;

        GraphicsAdapter g = new GdiPlusAdapter();
        g.BeginContext(outputWidth, outputHeight);
        g.Fill(ColorDesc.FromArgb(Config.Side == 0 ? 0xffffffff : 0xff000000));

        var bw = Config.Background.GetWidth();
        var bh = Config.Background.GetHeight();
        if ((float)outputWidth / bw > (float)outputHeight / bh)
        {
            var dw = outputWidth;
            var rate = (float)dw / bw;
            var dh = bh * rate;
            g.DrawImageScaled(Config.Background, 0, -Math.Abs(outputHeight - dh) / 2, dw, dh);
        }
        else
        {
            var dh = outputHeight;
            var rate = (float)dh / bh;
            var dw = bw * rate;
            g.DrawImageScaled(Config.Background, -Math.Abs(outputWidth - dw) / 2, 0, dw, dh);
        }

        g.FillRectangle(ColorDesc.FromArgb(Config.Side == 0 ? 0xc8f0f0f0 : 0xc8202020),
            25, 25, Config.Cols * Config.ColWidth + 50, Config.Rows * (int)Config.SegmentLengthInBaseBpm + 50 + 25);

        for (int x = 0; x < Config.Cols; x++)
        {
            double y = trackImg.GetHeight() - (x + 1) * Config.Rows * Config.SegmentLengthInBaseBpm;
            g.DrawImageCliped(trackImg, x * colWidth + colWidth - Config.TotalTrackWidth + 50, 50,
                0, (int)y, Config.TotalTrackWidth, (int)(Config.Rows * Config.SegmentLengthInBaseBpm));
        }

        DrawSegmentNumber(g);
        DrawSegmentBpm(g);
        DrawNoteLength(g);

        DrawFooter(g);

        return g.EndContext();
    }

    public void DrawTrack(GraphicsAdapter g)
    {
        for (int i = 0; i < 5; i++)
        {
            g.DrawLine(ColorDesc.FromArgb(Config.TrackLineColor), 0 < i && i < 4 ? 2f : 4f,
                Config.DrawingTrackWidth / 4 * i + 2, 0,
                Config.DrawingTrackWidth / 4 * i + 2, Config.TotalTrackLength / Config.TimingScale);
        }

        g.SetColor(ColorDesc.FromArgb(Config.TrackSegmentLineColor));
        foreach (var t in _affAnalyzer.SegmentTimings)
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
        foreach (var ev in affReader.Events)
        {
            if (ev is ArcaeaAffTap tap)
            {
                float x = Config.DrawingTrackWidth * (tap.Track - 1) / 4;
                float y = Config.TimingToY(tap.Timing);
                g.DrawImageScaled(Config.Tap,
                    x + 3, y - Config.NoteHeight / 4,
                    Config.SingleTrackWidth - 2, Config.NoteHeight / 4);
            }
            else if (ev is ArcaeaAffHold hold)
            {
                float x = Config.DrawingTrackWidth * (hold.Track - 1) / 4;
                float ys = Config.TimingToY(hold.Timing);
                float ye = Config.TimingToY(hold.EndTiming);
                g.DrawImageScaled(Config.Hold,
                    x + 3, ye, Config.SingleTrackWidth - 2, ys - ye);
            }
        }
    }

    public void DrawAirObjects(GraphicsAdapter g)
    {
        List<DrawObjectBase> airObjects = new();
        List<DrawObjectBase> airTaps = new();

        var AddDoubleTip = (float x, float z) =>
        {
            float sx = x <= Config.DrawingTrackWidth / 2 ? x + Config.SingleTrackWidth / 2 + 5 : x - Config.SingleTrackWidth / 2 - 20;
            airObjects.Add(new TextObject()
            {
                Text = "x2",
                Location = new Vector3(sx, 1.5f, z + 80),
                Color = ColorDesc.FromArgb(0xff000000),
                Font = new FontDesc("exo", 10f, FontDescStyle.Bold)
            });
        };

        foreach (var ev in affReader.Events)
        {
            if (ev is ArcaeaAffArc t)
            {
                int duration = t.EndTiming - t.Timing;

                int segSize = duration / (duration < 1000 ? 14 : 7);
                var segmentCount = (segSize == 0 ? 0 : duration / segSize) + 1;

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
                    });
                }

                if (t.ArcTaps is null)
                    continue;

                foreach (var airTapTiming in t.ArcTaps)
                {
                    float tm = airTapTiming - t.Timing;
                    float x = ArcAlgorithm.X(t.XStart, t.XEnd, tm / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                    float y = ArcAlgorithm.Y(t.YStart, t.YEnd, tm / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                    x = (x + 0.5f) * Config.DrawingTrackWidth / 2;

                    airTaps.Add(new ArcTap()
                    {
                        Location = new Vector3(x + 2, y, airTapTiming)
                    });

                    // detect underneath notes
                    foreach (var ev_other in affReader.Events)
                    {
                        if (ev_other is ArcaeaAffTap ev_at)
                        {
                            if (Math.Abs(ev_at.Timing - airTapTiming) <= 3)
                            {
                                float x_t = Config.DrawingTrackWidth * (ev_at.Track - 1) / 4 + Config.SingleTrackWidth / 2;
                                float y_t = Config.TimingToY(ev_at.Timing);

                                airObjects.Add(new ConnectLine()
                                {
                                    Location = new Vector3(x_t + 3, y, airTapTiming + 6),
                                    End = new Vector3(x + 3, 0, airTapTiming + 6)
                                });

                                if (Math.Abs(x - x_t) <= 5)
                                    AddDoubleTip(x, airTapTiming);
                            }
                        }
                        else if (!t.Equals(ev_other) && ev_other is ArcaeaAffArc ev_arc)
                        {
                            if (ev_arc.ArcTaps is null)
                                continue;

                            foreach (int arc_t in ev_arc.ArcTaps)
                            {
                                if (Math.Abs(arc_t - airTapTiming) <= 3)
                                {
                                    float arc_t_x = ArcAlgorithm.X(ev_arc.XStart, ev_arc.XEnd, ev_arc.Timing / duration, ArcaeaAffArc.ToArcLineType(ev_arc.LineType));
                                    float arc_t_y = ArcAlgorithm.Y(ev_arc.YStart, ev_arc.YEnd, ev_arc.Timing / duration, ArcaeaAffArc.ToArcLineType(ev_arc.LineType));
                                    arc_t_x = (arc_t_x + 0.5f) * Config.DrawingTrackWidth / 2;

                                    if (Math.Abs(arc_t_x - x) <= 5 && arc_t_y < y)
                                        AddDoubleTip(x, airTapTiming);
                                }
                            }

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
        ColorDesc c = ColorDesc.FromArgb(Config.Side == 0 ? 0xff000000 : 0xffffffff);
        c.SetColorA(240);
        g.SetColor(c);
        g.SetFont("exo", 10f, FontDescStyle.Bold);

        for (int i = 0, t = 1; i < _affAnalyzer.SegmentTimings.Count; i++)
        {
            int segment_timing = (int)_affAnalyzer.SegmentTimings[i];

            if (segment_timing > _affAnalyzer.totalTime)
                break;

            if (segment_timing < 0)
                continue;

            var next = i + 1;
            if (next < _affAnalyzer.SegmentTimings.Count)
            {
                int segment_timing2 = (int)_affAnalyzer.SegmentTimings[next];
                if (segment_timing2 - segment_timing < 100)
                    continue;
            }

            var colHeight = Config.Rows * Config.SegmentLengthInBaseBpm;
            var sy = segment_timing / Config.TimingScale;

            float y = colHeight - sy % colHeight;
            float x = Config.ColWidth * (sy / (int)colHeight);
            if (y <= 1)
                y = Config.Rows * Config.SegmentLengthInBaseBpm;

            g.DrawString(t.ToString(), x + Config.ColWidth - 220 - 25, y + 37);
            t++;
        }
    }

    public void DrawSegmentBpm(GraphicsAdapter g)
    {
        ColorDesc c = ColorDesc.FromArgb(0xffff7f50);

        g.SetColor(c);
        g.SetFont("exo", 10f, FontDescStyle.Bold);

        float lastBpl = 0;
        float lastBpm = 0;

        foreach (var ev in affReader.Events)
        {
            if (ev is ArcaeaAffTiming t)
            {
                if (t.Timing > Config.TotalTrackLength)
                    break;

                if (t.Timing == 0 && t.TimingGroup != 0)
                    continue;

                if (t.Bpm == 0 || t.Bpm > 1000)
                    continue;

                var colHeight = Config.Rows * Config.SegmentLengthInBaseBpm;
                float rate = t.Bpm / _affAnalyzer.baseBpm;
                float y = colHeight - t.Timing / Config.TimingScale % colHeight;
                float x = Config.ColWidth * (t.Timing / Config.TimingScale / (int)colHeight);
                if (y <= 1)
                    y = (int)colHeight;

                if (lastBpm != t.Bpm)
                {
                    g.DrawStringLayoutLTRB($"{(int)t.Bpm}",
                        x + Config.ColWidth - 220 - 20 - 50, y + 37,
                        x + Config.ColWidth - 220 - 20 - 3, y + 60,
                        StringAdapterAlignment.Far);

                    lastBpm = t.Bpm;
                }
                if (lastBpl != t.BeatsPerLine)
                {
                    var bpl = t.BeatsPerLine;
                    var bpl_str = "";

                    if ((int)(bpl * 100) % 100 == 0)
                        bpl_str = $"{(int)bpl}/4";
                    else if ((int)(bpl * 200) % 100 == 0)
                    {
                        bpl_str = $"{(int)(bpl * 2)}/8";
                    }
                    else if ((int)(bpl * 400) % 100 == 0)
                    {
                        bpl_str = $"{(int)(bpl * 2)}/16";
                    }
                    if (t.Bpm > 0)
                        g.DrawStringLayoutLTRB($"{bpl_str}",
                            x + Config.ColWidth - 220 - 20 - 50, y + 52,
                            x + Config.ColWidth - 220 - 20 - 3, y + 80,
                            StringAdapterAlignment.Far);

                    lastBpl = t.BeatsPerLine;
                }

            }
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

            var colHeight = Config.Rows * Config.SegmentLengthInBaseBpm;
            float y = colHeight - note.TimePoint / Config.TimingScale % colHeight;
            float x = Config.ColWidth * (note.TimePoint / Config.TimingScale / (int)colHeight);
            if (y <= 1)
                y = (int)colHeight;

            string s =
                (note.Divide > 0 ? note.Divide.ToString() : "-") +
                (note.hasDot ? "." : "") +
                (note.beyondFull ? "-" : "");

            g.DrawString(s, x + Config.ColWidth - 218, y + 37);
        }
    }
    public void DrawFooter(GraphicsAdapter g)
    {
        var footerX = 25;
        var footerY = Config.Rows * (int)Config.SegmentLengthInBaseBpm + 200 - 100 + 25;
        var footerW = Config.Cols * Config.ColWidth + 50;
        var footerH = 100;

        g.FillRectangle(ColorDesc.FromArgb(Config.Side == 0 ? 0xc8f0f0f0 : 0xc8202020),
            footerX, footerY, footerW, footerH);

        g.SetColor(ColorDesc.FromArgb(Config.Side == 0 ? 0xffffffff : 0xff000000));

        bool hasCover = Config.Cover.InnerImage is not null;
        if (hasCover)
            g.DrawImageScaled(Config.Cover, footerX, footerY, 100, 100);

        Regex en_regex = new(@"^[A-Za-z\d_\s/\(\)\+\=\-\.\[\]:\(\)&]+$");

        string _title = Title + $"   [ {DiffStr} {Rating:F1} ]" + (Notes > 0 ? $"    Notes {Notes}" : "");

        g.SetColor(ColorDesc.FromArgb(Config.Side == 0 ? 0xff000000 : 0xffffffff));
        g.SetFont(en_regex.IsMatch(_title) ? "GeosansLight" : "Kazesawa Regular", 26f, FontDescStyle.Regular);
        g.DrawString(_title, footerX + 15 + (hasCover ? 100 : 0), footerY + 10);

        string _cmps = (ChartBpm > 0 ? $"Bpm {ChartBpm}     " : "") + $"{Artist} / {Charter}";

        g.SetFont(en_regex.IsMatch(_cmps) ? "GeosansLight" : "Kazesawa Regular", 26f, FontDescStyle.Regular);
        g.DrawString(_cmps, footerX + 15 + (hasCover ? 100 : 0), footerY + 50);

        g.SetColor(ColorDesc.FromArgb(0xffff7f50));
        g.SetFont("exo", 18f, FontDescStyle.Bold);

        g.DrawStringLayout("Generate by AffTools.Aff2Preview 2.0 ",
            footerX + footerW - 500, footerY + footerH - 30,
            500, 30,
            StringAdapterAlignment.Far);
    }

}
