using System;
using Godot;

namespace IdleBar.Pixel;

public sealed class PixelCanvas
{
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

    public void Draw(PixelSprite sprite, int x, int y) =>
        _target.DrawTextureRect(sprite.Texture, new Rect2(x * _scale, y * _scale, sprite.Width * _scale, sprite.Height * _scale), false);
}
