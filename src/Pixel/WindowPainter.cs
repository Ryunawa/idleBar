using System;
using Godot;

namespace IdleBar.Pixel;

public static class WindowPainter
{
    public const int Width = 13;
    private const int Top = 4;
    private const int Height = 9;

    private static readonly Color Night = new("16233a");
    private static readonly Color Frame = PixelPalette.Resolve('B');
    private static readonly Color Sill = PixelPalette.Resolve('o');
    private static readonly Color Star = PixelPalette.Resolve('h');
    private static readonly Color Cloud = new("5d6670");

    public static void Paint(PixelCanvas canvas, int top, int x, Outdoors outdoors, float time)
    {
        int y = top + Top;
        SkyLight sky = outdoors.Sky;
        float gloom = outdoors.Weather switch
        {
            Precipitation.Rain => 0.55f,
            Precipitation.Snow => 0.35f,
            _ => 0,
        };
        canvas.Fill(x, y, Width, Height, Frame);
        canvas.Fill(x + 1, y + 1, Width - 2, 4, sky.Sky(Night).Lerp(Cloud, gloom));
        canvas.Fill(x + 1, y + 5, Width - 2, Height - 6, sky.Horizon(Night).Lerp(Cloud, gloom));
        if (!outdoors.Raining && sky.Daylight < 0.3f)
        {
            PaintStars(canvas, x, y, time);
        }

        SkyEffects.Paint(canvas, x, y, outdoors, time);

        canvas.Fill(x + Width / 2, y + 1, 1, Height - 2, Frame);
        canvas.Fill(x + 1, y + 4, Width - 2, 1, Frame);
        canvas.Fill(x - 1, y + Height, Width + 2, 1, outdoors.Weather == Precipitation.Snow ? Star : Sill);
    }

    private static void PaintStars(PixelCanvas canvas, int x, int y, float time)
    {
        int[] columns = [2, 9, 4, 10];
        int[] rows = [2, 1, 6, 6];
        for (int star = 0; star < columns.Length; star++)
        {
            float twinkle = 0.55f + 0.45f * MathF.Sin(time * 2 + star * 1.7f + x);
            canvas.Fill(x + columns[star], y + rows[star], 1, 1, Star with { A = twinkle });
        }
    }
}
