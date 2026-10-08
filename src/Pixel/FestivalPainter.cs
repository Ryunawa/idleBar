using Godot;

namespace IdleBar.Pixel;

public static class FestivalPainter
{
    private const int CornerMargin = 30;
    private const int Floor = TavernRows.CounterTop;
    private const int BaubleGap = 9;

    private static readonly Color Garland = PixelPalette.Resolve('V');
    private static readonly Color[] Baubles = [PixelPalette.Resolve('r'), PixelPalette.Resolve('g'), PixelPalette.Resolve('q')];

    public static void PaintWall(PixelCanvas canvas, int top, DecorPlan plan, Outdoors outdoors)
    {
        if (outdoors.Festival == Festival.Christmas)
        {
            for (int x = 0; x < canvas.Width; x++)
            {
                canvas.Fill(x, top + 2 + x / 3 % 2, 1, 1, Garland);
                if (x % BaubleGap == 4)
                {
                    canvas.Fill(x, top + 4, 1, 1, Baubles[x / BaubleGap % Baubles.Length]);
                }
            }
        }

        if (outdoors.Festival == Festival.Halloween)
        {
            canvas.Draw(FestivalSprites.Cobweb, 0, top + 2);
            foreach (int post in plan.Posts)
            {
                canvas.Draw(FestivalSprites.Cobweb, post + 2, top + 2);
                canvas.Draw(FestivalSprites.Cobweb.Mirrored, post - 6, top + 2);
            }
        }
    }

    public static void PaintCorner(PixelCanvas canvas, int top, Outdoors outdoors, float time)
    {
        int x = canvas.Width - CornerMargin;
        switch (outdoors.Festival, outdoors.Season)
        {
            case (Festival.Christmas, _):
                Stand(canvas, top, FestivalSprites.Fir, x);
                break;
            case (Festival.Halloween, _):
                Stand(canvas, top, (int)(time * 5) % 4 == 0 ? FestivalSprites.Pumpkin : FestivalSprites.Lantern, x);
                Stand(canvas, top, FestivalSprites.Lantern, x + 6);
                Stand(canvas, top, FestivalSprites.Pumpkin, x + 3, 4);
                break;
            case (_, Season.Autumn):
                Stand(canvas, top, FestivalSprites.Pumpkin, x);
                Stand(canvas, top, FestivalSprites.Pumpkin, x + 6);
                break;
            case (_, Season.Winter):
                Stand(canvas, top, FestivalSprites.Firewood, x);
                break;
            case (_, Season.Spring):
                Stand(canvas, top, FestivalSprites.Flowers, x);
                break;
            default:
                Stand(canvas, top, FestivalSprites.Sunflower, x);
                break;
        }
    }

    public static void PaintCounter(PixelCanvas canvas, int top, DecorPlan plan, Outdoors outdoors, float time)
    {
        if (outdoors.Festival != Festival.Halloween)
        {
            return;
        }

        foreach (PlacedDecor piece in plan.Counter)
        {
            if (piece.Kind == DecorKind.Candle)
            {
                bool flicker = (int)(time * 6 + piece.X) % 5 == 0;
                canvas.Draw(flicker ? FestivalSprites.Pumpkin : FestivalSprites.Lantern, piece.X - 1, top + Floor - 1 - FestivalSprites.Lantern.Height);
            }
        }
    }

    private static void Stand(PixelCanvas canvas, int top, PixelSprite sprite, int x, int lift = 0) =>
        canvas.Draw(sprite, x, top + Floor - sprite.Height - lift);
}
