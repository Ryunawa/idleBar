using System;
using Godot;

namespace IdleBar.Pixel;

public sealed class PixelCanvas
{
    private const int TextOutline = 4;

    private readonly CanvasItem _target;
    private readonly float _scale;

    public PixelCanvas(CanvasItem target, float scale, Vector2 size)
    {
        _target = target;
        _scale = scale;
        Width = (int)Math.Ceiling(size.X / scale);
        Height = (int)Math.Floor(size.Y / scale);
    }

    public int Width { get; }

    public int Height { get; }

    public void Fill(int x, int y, int width, int height, Color color) =>
        _target.DrawRect(new Rect2(x * _scale, y * _scale, width * _scale, height * _scale), color);

    public void Draw(PixelSprite sprite, int x, int y) => Draw(sprite, x, y, Colors.White);

    public void Draw(PixelSprite sprite, int x, int y, Color modulate) =>
        _target.DrawTextureRect(sprite.Texture, new Rect2(x * _scale, y * _scale, sprite.Width * _scale, sprite.Height * _scale), false, modulate);

    public int TextWidth(Font font, int fontSize, string text) =>
        (int)MathF.Ceiling(font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X / _scale);

    public void Text(Font font, int fontSize, string text, int x, int y, Color color, Color outline)
    {
        Vector2 at = new(MathF.Round(x * _scale), MathF.Round(y * _scale + font.GetAscent(fontSize)));
        _target.DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, fontSize, TextOutline, outline);
        _target.DrawString(font, at, text, HorizontalAlignment.Left, -1, fontSize, color);
    }
}
