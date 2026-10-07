using Godot;

namespace IdleBar.Pixel;

public sealed record PixelRing(Color TopLeft, Color BottomRight)
{
    public static PixelRing Solid(Color color) => new(color, color);
}
