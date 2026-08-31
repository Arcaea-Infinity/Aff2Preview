using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

namespace AffTools.MyGraphics;

#pragma warning disable CA1416

public class GdiImage : ImageDesc
{
    public override void FromFile(string filePath)
    {
        if (InnerImage is Image previousImage)
            previousImage.Dispose();

        InnerImage = null;
        FilePath = filePath;

        if (!string.IsNullOrEmpty(filePath))
            InnerImage = Image.FromFile(filePath);
    }

    public override int GetHeight() => ((InnerImage as Image)?.Height) ?? 0;
    public override int GetWidth() => ((InnerImage as Image)?.Width) ?? 0;
    public override void SaveToPng(string filePath)
    {
        if (InnerImage is Image im)
        {
            im.Save(filePath, ImageFormat.Png);
        }
    }
}

public class GdiPlusAdapter : GraphicsAdapter
{
    private static readonly Vector2[] TriangleSampleOffsets =
    {
        new(0.25f, 0.25f),
        new(0.75f, 0.25f),
        new(0.25f, 0.75f),
        new(0.75f, 0.75f),
    };

    private Graphics g = null!;
    private Image img = null!;
    private Font font = null!;
    private SolidBrush brush = new(Color.White);
    private Pen pen = new(Color.White);

    public static FontStyle ConvertStyle(FontDescStyle s)
        => (FontStyle)s;

    public static StringAlignment ConvertAlign(StringAdapterAlignment s)
        => (StringAlignment)s;

    public override void BeginContext(int width, int height)
    {
        img = new Bitmap(width, height);
        g = Graphics.FromImage(img);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
    }

    public override ImageDesc EndContext()
    {
        g.Dispose();
        font?.Dispose();
        brush.Dispose();
        pen.Dispose();
        ImageDesc im = new GdiImage
        {
            InnerImage = img
        };
        return im;
    }

    public override void DrawImage(ImageDesc image, float x, float y)
    {
        var im = (image.InnerImage as Image) ?? Image.FromFile(image.FilePath);
        g.DrawImage(im, x, y);
    }

    public override void DrawImageCliped(ImageDesc image, float x, float y, float clipx, float clipy, float clipw, float cliph)
    {
        var im = (image.InnerImage as Image) ?? Image.FromFile(image.FilePath);
        g.DrawImage(im,
                   x, y,
                   RectangleF.FromLTRB(clipx, clipy, clipx + clipw, clipy + cliph), GraphicsUnit.Pixel);
    }

    public override void DrawImageClipedAndScaled(ImageDesc image, float x, float y, float w, float h, float clipx, float clipy, float clipw, float cliph)
    {
        var im = (image.InnerImage as Image) ?? Image.FromFile(image.FilePath);
        g.DrawImage(im,
                   RectangleF.FromLTRB(x, y, x + w, y + h),
                   RectangleF.FromLTRB(clipx, clipy, clipx + clipw, clipy + cliph), GraphicsUnit.Pixel);
    }

    public override void DrawImageScaled(ImageDesc image, float x, float y, float w, float h, float transparency)
    {
        var im = (image.InnerImage as Image);
        if (im == null)
        {
            if (image.FilePath == "")
                return;
            im = Image.FromFile(image.FilePath);
        }
        if (transparency > 0)
        {
            float[][] nArray ={ new float[] {1, 0, 0, 0, 0},
                                new float[] {0, 1, 0, 0, 0},
                                new float[] {0, 0, 1, 0, 0},
                                new float[] {0, 0, 0, Math.Clamp(transparency, 0, 1), 0},
                                new float[] {0, 0, 0, 0, 1}};
            ColorMatrix matrix = new(nArray);
            using ImageAttributes attributes = new();
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            var rect = Rectangle.FromLTRB((int)x, (int)y, (int)(x + w), (int)(y + h));
            g.DrawImage(im, rect, 0, 0, im.Width, im.Height, GraphicsUnit.Pixel, attributes);
        }
        else
            g.DrawImage(im,
                       RectangleF.FromLTRB(x, y, x + w, y + h),
                       RectangleF.FromLTRB(0, 0, im.Width, im.Height), GraphicsUnit.Pixel);
    }

    public override void DrawLine(float width, float startx, float starty, float endx, float endy)
    {
        pen.Width = width;
        g.DrawLine(pen, startx, starty, endx, endy);
    }

    public override void DrawLine(ColorDesc color, float width, float startx, float starty, float endx, float endy)
    {
        pen.Color = Color.FromArgb((int)color.ColorArgb);
        pen.Width = width;
        g.DrawLine(pen, startx, starty, endx, endy);
    }

    public override void DrawTriangleStrip(IReadOnlyList<VertexDesc> vertices)
    {
        if (vertices.Count < 4 || vertices.Count % 2 != 0)
            return;

        float minVertexX = vertices.Min(vertex => vertex.Position.X);
        float maxVertexX = vertices.Max(vertex => vertex.Position.X);
        float minVertexY = vertices.Min(vertex => vertex.Position.Y);
        float maxVertexY = vertices.Max(vertex => vertex.Position.Y);

        int originX = Math.Clamp((int)MathF.Floor(minVertexX) - 1, 0, img.Width);
        int originY = Math.Clamp((int)MathF.Floor(minVertexY) - 1, 0, img.Height);
        int right = Math.Clamp((int)MathF.Ceiling(maxVertexX) + 1, 0, img.Width);
        int bottom = Math.Clamp((int)MathF.Ceiling(maxVertexY) + 1, 0, img.Height);
        int width = right - originX;
        int height = bottom - originY;
        if (width <= 0 || height <= 0)
            return;

        // Four alpha samples per pixel provide antialiasing. Samples are merged with
        // max rather than source-over, so adjacent triangles and self-intersections
        // cannot darken a translucent arc.
        var alphaSamples = new byte[checked(width * height * 4)];

        for (int i = 0; i + 3 < vertices.Count; i += 2)
        {
            RasterizeTriangle(
                vertices[i], vertices[i + 1], vertices[i + 2],
                originX, originY, width, height, alphaSamples);
            RasterizeTriangle(
                vertices[i + 1], vertices[i + 3], vertices[i + 2],
                originX, originY, width, height, alphaSamples);
        }

        uint color = vertices[0].ColorArgb;
        byte red = (byte)(color >> 16);
        byte green = (byte)(color >> 8);
        byte blue = (byte)color;

        using Bitmap layer = new(width, height, PixelFormat.Format32bppArgb);
        var data = layer.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            int stride = data.Stride;
            var pixels = new byte[checked(stride * height)];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sampleOffset = (y * width + x) * 4;
                    int alpha = (
                        alphaSamples[sampleOffset] +
                        alphaSamples[sampleOffset + 1] +
                        alphaSamples[sampleOffset + 2] +
                        alphaSamples[sampleOffset + 3] + 2) / 4;
                    if (alpha == 0)
                        continue;

                    int pixelOffset = y * stride + x * 4;
                    pixels[pixelOffset] = blue;
                    pixels[pixelOffset + 1] = green;
                    pixels[pixelOffset + 2] = red;
                    pixels[pixelOffset + 3] = (byte)alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(
                pixels,
                0,
                data.Scan0,
                pixels.Length);
        }
        finally
        {
            layer.UnlockBits(data);
        }

        g.DrawImageUnscaled(layer, originX, originY);
    }

    private static void RasterizeTriangle(
        VertexDesc vertex0,
        VertexDesc vertex1,
        VertexDesc vertex2,
        int originX,
        int originY,
        int targetWidth,
        int targetHeight,
        byte[] alphaSamples)
    {
        Vector2 origin = new(originX, originY);
        Vector2 point0 = vertex0.Position - origin;
        Vector2 point1 = vertex1.Position - origin;
        Vector2 point2 = vertex2.Position - origin;
        float area = Edge(point0, point1, point2);
        if (Math.Abs(area) < 0.0001f)
            return;

        int minX = Math.Clamp(
            (int)MathF.Floor(MathF.Min(point0.X, MathF.Min(point1.X, point2.X))),
            0,
            targetWidth - 1);
        int maxX = Math.Clamp(
            (int)MathF.Ceiling(MathF.Max(point0.X, MathF.Max(point1.X, point2.X))),
            0,
            targetWidth - 1);
        int minY = Math.Clamp(
            (int)MathF.Floor(MathF.Min(point0.Y, MathF.Min(point1.Y, point2.Y))),
            0,
            targetHeight - 1);
        int maxY = Math.Clamp(
            (int)MathF.Ceiling(MathF.Max(point0.Y, MathF.Max(point1.Y, point2.Y))),
            0,
            targetHeight - 1);

        byte alpha0 = (byte)(vertex0.ColorArgb >> 24);
        byte alpha1 = (byte)(vertex1.ColorArgb >> 24);
        byte alpha2 = (byte)(vertex2.ColorArgb >> 24);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int sample = 0; sample < TriangleSampleOffsets.Length; sample++)
                {
                    Vector2 point = new(x, y);
                    point += TriangleSampleOffsets[sample];
                    float weight0 = Edge(point1, point2, point) / area;
                    float weight1 = Edge(point2, point0, point) / area;
                    float weight2 = Edge(point0, point1, point) / area;
                    if (weight0 < -0.0001f || weight1 < -0.0001f || weight2 < -0.0001f)
                        continue;

                    byte alpha = (byte)Math.Clamp(
                        (int)MathF.Round(
                            weight0 * alpha0 +
                            weight1 * alpha1 +
                            weight2 * alpha2),
                        0,
                        255);
                    int offset = (y * targetWidth + x) * 4 + sample;
                    if (alpha > alphaSamples[offset])
                        alphaSamples[offset] = alpha;
                }
            }
        }
    }

    private static float Edge(Vector2 start, Vector2 end, Vector2 point)
        => (end.X - start.X) * (point.Y - start.Y) -
           (end.Y - start.Y) * (point.X - start.X);

    public override void DrawString(string str, ColorDesc color, FontDesc font, float x, float y)
    {
        brush.Color = Color.FromArgb((int)color.ColorArgb);
        using var f = new Font(font.Name, font.EmSize, ConvertStyle(font.Style));
        g.DrawString(str, f, brush, x, y);
    }

    public override void DrawString(string str, float x, float y)
    {
        g.DrawString(str, font, brush, x, y);
    }

    public override void Fill(ColorDesc color)
    {
        g.Clear(Color.FromArgb((int)color.ColorArgb));
    }

    public override void FillRectangle(ColorDesc color, float x, float y, float w, float h)
    {
        brush.Color = Color.FromArgb((int)color.ColorArgb);
        g.FillRectangle(brush, x, y, w, h);
    }

    public override void SetColor(ColorDesc color)
    {
        brush.Color = Color.FromArgb((int)color.ColorArgb);
        pen.Color = Color.FromArgb((int)color.ColorArgb);
    }

    public override void SetFont(string name, float emSize)
    {
        font?.Dispose();
        font = new(name, emSize, FontStyle.Regular);
    }

    public override void SetFont(string name, float emSize, FontDescStyle style)
    {
        font?.Dispose();
        font = new(name, emSize, ConvertStyle(style));
    }

    public override void DrawStringLayout(string str, float x, float y, float w, float h, StringAdapterAlignment align)
    {
        var a = ConvertAlign(align);
        using StringFormat sf = new StringFormat()
        {
            Alignment = a,
        };
        g.DrawString(str, font, brush, RectangleF.FromLTRB(x, y, x + w, y + h), sf);
    }

    public override void DrawStringLayoutLTRB(string str, float l, float t, float r, float b, StringAdapterAlignment align)
    {
        var a = ConvertAlign(align);
        using StringFormat sf = new StringFormat()
        {
            Alignment = a,
        };
        g.DrawString(str, font, brush, RectangleF.FromLTRB(l, t, r, b), sf);
    }
}
