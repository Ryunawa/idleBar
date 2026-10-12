using System;

namespace IdleBar.Scene;

public static class FarmPainters
{
    public static void Farm(ArtRaster r, int cx, int tier, float time)
    {
        int x0 = cx - 11;
        if (tier >= 2)
        {
            r.Fill(cx + 9, 9, 6, 15, Palette.Silver);
            r.Fill(cx + 14, 9, 1, 15, Palette.Gray);
            r.Fill(cx + 10, 8, 4, 1, Palette.Silver);
            r.Fill(cx + 11, 7, 2, 1, Palette.Gray);
            r.Fill(cx + 9, 13, 6, 1, Palette.Gray);
            r.Fill(cx + 9, 18, 6, 1, Palette.Gray);
        }

        r.Fill(cx - 17, 20, 5, 4, Palette.Gold);
        r.Fill(cx - 17, 20, 5, 1, Palette.Yellow);
        r.Fill(cx - 15, 20, 1, 4, Palette.Peach);
        r.Fill(x0 + 1, 14, 19, 10, Palette.Red);
        for (int x = x0 + 3; x < x0 + 19; x += 3)
        {
            r.Fill(x, 14, 1, 10, Palette.Wine);
        }

        r.Fill(x0 + 1, 14, 1, 10, Palette.Wine);
        r.Fill(x0 + 19, 14, 1, 10, Palette.Wine);
        r.Fill(x0 + 6, 9, 9, 1, Palette.Wood);
        r.Fill(x0 + 4, 10, 13, 1, Palette.Bark);
        r.Fill(x0 + 2, 11, 17, 1, Palette.Bark);
        r.Fill(x0, 12, 21, 1, Palette.Bark);
        r.Fill(x0, 13, 21, 1, Palette.Soil);
        r.Fill(x0 + 9, 10, 3, 2, Palette.Gold);
        r.Fill(x0 + 7, 17, 7, 7, Palette.Wine);
        r.Fill(x0 + 7, 17, 7, 1, Palette.Sand);
        r.Fill(x0 + 7, 17, 1, 7, Palette.Sand);
        r.Fill(x0 + 13, 17, 1, 7, Palette.Sand);
        for (int k = 0; k < 5; k++)
        {
            r.Set(x0 + 8 + k, 18 + k, Palette.Sand);
            r.Set(x0 + 12 - k, 18 + k, Palette.Sand);
        }

        int peck = MathF.Sin(time * 3 + cx) > 0.6f ? 1 : 0;
        int hen = cx - 9;
        r.Fill(hen, 21, 3, 2, Palette.White);
        r.Set(hen - 1, 20 + peck, Palette.White);
        r.Set(hen - 2, 20 + peck, Palette.Gold);
        r.Set(hen - 1, 19 + peck, Palette.Red);
        r.Set(hen + 1, 23, Palette.Gold);
    }

    public static void Mill(ArtRaster r, int cx, int tier, float time)
    {
        for (int y = 9; y < 24; y++)
        {
            int half = 3 + (y - 9) / 5;
            r.Fill(cx - half, y, half * 2 + 1, 1, Palette.Sand);
            r.Set(cx + half, y, Palette.Tan);
            r.Set(cx - half, y, Palette.Peach);
        }

        r.Fill(cx - 1, 20, 3, 4, Palette.Bark);
        r.Fill(cx - 1, 13, 2, 2, Palette.Soil);
        for (int y = 5; y < 9; y++)
        {
            int half = y - 4;
            r.Fill(cx - half, y, half * 2 + 1, 1, tier >= 5 ? Palette.Gold : Palette.Brick);
            r.Set(cx + half, y, tier >= 5 ? Palette.Yellow : Palette.Wine);
        }

        r.Fill(cx + 7, 21, 3, 3, Palette.Sand);
        r.Set(cx + 8, 20, Palette.Sand);
        r.Fill(cx + 10, 22, 2, 2, Palette.Peach);
        if (tier >= 2)
        {
            r.Fill(cx - 12, 20, 3, 4, Palette.Sand);
            r.Fill(cx - 9, 21, 3, 3, Palette.Sand);
            r.Set(cx - 11, 19, Palette.Sand);
        }

        float angle = time * (0.9f + Math.Min(tier, 5) * 0.2f);
        for (int arm = 0; arm < 4; arm++)
        {
            float a = angle + arm * MathF.PI / 2;
            float cos = MathF.Cos(a);
            float sin = MathF.Sin(a);
            for (int radius = 1; radius <= 9; radius++)
            {
                r.Set((int)MathF.Round(cx + cos * radius), (int)MathF.Round(7 + sin * radius), Palette.Bark);
                if (radius >= 3)
                {
                    r.Set((int)MathF.Round(cx + cos * radius - sin * 1.6f), (int)MathF.Round(7 + sin * radius + cos * 1.6f), Palette.White);
                }
            }
        }

        r.Set(cx, 7, Palette.Soil);
    }

    public static void Bakery(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx + 5, 5, 3, 6, Palette.Steel);
        r.Fill(cx + 4, 4, 5, 1, Palette.Slate);
        if (tier >= 2)
        {
            r.Fill(cx - 7, 6, 3, 5, Palette.Steel);
            r.Fill(cx - 8, 5, 5, 1, Palette.Slate);
        }

        r.Fill(cx - 10, 14, 21, 10, Palette.Sand);
        r.Fill(cx - 10, 14, 21, 1, Palette.Bark);
        r.Fill(cx - 10, 14, 1, 10, Palette.Bark);
        r.Fill(cx + 10, 14, 1, 10, Palette.Bark);
        r.Fill(cx, 14, 1, 10, Palette.Bark);
        int[] halves = [4, 6, 8, 10, 12, 12];
        for (int row = 0; row < halves.Length; row++)
        {
            Rgba roof = tier >= 5 ? Palette.Gold : Palette.Brick;
            r.Fill(cx - halves[row], 8 + row, halves[row] * 2 + 1, 1, row == 5 ? Palette.Wine : roof);
        }

        r.Fill(cx - 4, 8, 9, 1, tier >= 5 ? Palette.Yellow : Palette.Rust);
        r.Fill(cx - 8, 17, 6, 4, Palette.Slate);
        r.Fill(cx - 8, 21, 6, 1, Palette.Bark);
        r.Fill(cx - 7, 19, 2, 1, Palette.Peach);
        r.Fill(cx - 4, 19, 2, 1, Palette.Rust);
        r.Fill(cx - 7, 20, 4, 1, Palette.Peach);
        r.Fill(cx + 3, 17, 5, 7, Palette.Bark);
        r.Set(cx + 6, 20, Palette.Gold);
        r.Fill(cx + 2, 15, 7, 2, Palette.Wood);
        r.Fill(cx + 4, 15, 2, 1, Palette.Peach);
        r.Set(cx + 6, 16, Palette.Peach);
    }
}
