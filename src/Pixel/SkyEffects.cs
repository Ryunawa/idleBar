using System;
using Godot;

namespace IdleBar.Pixel;

public static class SkyEffects
{
    private const int PaneWidth = WindowPainter.Width - 2;
    private const int PaneHeight = 7;
    private const float BatEvery = 9f;
    private const float BatSeconds = 1.6f;

    private static readonly Color Drop = new("9fb4c8");
    private static readonly Color Flake = PixelPalette.Resolve('h');
    private static readonly Color[] Leaves = [PixelPalette.Resolve('a'), PixelPalette.Resolve('I'), PixelPalette.Resolve('g')];
    private static readonly Color Petal = new("f4b6c8");
    private static readonly Color Bat = PixelPalette.Resolve('k');

    public static void Paint(PixelCanvas canvas, int x, int y, Outdoors outdoors, float time)
    {
        switch (outdoors.Weather)
        {
            case Precipitation.Rain:
                Fall(canvas, x, y, time, 14, 2, 2, Drop with { A = 0.8f }, 0);
                break;
            case Precipitation.Snow:
                Fall(canvas, x, y, time, 3, 3, 1, Flake with { A = 0.9f }, 1);
                break;
            default:
                Drift(canvas, x, y, outdoors.Season, time);
                break;
        }

        if (outdoors.Festival == Festival.Halloween && outdoors.Sky.Daylight < 0.5f)
        {
            PaintBat(canvas, x, y, time);
        }
    }

    private static void Fall(PixelCanvas canvas, int x, int y, float time, float speed, int spacing, int length, Color color, int sway)
    {
        for (int column = 1; column < PaneWidth; column += spacing)
        {
            int fall = (int)(time * speed + column * 3.7f) % PaneHeight;
            int drift = sway == 0 ? 0 : (int)MathF.Round(MathF.Sin(time * 1.5f + column) * sway);
            int at = Math.Clamp(column + drift, 0, PaneWidth - 1);
            canvas.Fill(x + 1 + at, y + 1 + fall, 1, Math.Min(length, PaneHeight - fall), color);
        }
    }

    private static void Drift(PixelCanvas canvas, int x, int y, Season season, float time)
    {
        if (season is Season.Winter or Season.Summer)
        {
            return;
        }

        for (int piece = 0; piece < 2; piece++)
        {
            float phase = (time * 0.35f + piece * 0.5f + x * 0.013f) % 1f;
            int across = (int)(phase * PaneWidth);
            int down = (int)(phase * PaneHeight + MathF.Sin(time * 3 + piece) * 1.5f);
            Color color = season == Season.Autumn ? Leaves[(piece + x) % Leaves.Length] : Petal;
            if (down is >= 0 and < PaneHeight)
            {
                canvas.Fill(x + 1 + across, y + 1 + down, 1, 1, color);
            }
        }
    }

    private static void PaintBat(PixelCanvas canvas, int x, int y, float time)
    {
        float moment = (time + x * 0.07f) % BatEvery;
        if (moment > BatSeconds)
        {
            return;
        }

        int across = (int)(moment / BatSeconds * (PaneWidth + 3)) - 2;
        int height = y + 2 + (int)(MathF.Sin(moment * 9) * 1.2f);
        bool up = (int)(time * 10) % 2 == 0;
        for (int wing = -1; wing <= 1; wing++)
        {
            int at = across + wing;
            if (at is >= 0 and < PaneWidth)
            {
                canvas.Fill(x + 1 + at, height + (wing == 0 ? 1 : up ? 0 : 1), 1, 1, Bat);
            }
        }
    }
}
