using System.Runtime.InteropServices;

namespace AffTools.MyGraphics;

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct ColorRaw
{
    [FieldOffset(0)] public uint argb;
    [FieldOffset(3)] public byte a;
    [FieldOffset(2)] public byte r;
    [FieldOffset(1)] public byte g;
    [FieldOffset(0)] public byte b;
}

public class ColorDesc
{
    public ColorRaw InnerColor;

    public uint ColorArgb => InnerColor.argb;

    public void SetColor(uint argb)
        => InnerColor.argb = argb;

    public void SetColorA(byte a)
        => InnerColor.a = a;

    public void SetColor(byte a, byte r, byte g, byte b)
    {
        InnerColor.a = a;
        InnerColor.r = r;
        InnerColor.g = g;
        InnerColor.b = b;
    }

    public static ColorDesc FromArgb(uint argb)
    {
        ColorDesc color = new();
        color.SetColor(argb);
        return color;
    }
    public static ColorDesc FromArgb(byte a, byte r, byte g, byte b)
    {
        ColorDesc color = new();
        color.SetColor(a,r,g,b);
        return color;
    }
}

public enum FontDescStyle
{
    Regular = 0x0,
    Bold = 0x1,
    Italic = 0x2,
    Underline = 0x4,
    Strikeout = 0x8
}

public enum StringAdapterAlignment
{
    Near = 0,
    Center = 1,
    Far = 2,
}

public class FontDesc
{
    public object? InnerFont { get; set; } = null;
    public string Name { get; set; } = "";
    public int PixelHeight { get; set; } = 0;
    public float EmSize { get; set; } = 0;
    public FontDescStyle Style { get; set; } = FontDescStyle.Regular;

    public FontDesc() { }

    public FontDesc(string name, float emSize, FontDescStyle style)
    {
        Name = name;
        EmSize = emSize;
        Style = style;
    }
}

public abstract class ImageDesc
{
    public object? InnerImage { get; set; } = null;
    public string FilePath { get; set; } = "";
    public abstract void FromFile(string filePath);
    public abstract int GetWidth();
    public abstract int GetHeight();
    public abstract void SaveToPng(string filePath);
}

public enum Alignment
{
    Near,
    Center,
    Far,
}

public abstract class GraphicsAdapter
{
    public abstract void BeginContext(int width, int height);
    public abstract ImageDesc EndContext();
    public abstract void SetColor(ColorDesc color);
    public abstract void SetFont(string name, float emSize);
    public abstract void SetFont(string name, float emSize, FontDescStyle style);
    public abstract void Fill(ColorDesc color);
    public abstract void DrawImage(ImageDesc image, float x, float y);
    public abstract void DrawImageScaled(ImageDesc image, float x, float y, float width, float height, float transparency = 0);
    public abstract void DrawImageCliped(ImageDesc image, float x, float y, float clipx, float clipy, float clipw, float cliph);
    public abstract void DrawImageClipedAndScaled(ImageDesc image, float x, float y, float width, float height, float clipx, float clipy, float clipw, float cliph);
    public abstract void FillRectangle(ColorDesc color, float x, float y, float w, float h);
    public abstract void DrawLine(ColorDesc color, float width, float startx, float starty, float endx, float endy);
    public abstract void DrawLine(float width, float startx, float starty, float endx, float endy);
    public abstract void DrawString(string str, ColorDesc color, FontDesc font, float x, float y);
    public abstract void DrawString(string str, float x, float y);
    public abstract void DrawStringLayout(string str, float x, float y, float w, float h, StringAdapterAlignment align);
    public abstract void DrawStringLayoutLTRB(string str, float l, float t, float r, float b, StringAdapterAlignment align);
}
