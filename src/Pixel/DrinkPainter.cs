using System;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class DrinkPainter
{
    public const int MugWidth = 6;
    private const int MugRows = 4;

    private static readonly Color Pewter = PixelPalette.Resolve('n');
    private static readonly Color PewterDark = PixelPalette.Resolve('N');
    private static readonly Color Beer = PixelPalette.Resolve('e');
    private static readonly Color Foam = PixelPalette.Resolve('f');
    private static readonly Color China = PixelPalette.Resolve('q');
    private static readonly Color Saucer = PixelPalette.Resolve('Q');
    private static readonly Color GoldenTea = PixelPalette.Resolve('j');
    private static readonly Color PlainTea = PixelPalette.Resolve('J');

    public static void Paint(PixelCanvas canvas, PreparedDrink drink, int x, int bottom)
    {
        if (drink.Drink == Drink.Beer)
        {
            Mug(canvas, x, bottom, 1f);
            return;
        }

        Cup(canvas, x, bottom, drink.Perfect);
    }

    public static void Mug(PixelCanvas canvas, int x, int bottom, float level)
    {
        int top = bottom - MugRows;
        canvas.Fill(x, top, 1, MugRows, Pewter);
        canvas.Fill(x + 4, top, 1, MugRows, Pewter);
        canvas.Fill(x, bottom, 5, 1, PewterDark);
        canvas.Fill(x + 5, top + 1, 1, 2, PewterDark);
        int rows = Math.Clamp((int)MathF.Ceiling(Math.Min(level, 1f) * MugRows), 0, MugRows);
        if (rows > 0)
        {
            canvas.Fill(x + 1, bottom - rows, 3, rows, Beer);
        }

        if (rows >= 2)
        {
            canvas.Fill(x + 1, bottom - rows, 3, 1, Foam);
        }

        if (level > 1f)
        {
            int drip = Math.Min(MugRows, (int)MathF.Ceiling((level - 1f) * 12));
            canvas.Fill(x, top - 1, 5, 1, Foam);
            canvas.Fill(x - 1, top, 1, drip, Foam);
            canvas.Fill(x + 5, top, 1, Math.Max(0, drip - 1), Foam);
        }
    }

    public static void Cup(PixelCanvas canvas, int x, int bottom, bool golden)
    {
        canvas.Fill(x, bottom - 2, 4, 1, China);
        canvas.Fill(x + 1, bottom - 2, 2, 1, golden ? GoldenTea : PlainTea);
        canvas.Fill(x, bottom - 1, 4, 1, China);
        canvas.Fill(x + 4, bottom - 2, 1, 1, China);
        canvas.Fill(x - 1, bottom, 6, 1, Saucer);
    }
}
