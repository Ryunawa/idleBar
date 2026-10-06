using Godot;

namespace IdleBar.Pixel;

public static class CounterPainter
{
    private const int MerchantOffset = 8;
    private const int CoinOffset = 16;

    private static readonly Color Glint = new("f7d774");

    public static int Paint(PixelCanvas canvas, int x, int ground, bool busy, int beat)
    {
        canvas.Draw(CounterSprites.Sacks, x, ground - CounterSprites.Sacks.Height + 1);
        int stallX = x + 10;
        int counterTop = ground - CounterSprites.CounterFront.Height + 1;
        canvas.Draw(CounterSprites.StallTop, stallX, counterTop - CounterSprites.StallTop.Height);

        PixelSprite merchant = busy && beat % 2 == 1 ? WorkerSprites.MerchantCounting : WorkerSprites.MerchantWaiting;
        canvas.Draw(merchant, stallX + MerchantOffset, ground - merchant.Height);
        canvas.Draw(CounterSprites.CounterFront, stallX, counterTop);
        canvas.Draw(CounterSprites.Scales, stallX + 2, counterTop - CounterSprites.Scales.Height);
        canvas.Draw(CounterSprites.Coins, stallX + CoinOffset, counterTop - CounterSprites.Coins.Height);

        if (busy)
        {
            int seed = LandscapePainter.Scatter(beat);
            canvas.Fill(stallX + CoinOffset + seed % 3, counterTop - CounterSprites.Coins.Height - 1 - seed / 3 % 2, 1, 1, Glint);
        }

        return stallX + CounterSprites.CounterFront.Width + 2;
    }
}
