using System;
using Godot;

namespace IdleBar.Pixel;

public static class DecorPainter
{
    private const int HangRow = 2;
    private const int ShelfRow = 9;

    private static readonly Color Ceiling = PixelPalette.Resolve('d');
    private static readonly Color Timber = PixelPalette.Resolve('B');
    private static readonly Color TimberLight = PixelPalette.Resolve('O');
    private static readonly Color Plank = PixelPalette.Resolve('b');
    private static readonly Color Gold = PixelPalette.Resolve('g');
    private static readonly Color Glow = PixelPalette.Resolve('z');

    public static void PaintCeiling(PixelCanvas canvas, int top)
    {
        canvas.Fill(0, 0, canvas.Width, top, Ceiling);
        if (top >= 5)
        {
            canvas.Fill(0, top - 4, canvas.Width, 2, Timber);
            canvas.Fill(0, top - 3, canvas.Width, 1, TimberLight);
        }
    }

    public static void PaintPosts(PixelCanvas canvas, int top, DecorPlan plan)
    {
        int bottom = top + TavernRows.Wainscot;
        foreach (int x in plan.Posts)
        {
            canvas.Fill(x, 0, 2, bottom, Timber);
            canvas.Fill(x, 0, 1, bottom, TimberLight);
            for (int step = 0; step < 4; step++)
            {
                canvas.Fill(x - 1 - step, top + 2 + step, 1, 1, Timber);
                canvas.Fill(x + 2 + step, top + 2 + step, 1, 1, Timber);
            }
        }
    }

    public static void PaintWall(PixelCanvas canvas, int top, DecorPlan plan, SkyLight sky, float time)
    {
        foreach (PlacedDecor piece in plan.Wall)
        {
            switch (piece.Kind)
            {
                case DecorKind.Window:
                    WindowPainter.Paint(canvas, top, piece.X, sky, time);
                    break;
                case DecorKind.Shelf:
                    PaintShelf(canvas, top, piece.X, DecorSizes.Shelf);
                    break;
                case DecorKind.Clock:
                    canvas.Draw(DecorSprites.Clock, piece.X, top + 3);
                    PaintPendulum(canvas, piece.X + 3, top + 10, time);
                    break;
                default:
                    canvas.Draw(DecorSprites.For(piece.Kind), piece.X, top + Row(piece.Kind));
                    break;
            }
        }
    }

    public static void PaintHanging(PixelCanvas canvas, int top, DecorPlan plan, SkyLight sky, float time)
    {
        foreach (PlacedDecor piece in plan.Hanging)
        {
            canvas.Draw(PropSprites.For(piece.Kind), piece.X, top + HangRow);
            if (piece.Kind == DecorKind.Lantern)
            {
                PaintGlow(canvas, piece.X + 2, top + 4, (0.05f + 0.07f * (1 - sky.Daylight)) * Flicker(time, piece.X));
            }
        }
    }

    public static void PaintShelf(PixelCanvas canvas, int top, int x, int width)
    {
        int board = top + ShelfRow;
        canvas.Fill(x, board, width, 1, Plank);
        canvas.Fill(x + 2, board + 1, 1, 2, Timber);
        canvas.Fill(x + width - 3, board + 1, 1, 2, Timber);
        for (int slot = 0, at = x + 2; at + 3 <= x + width - 1; slot++, at += 5)
        {
            int pick = (slot * 7 + x * 13 + slot * x) % (TavernSprites.Bottles.Length + 3);
            if (pick < TavernSprites.Bottles.Length)
            {
                PixelSprite bottle = TavernSprites.Bottles[pick];
                canvas.Draw(bottle, at, board - bottle.Height);
            }
        }
    }

    public static void PaintGlow(PixelCanvas canvas, int x, int y, float strength)
    {
        foreach (int radius in new[] { 9, 6, 3 })
        {
            canvas.Fill(x - radius, y - radius / 2, radius * 2 + 1, radius + 1, Glow with { A = strength });
        }
    }

    public static float Flicker(float time, int seed) => 0.85f + 0.15f * MathF.Sin(time * 6 + seed);

    private static void PaintPendulum(PixelCanvas canvas, int x, int y, float time)
    {
        int swing = (int)MathF.Round(MathF.Sin(time * MathF.PI));
        canvas.Fill(x, y, 1, 1, Gold);
        canvas.Fill(x + swing, y + 1, 1, 1, Gold);
    }

    private static int Row(DecorKind kind) => kind switch
    {
        DecorKind.Banner or DecorKind.BlueBanner => HangRow,
        DecorKind.Shield or DecorKind.Board => 4,
        DecorKind.Dartboard => 5,
        _ => 3,
    };
}
