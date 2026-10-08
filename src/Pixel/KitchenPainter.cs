using System;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class KitchenPainter
{
    private const int Left = -12;
    private const int Bottom = TavernRows.CounterTop - 1;

    private static readonly Color Wood = PixelPalette.Resolve('b');
    private static readonly Color WoodDark = PixelPalette.Resolve('B');
    private static readonly Color Plate = PixelPalette.Resolve('O');
    private static readonly Color Screw = PixelPalette.Resolve('Z');
    private static readonly Color Apple = PixelPalette.Resolve('r');
    private static readonly Color GreenApple = PixelPalette.Resolve('l');
    private static readonly Color Juice = PixelPalette.Resolve('L');
    private static readonly Color Ember = PixelPalette.Resolve('I');
    private static readonly Color Flame = PixelPalette.Resolve('a');
    private static readonly Color RawCrust = PixelPalette.Resolve('y');
    private static readonly Color GoldenCrust = PixelPalette.Resolve('j');
    private static readonly Color BurntCrust = PixelPalette.Resolve('J');
    private static readonly Color Smoke = PixelPalette.Resolve('S');

    public static void PaintSoup(PixelCanvas canvas, int top, SoupStation soup, float time)
    {
        int x = soup.X + Left;
        bool glowing = (int)(time * 3) % 2 == 0;
        canvas.Draw(glowing ? TavernSprites.BrazierGlow : TavernSprites.Brazier, x + 1, top + 17);
        canvas.Draw((int)(time * 4) % 2 == 0 ? KitchenSprites.Cauldron : KitchenSprites.CauldronBubbling, x, top + 10);
        int sway = soup.Active ? (int)MathF.Round(MathF.Sin(soup.Progress * MathF.PI * 2) * 2) : 0;
        for (int step = 0; step < 4; step++)
        {
            canvas.Fill(x + 6 + sway * step / 3 + step, top + 10 - step, 1, 1, Wood);
        }

        if (soup.InZone)
        {
            Sparkles.Paint(canvas, x + 3, top + 10, time);
        }

        if (soup.Ready is not null)
        {
            canvas.Draw(KitchenSprites.SoupBowl, soup.X, top + Bottom - 1);
        }
    }

    public static void PaintCider(PixelCanvas canvas, int top, CiderStation cider, float time)
    {
        int x = cider.X + Left;
        int depth = cider.Active ? Math.Min(cider.Beats, 3) : 0;
        canvas.Fill(x, top + 8, 1, 12, WoodDark);
        canvas.Fill(x + 10, top + 8, 1, 12, WoodDark);
        canvas.Fill(x, top + 8, 11, 1, Wood);
        canvas.Fill(x + 5, top + 9, 1, 2 + depth, Screw);
        canvas.Fill(x + 2, top + 11 + depth, 7, 1, Plate);
        for (int apple = 0; apple < 3; apple++)
        {
            canvas.Fill(x + 2 + apple * 2, top + 15, 2, 1, apple % 2 == 0 ? Apple : GreenApple);
        }

        canvas.Fill(x + 1, top + 16, 9, 3, Plate);
        canvas.Fill(x + 1, top + 16, 9, 1, Wood);
        canvas.Fill(x, top + 19, 11, 1, WoodDark);
        if (cider.Active && depth > 0)
        {
            canvas.Fill(x + 10, top + 17 + (int)(time * 6) % 2, 1, 1, Juice);
        }

        if (cider.InZone)
        {
            Sparkles.Paint(canvas, x + 3, top + 11, time);
        }

        if (cider.Ready is not null)
        {
            canvas.Draw(KitchenSprites.CiderGlass, cider.X, top + Bottom - 4);
        }
    }

    public static void PaintPie(PixelCanvas canvas, int top, PieStation pie, float time)
    {
        int x = pie.X + Left;
        int y = top + 11;
        canvas.Draw(KitchenSprites.Oven, x, y);
        canvas.Fill(x + 2, y + 6, 8, 1, (int)(time * 5) % 2 == 0 ? Ember : Flame);
        if (pie.Baking)
        {
            Color crust = pie.Progress < pie.ZoneStart ? RawCrust : pie.Progress <= pie.ZoneEnd ? GoldenCrust : BurntCrust;
            canvas.Fill(x + 3, y + 5, 6, 1, crust);
            canvas.Fill(x + 4, y + 4, 4, 1, crust.Lightened(0.15f));
            if (pie.Progress > pie.ZoneEnd)
            {
                float age = time % 1f;
                canvas.Fill(x + 5 + (int)(MathF.Sin(time * 4) * 1.5f), y - 1 - (int)(age * 5), 2, 1, Smoke with { A = 1 - age });
            }
        }

        if (pie.InZone)
        {
            Sparkles.Paint(canvas, x + 4, y + 2, time);
        }

        if (pie.Ready is not null)
        {
            canvas.Draw(KitchenSprites.PieDish, pie.X + 1, top + Bottom - 2);
        }
    }
}
