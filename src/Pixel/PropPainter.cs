using System;
using Godot;

namespace IdleBar.Pixel;

public static class PropPainter
{
    private const int Floor = TavernRows.CounterTop;
    private const int CounterBottom = TavernRows.CounterTop - 1;
    private const float SnoreSeconds = 3f;

    private static readonly Color Stone = PixelPalette.Resolve('s');
    private static readonly Color StoneDark = PixelPalette.Resolve('S');
    private static readonly Color Mantel = PixelPalette.Resolve('b');
    private static readonly Color MantelShade = PixelPalette.Resolve('O');
    private static readonly Color Hearth = PixelPalette.Resolve('X');
    private static readonly Color Log = PixelPalette.Resolve('B');
    private static readonly Color Ember = PixelPalette.Resolve('I');
    private static readonly Color Flame = PixelPalette.Resolve('a');
    private static readonly Color FlameTip = PixelPalette.Resolve('L');
    private static readonly Color Fur = PixelPalette.Resolve('a');
    private static readonly Color Snore = PixelPalette.Resolve('h');

    public static void PaintFloor(PixelCanvas canvas, int top, DecorPlan plan, float time)
    {
        foreach (PlacedDecor piece in plan.Floor)
        {
            switch (piece.Kind)
            {
                case DecorKind.Fireplace:
                    PaintFireplace(canvas, top, piece.X, time);
                    break;
                case DecorKind.Plant:
                    canvas.Draw(PropSprites.Plant, piece.X, top + Floor - PropSprites.Plant.Height);
                    break;
                default:
                    PaintBarrels(canvas, top, piece, time);
                    break;
            }
        }
    }

    public static void PaintCounter(PixelCanvas canvas, int top, DecorPlan plan, float time)
    {
        int bottom = top + CounterBottom;
        foreach (PlacedDecor piece in plan.Counter)
        {
            switch (piece.Kind)
            {
                case DecorKind.Mug:
                    DrinkPainter.Mug(canvas, piece.X, bottom, 0.2f);
                    break;
                case DecorKind.Candle:
                    canvas.Draw(PropSprites.Candle, piece.X, bottom - PropSprites.Candle.Height + 1);
                    bool tall = (int)(time * 7 + piece.X) % 3 != 0;
                    canvas.Fill(piece.X + 1, bottom - 4, 1, 1, FlameTip);
                    canvas.Fill(piece.X + 1, bottom - 5, 1, tall ? 1 : 0, Flame);
                    DecorPainter.PaintGlow(canvas, piece.X + 1, bottom - 4, 0.035f * DecorPainter.Flicker(time, piece.X));
                    break;
                default:
                    PixelSprite sprite = PropSprites.For(piece.Kind);
                    canvas.Draw(sprite, piece.X, bottom - sprite.Height + 1);
                    break;
            }
        }
    }

    private static void PaintBarrels(PixelCanvas canvas, int top, PlacedDecor piece, float time)
    {
        int y = top + Floor - PropSprites.Barrel.Height;
        canvas.Draw(PropSprites.Barrel, piece.X, y);
        canvas.Draw(PropSprites.Barrel, piece.X + 8, y);
        if (piece.Kind != DecorKind.SleepingCat)
        {
            return;
        }

        int catY = y - PropSprites.Cat.Height;
        canvas.Draw(PropSprites.Cat, piece.X + 7, catY);
        bool flick = (int)(time * 1.5f) % 4 == 0;
        canvas.Fill(piece.X + 15, catY + (flick ? 2 : 3), 1, 1, Fur);
        float snore = time % SnoreSeconds / SnoreSeconds;
        int rise = (int)(snore * 5);
        Color z = Snore with { A = 1 - snore };
        canvas.Fill(piece.X + 6 - rise / 2, catY - 2 - rise, 2, 1, z);
        canvas.Fill(piece.X + 6 - rise / 2, catY - 1 - rise, 1, 1, z);
        canvas.Fill(piece.X + 6 - rise / 2, catY - rise, 2, 1, z);
    }

    private static void PaintFireplace(PixelCanvas canvas, int top, int x, float time)
    {
        int width = DecorSizes.Fireplace;
        int y = top + 6;
        DecorPainter.PaintGlow(canvas, x + width / 2, top + 12, 0.07f * DecorPainter.Flicker(time * 1.7f, x));
        canvas.Fill(x - 1, y, width + 2, 1, Mantel);
        canvas.Fill(x - 1, y + 1, width + 2, 1, MantelShade);
        canvas.Fill(x, y + 2, width, Floor - 8, Stone);
        for (int speck = 0; speck < 18; speck++)
        {
            canvas.Fill(x + speck * 7 % width, y + 2 + speck * 5 % (Floor - 8), 2, 1, StoneDark);
        }

        canvas.Fill(x + 4, y + 3, width - 8, 1, Hearth);
        canvas.Fill(x + 3, y + 4, width - 6, Floor - 10, Hearth);
        canvas.Fill(x - 1, top + Floor - 1, width + 2, 1, StoneDark);
        canvas.Fill(x + 6, top + Floor - 3, width - 12, 2, Log);
        canvas.Fill(x + 6, top + Floor - 3, width - 12, 1, Mantel);
        for (int column = 0; column < width - 10; column++)
        {
            float wave = 0.5f + 0.5f * MathF.Sin(time * 9 + column * 1.7f) * MathF.Cos(time * 5.3f + column * 0.9f);
            int height = 1 + (int)(wave * 4) + (column is > 3 and < 10 ? 2 : 0);
            int ground = top + Floor - 4;
            for (int level = 0; level < height; level++)
            {
                Color color = level < 2 ? Ember : level < height - 1 ? Flame : FlameTip;
                canvas.Fill(x + 5 + column, ground - level, 1, 1, color);
            }
        }

        canvas.Draw(PropSprites.Candle, x + 2, y - PropSprites.Candle.Height);
        canvas.Fill(x + 3, y - 5, 1, 1, FlameTip);
        canvas.Draw(PropSprites.Bowl, x + width - 8, y - 2);
    }
}
