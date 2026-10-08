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
    private static readonly Color Drop = new("9fb4c8");

    public static void Paint(PixelCanvas canvas, int top, int x, Outdoors outdoors, float time)
    {
        int y = top + Top;
        SkyLight sky = outdoors.Sky;
        float gloom = outdoors.Raining ? 0.55f : 0;
        canvas.Fill(x, y, Width, Height, Frame);
        canvas.Fill(x + 1, y + 1, Width - 2, 4, sky.Sky(Night).Lerp(Cloud, gloom));
        canvas.Fill(x + 1, y + 5, Width - 2, Height - 6, sky.Horizon(Night).Lerp(Cloud, gloom));
        if (outdoors.Raining)
        {
            PaintRain(canvas, x, y, time);
        }
        else if (sky.Daylight < 0.3f)
        {
            PaintStars(canvas, x, y, time);
        }

        canvas.Fill(x + Width / 2, y + 1, 1, Height - 2, Frame);
        canvas.Fill(x + 1, y + 4, Width - 2, 1, Frame);
        canvas.Fill(x - 1, y + Height, Width + 2, 1, Sill);
    }

    private static void PaintRain(PixelCanvas canvas, int x, int y, float time)
    {
        for (int column = 1; column < Width - 1; column += 2)
        {
            int fall = (int)(time * 14 + column * 3.7f) % (Height - 2);
            canvas.Fill(x + column, y + 1 + fall, 1, 2, Drop with { A = 0.8f });
        }
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
