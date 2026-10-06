using System;
using System.Collections.Generic;
using Godot;

namespace IdleBar.Pixel;

public static class WorkshopPainter
{
    private const float StepSeconds = 0.35f;
    private const int ProductsPerRow = 6;

    private static readonly Color Interior = new("1a1410");
    private static readonly Color[] Flames = [new("f0883e"), new("f2c14e"), new("a8473b")];
    private static readonly Color Ember = new("7a2f27");
    private static readonly Color Smoke = new("8b949e");
    private static readonly Color Spark = new("f2c14e");
    private static readonly Color Chip = new("a07850");
    private static readonly Color Bubble = new("7fb24a");
    private static readonly Color Steam = new("c9d1d9");
    private static readonly Color Shuttle = new("f2c14e");

    public static void Paint(PixelCanvas canvas, string craftId, float time, bool busy, IReadOnlyList<string> products)
    {
        int ground = LandscapePainter.GroundTop(canvas);
        int origin = canvas.Width / 8;
        int beat = (int)(time / StepSeconds);
        bool striking = busy && beat % 2 == 1;

        TownPainter.PaintHouses(canvas, canvas.Width * 11 / 20);
        int productsX = craftId switch
        {
            "forgeron" => PaintForge(canvas, origin, ground, busy, striking, beat),
            "charron" => PaintWheelwright(canvas, origin, ground, striking, beat),
            "tisserand" => PaintWeaver(canvas, origin, ground, busy, striking, time),
            "negociant" => CounterPainter.Paint(canvas, origin, ground, busy, beat),
            _ => PaintHerbalist(canvas, origin, ground, busy, striking, beat),
        };
        PaintProducts(canvas, productsX, ground, products);
    }

    private static int PaintForge(PixelCanvas canvas, int x, int ground, bool busy, bool striking, int beat)
    {
        canvas.Draw(WorkshopSprites.OreAndCoal, x, ground - 2);
        int forgeX = x + 15;
        int forgeY = ground - WorkshopSprites.Forge.Height + 1;
        canvas.Draw(WorkshopSprites.Forge, forgeX, forgeY);
        canvas.Fill(forgeX + 3, forgeY + 7, 9, 5, Interior);
        for (int index = 0; index < 8; index++)
        {
            int seed = LandscapePainter.Scatter(beat * 31 + index);
            Color flame = busy ? Flames[seed % Flames.Length] : Ember;
            canvas.Fill(forgeX + 3 + seed % 9, forgeY + 10 + seed / 9 % 2, 1, 1, flame);
        }

        if (busy)
        {
            for (int puff = 0; puff < 3; puff++)
            {
                int rise = (beat + puff * 3) % 9;
                canvas.Fill(forgeX + 11 + LandscapePainter.Scatter(beat + puff) % 2, forgeY - 1 - rise, 2, 1, Smoke with { A = 0.6f - rise * 0.06f });
            }
        }

        int smithX = x + 36;
        canvas.Draw(striking ? WorkerSprites.HammerStruck : WorkerSprites.HammerRaised, smithX, ground - 9);
        canvas.Draw(WorkshopSprites.Anvil, smithX + 6, ground - 3);
        if (striking)
        {
            Sprinkle(canvas, beat, smithX + 7, ground - 8, 6, 4, Spark);
        }

        return x + 52;
    }

    private static int PaintWheelwright(PixelCanvas canvas, int x, int ground, bool striking, int beat)
    {
        canvas.Draw(WorkshopSprites.Logs, x, ground - 3);
        canvas.Draw(WorkshopSprites.Shed, x + 10, ground - WorkshopSprites.Shed.Height + 1);
        canvas.Draw(striking ? WorkerSprites.HammerStruck : WorkerSprites.HammerRaised, x + 30, ground - 9);
        canvas.Draw(WorkshopSprites.WheelStand, x + 36, ground - WorkshopSprites.WheelStand.Height + 1);
        if (striking)
        {
            Sprinkle(canvas, beat, x + 35, ground - 8, 5, 3, Chip);
        }

        return x + 48;
    }

    private static int PaintWeaver(PixelCanvas canvas, int x, int ground, bool busy, bool striking, float time)
    {
        canvas.Draw(WorkshopSprites.WoolBales, x, ground - 3);
        canvas.Draw(WorkshopSprites.Shed, x + 10, ground - WorkshopSprites.Shed.Height + 1);
        canvas.Draw(striking ? WorkerSprites.WeaverPushing : WorkerSprites.WeaverPulling, x + 28, ground - 7);
        int loomX = x + 35;
        int loomY = ground - WorkshopSprites.Loom.Height + 1;
        canvas.Draw(WorkshopSprites.Loom, loomX, loomY);
        if (busy)
        {
            int sweep = (int)(time * 8) % 10;
            canvas.Fill(loomX + 2 + (sweep < 5 ? sweep : 9 - sweep), loomY + 3, 2, 1, Shuttle);
        }

        return x + 50;
    }

    private static int PaintHerbalist(PixelCanvas canvas, int x, int ground, bool busy, bool striking, int beat)
    {
        canvas.Draw(WorkshopSprites.HerbRack, x, ground - WorkshopSprites.HerbRack.Height + 1);
        canvas.Draw(WorkshopSprites.Shed, x + 10, ground - WorkshopSprites.Shed.Height + 1);
        canvas.Draw(striking ? WorkerSprites.StirrerHigh : WorkerSprites.StirrerLow, x + 30, ground - 9);
        int cauldronX = x + 37;
        int cauldronY = ground - WorkshopSprites.Cauldron.Height + 1;
        canvas.Draw(WorkshopSprites.Cauldron, cauldronX, cauldronY);
        if (busy)
        {
            Sprinkle(canvas, beat, cauldronX + 1, cauldronY + 1, 5, 1, Bubble);
            int rise = beat % 4;
            canvas.Fill(cauldronX + 2 + beat % 3, cauldronY - 1 - rise, 1, 1, Steam with { A = 0.7f - rise * 0.15f });
        }

        return x + 50;
    }

    private static void PaintProducts(PixelCanvas canvas, int x, int ground, IReadOnlyList<string> products)
    {
        int column = 0;
        int rowBottom = ground;
        foreach (string goodId in products)
        {
            if (column == ProductsPerRow)
            {
                column = 0;
                rowBottom -= GoodIcons.TallestIcon + 1;
            }

            PixelSprite icon = GoodIcons.For(goodId);
            canvas.Draw(icon, x + column * (GoodIcons.TallestIcon + 1), rowBottom - icon.Height + 1);
            column++;
        }
    }

    private static void Sprinkle(PixelCanvas canvas, int beat, int x, int y, int width, int height, Color color)
    {
        for (int index = 0; index < 4; index++)
        {
            int seed = LandscapePainter.Scatter(beat * 17 + index);
            canvas.Fill(x + seed % width, y + seed / width % height, 1, 1, color);
        }
    }
}
