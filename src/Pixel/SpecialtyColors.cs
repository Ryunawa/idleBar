using System;
using Godot;

namespace IdleBar.Pixel;

public static class SpecialtyColors
{
    private const int SwatchSize = 12;

    public static Color[] All { get; } =
    [
        new("d9912b"), new("a8323e"), new("3f8a55"), new("3f6fb5"), new("7d4fa8"), new("e8c04a"), new("e07aa0"), new("b8c2cc"),
    ];

    public static Color Of(int index) => All[Math.Clamp(index, 0, All.Length - 1)];

    public static Texture2D Swatch(int index)
    {
        Image swatch = Image.CreateEmpty(SwatchSize, SwatchSize, false, Image.Format.Rgba8);
        swatch.Fill(Of(index));
        return ImageTexture.CreateFromImage(swatch);
    }
}
