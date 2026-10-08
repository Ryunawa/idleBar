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
        switch (drink.Drink)
        {
            case Drink.Beer:
                Mug(canvas, x, bottom, 1f);
                break;
            case Drink.Tea:
                Cup(canvas, x, bottom, drink.Perfect);
                break;
            default:
                PixelSprite sprite = Served(drink.Drink);
                canvas.Draw(sprite, x, bottom - sprite.Height + 1);
                break;
        }
    }

    public static PixelSprite Icon(Drink drink) => drink switch
    {
        Drink.Beer => TavernSprites.BeerIcon,
        Drink.Tea => TavernSprites.TeaIcon,
        Drink.Soup => KitchenSprites.SoupIcon,
        Drink.Cider => KitchenSprites.CiderIcon,
        _ => KitchenSprites.PieIcon,
    };

    private static PixelSprite Served(Drink drink) => drink switch
    {
        Drink.Soup => KitchenSprites.SoupBowl,
        Drink.Cider => KitchenSprites.CiderGlass,
        _ => KitchenSprites.PieDish,
    };

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
