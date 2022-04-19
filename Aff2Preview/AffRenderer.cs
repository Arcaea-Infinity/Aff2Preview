using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using System.Text.RegularExpressions;

#pragma warning disable CA1416 // 验证平台兼容性

namespace AimuBotCS.Modules.Arcaea.Aff2Preview
{
    internal class AffRenderer
    {
        readonly AffReader affReader = new();

        private const int note_height = 64;
        private const int sky_note_height = 61;
        private const int whole_track_width = 247;
        private const int track_width = whole_track_width / 4;

        private const float void_arc_width = 3f;
        private const float arc_width = 20f;

        float bpm = 0;
        float bpl = 0;
        double time_per_line = 0;
        float total_time = 0;
        bool side_light = true;
        int s_counts = 0;

        Image? im_tap, im_hold, im_sky, bg;

        SortedSet<int> note_timings = new();

        public void LoadResource(string tap_note_img, string hold_note_img, string sky_note_img, string bg_img)
        {
            im_tap = Image.FromFile(tap_note_img);
            im_hold = Image.FromFile(hold_note_img);
            im_sky = Image.FromFile(sky_note_img);
            bg = Image.FromFile(bg_img);
        }

        private async Task LoadAff(string aff_path, int side)
        {
            affReader.ParseFile(aff_path);

            var global_group = affReader.Events[0] as ArcaeaAffTiming;
            if (global_group is not null)
            {
                bpm = global_group.Bpm;
                bpl = global_group.BeatsPerLine;
                time_per_line = 60 * 1000 * (int)bpl / bpm;
            }

            foreach (var ev in affReader.Events)
            {
                if (ev is ArcaeaAffTap)
                    total_time = MathF.Max(total_time, ev.Timing);

                else if (ev is ArcaeaAffArc arc)
                    total_time = MathF.Max(total_time, arc.EndTiming);

                else if (ev is ArcaeaAffHold hd)
                    total_time = MathF.Max(total_time, hd.EndTiming);

                else if (ev is ArcaeaAffTiming tm)
                    total_time = MathF.Max(total_time, tm.Timing);
            }

            total_time += (float)time_per_line;

            side_light = side == 0;

            for (double i = 0; i < total_time; i += time_per_line)
            {
                s_counts++;
            }
        }

        private void DrawTrack(Graphics g)
        {
            //draw track strip

            Color color_line = side_light ? Color.LightGray : Color.FromArgb(46, 31, 60);

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Near;

            Pen track_pen = new Pen(color_line, 3f);
            for (int i = 0; i < 5; i++)
            {
                g.DrawLine(track_pen, track_width * i + 1, 0, track_width * i + 1, (int)total_time / 10);
            }

            track_pen.Color = Color.FromArgb(128, color_line);
            track_pen.Width = 1f;
            for (double i = 0; i < total_time; i += time_per_line / (int)bpl)
            {
                g.DrawLine(track_pen, 0, (int)((total_time - i) / 10), whole_track_width, (int)((total_time - i) / 10));
            }

            track_pen.Color = Color.FromArgb(192, color_line);
            track_pen.Width = 2f;
            for (double i = 0; i < total_time; i += time_per_line)
            {
                g.DrawLine(track_pen, 0, (int)((total_time - i) / 10), whole_track_width, (int)((total_time - i) / 10));
            }

            track_pen.Color = Color.FromArgb(8, side_light ? Color.Gray : Color.AliceBlue);
            track_pen.Width = 57f;
            for (int i = 0; i < total_time; i += 45)
            {
                g.DrawLine(track_pen, -20, i, 300, i - 200);
            }
        }

        public async Task<Image?> Render(string aff_path, int side, int difficulty, string cover_path, string title, string rating_text, string artist, string charter)
        {
            await LoadAff(aff_path, side);

            Image img = new Bitmap(whole_track_width, (int)total_time / 10);

            using (Graphics g = Graphics.FromImage(img))
            {
                g.Clear(side_light ? Color.White : Color.FromArgb(57, 42, 71));
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.TextContrast = 8;

                Color color_line = side_light ? Color.LightGray : Color.FromArgb(46, 31, 60);

                FontFamily f = new("Exo");
                Font font_exo = new(f, 11f, FontStyle.Bold);

                Brush b = new SolidBrush(Color.Black);

                StringFormat stringFormat = new();
                stringFormat.Alignment = StringAlignment.Near;

                DrawTrack(g);

                // draw tap and hold notes

                foreach (var ev in affReader.Events)
                {
                    if (ev is ArcaeaAffTap ev_tap)
                    {
                        note_timings.Add(ev_tap.Timing);
                        float x = whole_track_width * (ev_tap.Track - 1) / 4;
                        float y = (total_time - ev_tap.Timing) / 10;
                        var rect = RectangleF.FromLTRB(x + 1, y - note_height / 4, x + track_width - 1, y);
                        g.DrawImage(im_tap, rect);
                    }
                    else if (ev is ArcaeaAffHold ev_hold)
                    {
                        note_timings.Add(ev_hold.Timing);
                        float x = whole_track_width * (ev_hold.Track - 1) / 4;
                        float ys = (total_time - ev_hold.Timing) / 10;
                        float ye = (total_time - ev_hold.EndTiming) / 10;
                        g.DrawImage(im_hold,
                            RectangleF.FromLTRB
                            (x + 1, ye, x + track_width - 1, ys));
                    }
                }

                // draw arc

                Dictionary<int, List<ArcaeaAffArc>> arcs = new();

                SolidBrush note_text_brush = new(Color.FromArgb(200, side_light ? Color.Black : Color.White));

                Color _void_color = side_light ? Color.LightGray : Color.DarkGray;
                Color _red_color = side_light ? Color.HotPink : Color.DeepPink;
                Color _blue_color = side_light ? Color.FromArgb(49, 218, 231) : Color.DarkTurquoise;
                Color _green_color = side_light ? Color.LawnGreen : Color.DarkGreen;

                foreach (var ev in affReader.Events)
                {
                    if (ev is ArcaeaAffArc t)
                    {
                        if (!t.IsVoid)
                            arcs[t.Color].Add(t);

                        int duration = t.EndTiming - t.Timing;

                        int segSize = duration / (duration < 1000 ? 14 : 7);
                        var segmentCount = (segSize == 0 ? 0 : duration / segSize) + 1;

                        List<Vector3> segments = new();

                        Vector3 start = new();
                        Vector3 end = new((t.XStart + 0.5f) * whole_track_width / 2, t.YStart, t.Timing);
                        segments.Add(end);

                        for (int i = 0; i < segmentCount - 1; ++i)
                        {
                            start = end;
                            float x = ArcAlgorithm.X(t.XStart, t.XEnd, (i + 1f) * segSize / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                            float y = ArcAlgorithm.Y(t.YStart, t.YEnd, (i + 1f) * segSize / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                            end = new Vector3((x + 0.5f) * whole_track_width / 2,
                                              y,
                                              t.Timing + segSize * (i + 1));
                            segments.Add(end);
                        }

                        {
                            start = end;
                            end = new Vector3((t.XEnd + 0.5f) * whole_track_width / 2,
                                              t.YEnd,
                                              t.EndTiming);
                            segments.Add(end);
                        }

                        Color color = (t.IsVoid ? _void_color :
                            (t.Color == 0 ? _blue_color : t.Color == 1 ? _red_color : _green_color));

                        Pen pen = new(color, t.IsVoid ? void_arc_width : arc_width);

                        for (int i = 0; i < segments.Count - 1; i++)
                        {
                            var st = segments[i];
                            var ed = segments[i + 1];
                            pen.Color = Color.FromArgb(Math.Clamp((int)(47 + (230 - 47) * st.Y), 0, 255), pen.Color.R, pen.Color.G, pen.Color.B);
                            g.DrawLine(pen, st.X, (total_time - st.Z) / 10f, ed.X, (total_time - ed.Z) / 10f);
                        }

                        // draw connect lines between tap and sky notes
                        Pen pen_connect = new(side_light ? Color.FromArgb(220, Color.LightGreen) : Color.FromArgb(220, Color.DeepPink), 2f);
                        if (t.ArcTaps is not null)
                        {
                            foreach (var at in t.ArcTaps)
                            {
                                note_timings.Add(at);
                                float tm = at - t.Timing;
                                float x = ArcAlgorithm.X(t.XStart, t.XEnd, tm / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                                float y = ArcAlgorithm.Y(t.YStart, t.YEnd, tm / duration, ArcaeaAffArc.ToArcLineType(t.LineType));
                                float z = at;
                                x = (x + 0.5f) * whole_track_width / 2;
                                z = (total_time - z) / 10f;

                                foreach (var ev_t in affReader.Events)
                                {
                                    if (ev_t is ArcaeaAffTap ev_at)
                                    {
                                        if (ev_at.Timing == at)
                                        {
                                            float x_t = whole_track_width * (ev_at.Track - 1) / 4 + track_width / 2;
                                            float y_t = (total_time - ev_at.Timing) / 10;
                                            g.DrawLine(pen_connect, x_t, y_t - 1, x, z - 1);

                                            //detect underneath notes
                                            if (Math.Abs(x - x_t) <= 5)
                                            {
                                                g.DrawString("x2", font_exo, note_text_brush, x + track_width / 2, z - 16);
                                            }
                                        }
                                    }
                                }

                                // adjust tansparency for sky notes by Y
                                float[][] nArray ={ new float[] {1, 0, 0, 0, 0},
                                                    new float[] {0, 1, 0, 0, 0},
                                                    new float[] {0, 0, 1, 0, 0},
                                                    new float[] {0, 0, 0, Math.Clamp(0.3f + y * 0.7f, 0, 1), 0},
                                                    new float[] {0, 0, 0, 0, 1}};
                                ColorMatrix matrix = new(nArray);
                                ImageAttributes attributes = new();
                                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                                var rect = Rectangle.FromLTRB(
                                    (int)(x - track_width / 2 + 1), (int)(z - sky_note_height / 4),
                                    (int)(x + track_width - track_width / 2 - 1), (int)(z));
                                g.DrawImage(im_sky, rect, 0, 0, im_sky.Width, im_sky.Height, GraphicsUnit.Pixel, attributes);

                                // detect underneath notes
                                foreach (var ev_other_arc in affReader.Events)
                                {
                                    if (!t.Equals(ev_other_arc) && ev_other_arc is ArcaeaAffArc ev_arc)
                                    {
                                        if (ev_arc.ArcTaps is not null)
                                        {
                                            foreach (int arc_t in ev_arc.ArcTaps)
                                            {
                                                if (arc_t == at)
                                                {
                                                    float arc_t_r = arc_t / ev_arc.Timing;
                                                    float arc_t_x = ArcAlgorithm.X(ev_arc.XStart, ev_arc.XEnd, arc_t_r / duration, ArcaeaAffArc.ToArcLineType(ev_arc.LineType));
                                                    float arc_t_y = ArcAlgorithm.Y(ev_arc.YStart, ev_arc.YEnd, arc_t_r / duration, ArcaeaAffArc.ToArcLineType(ev_arc.LineType));
                                                    arc_t_x = (arc_t_x + 0.5f) * whole_track_width / 2;
                                                    if (Math.Abs(arc_t_x - x) <= 5 && arc_t_y < y)
                                                    {
                                                        g.DrawString("x2", font_exo, note_text_brush, x + track_width / 2, z - 16);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }

                foreach (var (arc_color, arc_desc) in arcs)
                {
                    for (int i = arc_desc.Count - 1; i > 0; i--)
                    {
                        if (arc_desc[i].Timing != arc_desc[i - 1].EndTiming)
                            note_timings.Add(arc_desc[i].Timing);
                    }
                    note_timings.Add(arc_desc[0].Timing);
                }
            }

            double pixels_per_s = time_per_line / 10;
            int rows = s_counts;
            int cols = 1;
            int col_width = whole_track_width + 73;

            for (; rows > 0; rows--)
            {
                cols = s_counts / rows + (s_counts % rows == 0 ? 0 : 1);
                double w = cols * col_width;
                double h = rows * pixels_per_s;
                if (w / h >= 4f / 3f)
                {
                    break;
                }
            }

            List<NoteDesc> affNotes = new();
            for (int i = 0; i < note_timings.Count - 1; i++)
            {
                NoteDesc n = new(note_timings.ElementAt(i), note_timings.ElementAt(i + 1) - note_timings.ElementAt(i), bpm);
                affNotes.Add(n);
            }

            Image bim = new Bitmap(cols * col_width + 100, rows * (int)pixels_per_s + 200 + 25 + 25);
            using (Graphics g = Graphics.FromImage(bim))
            {
                g.Clear(side_light ? Color.White : Color.Black);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.TextContrast = 8;

                g.DrawImage(bg,
                    RectangleF.FromLTRB(0, 0, cols * col_width + 100, (cols * col_width + 100) * bg.Height / bg.Width),
                    RectangleF.FromLTRB(0, 0, bg.Width, bg.Height), GraphicsUnit.Pixel);

                FontFamily f = new("Exo");
                Font ft = new(f, 10f, FontStyle.Bold);

                StringFormat sf = new();
                sf.Alignment = StringAlignment.Near;

                SolidBrush sb = new(side_light ? Color.FromArgb(200, 240, 240, 240) : Color.FromArgb(200, 32, 32, 32));
                g.FillRectangle(sb, 25, 25, cols * col_width + 50, rows * (int)pixels_per_s + 50 + 25);

                // split draw strips to big image
                for (int x = 0; x < cols; x++)
                {
                    double y = img.Height - (x + 1) * rows * pixels_per_s;
                    g.DrawImage(img, x * col_width + col_width - whole_track_width + 50, 50,
                        RectangleF.FromLTRB(0, (float)y, whole_track_width, (float)(y + rows * pixels_per_s)), GraphicsUnit.Pixel);
                }

                // draw segments number
                sb.Color = Color.FromArgb(240, side_light ? Color.Black : Color.White);
                for (int i = 0; i < s_counts; i++)
                {
                    int x = i / rows;
                    int y = i % rows;
                    x *= col_width;
                    y = (int)(y * pixels_per_s);
                    y = (int)(rows * pixels_per_s) - y;
                    g.DrawString(i.ToString(), ft, sb, x + col_width - 220 - 20, y + 40);
                }

                g.DrawString($"{bpm}  {bpl}/{4}", ft, sb, 40 + 15, (int)(rows * pixels_per_s) + 13 + 43);

                // draw timinggroup speed
                sb.Color = Color.Coral;
                sf.Alignment = StringAlignment.Far;
                foreach (var ev in affReader.Events)
                {
                    if (ev is ArcaeaAffTiming t)
                    {
                        float rate = t.Bpm / bpm;
                        int y = (int)(rows * pixels_per_s - ((t.Timing / 10) % (rows * pixels_per_s)));
                        int x = col_width * (t.Timing / 10 / (int)(rows * pixels_per_s));
                        if (y <= 1)
                        {
                            y = (int)(rows * pixels_per_s);
                            x += col_width;
                        }
                        g.DrawString($"{rate:F1}", ft, sb, RectangleF.FromLTRB(x + col_width - 220 - 20 - 50, y + 20, x + col_width - 220 - 20 - 3, y + 40), sf);
                    }
                }

                // draw note divide
                sb.Color = Color.DarkCyan;
                foreach (var t in affNotes)
                {
                    int y = (int)(rows * pixels_per_s - ((t.timing / 10) % (rows * pixels_per_s)));
                    int x = col_width * (t.timing / 10 / (int)(rows * pixels_per_s));
                    if (y <= 1)
                    {
                        y = (int)(rows * pixels_per_s);
                        x += col_width;
                    }
                    string s = (t.divide > 0 ? t.divide.ToString() : "-") +
                        (t.hasDot ? "." : "") +
                        (t.isTriplet ? "t" : "") +
                        (t.beyondFull ? "-" : "");
                    g.DrawString(s, ft, sb, x + col_width - 215, y + 40);
                }


                // draw footer aff info
                // remove if unwanted
                sb.Color = side_light ? Color.FromArgb(200, 240, 240, 240) : Color.FromArgb(200, 32, 32, 32);
                g.FillRectangle(sb, 25, (rows * (int)pixels_per_s) + 200 - 100 + 25, cols * col_width + 50, 100);

                sb.Color = side_light ? Color.Black : Color.White;
                if (new FileInfo(cover_path).Exists)
                {
                    Image cover = Image.FromFile(cover_path);
                    g.DrawImage(cover, RectangleF.FromLTRB(25, rows * (int)pixels_per_s + 200 - 100 + 25, 125, rows * (int)pixels_per_s + 200 + 25),
                        RectangleF.FromLTRB(0, 0, cover.Width, cover.Height), GraphicsUnit.Pixel);
                }

                string diff_str = difficulty switch
                {
                    0 => "PST",
                    1 => "PRS",
                    2 => "FTR",
                    3 => "BYD",
                    _ => ""
                };

                Regex en_regex = new(@"^[A-Za-z\d_\s/\(\)\+\=\-]+$");

                string _title = title + $" {diff_str} {rating_text}";
                g.DrawString(_title,
                    new Font(en_regex.IsMatch(_title) ? "Noto Sans CJK SC Regular" : "Kazesawa Regular", 24f, FontStyle.Regular), sb, 140, rows * (int)pixels_per_s + 200 - 100 + 10 + 25);

                string _cmps = $"{artist} / {charter}";
                g.DrawString(_cmps,
                    new Font(en_regex.IsMatch(_cmps) ? "Noto Sans CJK SC Regular" : "Kazesawa Regular", 16f, FontStyle.Regular), sb, 140 + 2, rows * (int)pixels_per_s + 200 - 100 + 50 + 25);

                sb.Color = Color.Coral;
                sf.Alignment = StringAlignment.Far;
                g.DrawString("Generate by Aff2Preview 1.2 ", new Font(f, 17f, FontStyle.Bold), sb,
                    RectangleF.FromLTRB(25 + cols * col_width + 50 - 500, rows * (int)pixels_per_s + 200 - 30 + 25, 25 + cols * col_width + 50, rows * (int)pixels_per_s + 200 + 25), sf);
            }

            return bim;
        }

    }
}
