namespace AffTools.MyGraphics;

public class SkiaAdapter : GraphicsAdapter
{
    public override void BeginContext(int width, int height) => throw new NotImplementedException();
    public override void DrawImage(ImageDesc image, float x, float y) => throw new NotImplementedException();
    public override void DrawImageCliped(ImageDesc image, float x, float y, float clipx, float clipy, float clipw, float cliph) => throw new NotImplementedException();
    public override void DrawImageClipedAndScaled(ImageDesc image, float x, float y, float width, float height, float clipx, float clipy, float clipw, float cliph) => throw new NotImplementedException();
    public override void DrawImageScaled(ImageDesc image, float x, float y, float width, float height, float transparency = 0) => throw new NotImplementedException();
    public override void DrawLine(ColorDesc color, float width, float startx, float starty, float endx, float endy) => throw new NotImplementedException();
    public override void DrawLine(float width, float startx, float starty, float endx, float endy) => throw new NotImplementedException();
    public override void DrawString(string str, ColorDesc color, FontDesc font, float x, float y) => throw new NotImplementedException();
    public override void DrawString(string str, float x, float y) => throw new NotImplementedException();
    public override void DrawStringLayout(string str, float x, float y, float w, float h, StringAdapterAlignment align) => throw new NotImplementedException();
    public override void DrawStringLayoutLTRB(string str, float l, float t, float r, float b, StringAdapterAlignment align) => throw new NotImplementedException();
    public override ImageDesc EndContext() => throw new NotImplementedException();
    public override void Fill(ColorDesc color) => throw new NotImplementedException();
    public override void FillRectangle(ColorDesc color, float x, float y, float w, float h) => throw new NotImplementedException();
    public override void SetColor(ColorDesc color) => throw new NotImplementedException();
    public override void SetFont(string name, float emSize) => throw new NotImplementedException();
    public override void SetFont(string name, float emSize, FontDescStyle style) => throw new NotImplementedException();
}

