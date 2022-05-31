using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AffTools.MyGraphics;

#pragma warning disable CA1416

public class GdiImage : ImageDesc
{
    public override void FromFile(string filePath)
    {
        if (filePath != "")
            InnerImage = Image.FromFile(filePath);
    }

    public override int GetHeight() => ((InnerImage as Image)?.Height) ?? 0;
    public override int GetWidth() => ((InnerImage as Image)?.Width) ?? 0;
    public override void SaveToPng(string filePath)
    {
        if (InnerImage is Image im)
        {
            im.Save(filePath);
        }
    }
}

public class GdiPlusAdapter : GraphicsAdapter
{
    private Graphics g;
    private Image img;
    private Font font;
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
            ImageAttributes attributes = new();
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

    public override void DrawString(string str, ColorDesc color, FontDesc font, float x, float y)
    {
        brush.Color = Color.FromArgb((int)color.ColorArgb);
        var f = new Font(font.Name, font.EmSize, ConvertStyle(font.Style));
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
        font = new(name, emSize, FontStyle.Regular);
    }

    public override void SetFont(string name, float emSize, FontDescStyle style)
    {
        font = new(name, emSize, ConvertStyle(style));
    }

    public override void DrawStringLayout(string str, float x, float y, float w, float h, StringAdapterAlignment align)
    {
        var a = ConvertAlign(align);
        StringFormat sf = new StringFormat()
        {
            Alignment = a,
        };
        g.DrawString(str, font, brush, RectangleF.FromLTRB(x, y, x + w, y + h), sf);
    }

    public override void DrawStringLayoutLTRB(string str, float l, float t, float r, float b, StringAdapterAlignment align)
    {
        var a = ConvertAlign(align);
        StringFormat sf = new StringFormat()
        {
            Alignment = a,
        };
        g.DrawString(str, font, brush, RectangleF.FromLTRB(l, t, r, b), sf);
    }
}
