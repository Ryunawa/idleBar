using System;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class StationPainter
{
    private const int Bottom = TavernRows.CounterTop - 1;
    private const int BarrelLeft = -10;
    private const int TeapotLeft = -12;
    private const float PuffSeconds = 1.2f;
    private const float HopRate = 10f;

    private static readonly Color Beer = PixelPalette.Resolve('e');
    private static readonly Color Gold = PixelPalette.Resolve('g');
    private static readonly Color Overflow = PixelPalette.Resolve('R');
    private static readonly Color Stewed = PixelPalette.Resolve('J');
    private static readonly Color PaleSteam = PixelPalette.Resolve('h');
    private static readonly Color GoldenSteam = PixelPalette.Resolve('g');
    private static readonly Color DarkSteam = PixelPalette.Resolve('J');

    public static Rect2I Bounds(Station station, int top) => station switch
    {
        TapStation => new Rect2I(station.X + BarrelLeft, top + 9, 18, 12),
        _ => new Rect2I(station.X + TeapotLeft, top + 6, 18, 15),
    };

    public static void Paint(PixelCanvas canvas, int top, Station station, float time)
    {
        switch (station)
        {
            case TapStation tap:
                PaintTap(canvas, top, tap, time);
                GaugePainter.Paint(canvas, top, tap.X + BarrelLeft, tap, Overflow, time);
                break;
            case TeapotStation teapot:
                PaintTeapot(canvas, top, teapot, time);
                GaugePainter.Paint(canvas, top, teapot.X + TeapotLeft, teapot, Stewed, time);
                break;
        }
    }

    private static void PaintTap(PixelCanvas canvas, int top, TapStation tap, float time)
    {
        canvas.Draw(TavernSprites.Barrel, tap.X + BarrelLeft, top + 10);
        int bottom = top + Bottom;
        float level = tap.Ready is not null ? 1f : tap.Level;
        DrinkPainter.Mug(canvas, tap.X, bottom, level);
        if (tap.Pouring)
        {
            int surface = bottom - (int)MathF.Ceiling(Math.Min(level, 1f) * 4);
            canvas.Fill(tap.X + 2, top + 15, 1, Math.Max(1, surface - top - 15), Beer);
        }

        if (tap.InZone)
        {
            canvas.Fill(tap.X, bottom - 4, 1, 4, Gold);
            canvas.Fill(tap.X + 4, bottom - 4, 1, 4, Gold);
            canvas.Fill(tap.X + 5, bottom - 3, 1, 2, Gold);
            Sparkles.Paint(canvas, tap.X, bottom - 4, time);
        }
    }

    private static void PaintTeapot(PixelCanvas canvas, int top, TeapotStation teapot, float time)
    {
        int left = teapot.X + TeapotLeft;
        bool glowing = (int)(time * 3) % 2 == 0;
        canvas.Draw(glowing ? TavernSprites.BrazierGlow : TavernSprites.Brazier, left + 1, top + 17);
        if (teapot.InZone)
        {
            int y = top + 10 - (int)(time * HopRate) % 2;
            Color halo = Colors.White with { A = 0.6f + 0.4f * MathF.Sin(time * 10) };
            canvas.Draw(TavernSprites.TeapotHalo, left - 1, y, halo);
            canvas.Draw(TavernSprites.TeapotHalo, left + 1, y, halo);
            canvas.Draw(TavernSprites.TeapotHalo, left, y - 1, halo);
            canvas.Draw(TavernSprites.TeapotHalo, left, y + 1, halo);
            canvas.Draw(TavernSprites.Teapot, left, y);
            Sparkles.Paint(canvas, left + 2, top + 10, time);
        }
        else
        {
            canvas.Draw(TavernSprites.Teapot, left, top + 10);
        }

        if (teapot.Steeping)
        {
            PaintSteam(canvas, left + 4, top + 9, teapot, time);
        }

        if (teapot.Ready is PreparedDrink tea)
        {
            DrinkPainter.Cup(canvas, teapot.X, top + Bottom, tea.Perfect);
        }
    }

    private static void PaintSteam(PixelCanvas canvas, int x, int y, TeapotStation teapot, float time)
    {
        Color color = teapot.Steeped < TeapotStation.GoldenFrom ? PaleSteam : teapot.Golden ? GoldenSteam : DarkSteam;
        for (int puff = 0; puff < 3; puff++)
        {
            float age = (time + puff * PuffSeconds / 3) % PuffSeconds / PuffSeconds;
            int rise = (int)(age * 7);
            int drift = (int)(MathF.Sin((time + puff) * 3) * 1.5f);
            int size = rise > 3 ? 2 : 1;
            canvas.Fill(x + drift + puff % 2, y - rise - size + 1, size, size, color with { A = 1 - age * 0.6f });
        }
    }
}
