namespace IdleBar.Scene;

public static class CivicPainters
{
    public static void Barracks(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx - 12, 12, 25, 12, Palette.Wood);
        for (int x = cx - 12; x <= cx + 12; x += 4)
        {
            r.Fill(x, 12, 1, 12, Palette.Bark);
        }

        Rgba roof = tier >= 5 ? Palette.Gold : Palette.Pine;
        for (int row = 0; row < 5; row++)
        {
            int half = 9 + row;
            r.Fill(cx - half, 6 + row, half * 2 + 1, 1, row == 4 ? Palette.Deep : roof);
        }

        r.Fill(cx - 2, 17, 5, 7, Palette.Ink);
        r.Fill(cx - 9, 15, 3, 3, Palette.Red);
        r.Set(cx - 8, 16, Palette.Gold);
        r.Fill(cx + 7, 15, 3, 3, Palette.Blue);
        r.Set(cx + 8, 16, Palette.Silver);
        r.Fill(cx, 1, 1, 5, Palette.Bark);
        r.Fill(cx + 1, 1, 4, 2, Palette.Red);
        if (tier >= 2)
        {
            r.Fill(cx + 14, 13, 1, 11, Palette.Bark);
            r.Fill(cx + 16, 13, 1, 11, Palette.Bark);
            r.Set(cx + 14, 12, Palette.Silver);
            r.Set(cx + 16, 12, Palette.Silver);
            r.Fill(cx + 13, 18, 5, 1, Palette.Bark);
        }
    }

    public static void Chapel(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx - 9, 13, 19, 11, Palette.Silver);
        r.Fill(cx + 8, 13, 2, 11, Palette.Gray);
        Rgba roof = tier >= 5 ? Palette.Gold : Palette.NavyBlue;
        for (int row = 0; row < 6; row++)
        {
            int half = 5 + row;
            r.Fill(cx - half, 7 + row, half * 2 + 1, 1, roof);
        }

        r.Fill(cx - 1, 3, 3, 4, Palette.Gray);
        r.Fill(cx, 0, 1, 3, Palette.Gold);
        r.Fill(cx - 1, 1, 3, 1, Palette.Gold);
        r.Fill(cx - 1, 14, 3, 2, Palette.Blue);
        r.Fill(cx - 2, 18, 5, 6, Palette.Wood);
        r.Fill(cx - 1, 17, 3, 1, Palette.Wood);
        r.Set(cx, 21, Palette.Gold);
        if (tier >= 2)
        {
            r.Fill(cx - 7, 16, 2, 3, Palette.Blue);
            r.Fill(cx + 5, 16, 2, 3, Palette.Blue);
            r.Fill(cx - 13, 21, 1, 3, Palette.Gray);
            r.Fill(cx - 14, 22, 3, 1, Palette.Gray);
        }
    }

    public static void Tavern(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx + 5, 1, 3, 5, Palette.Brick);
        r.Fill(cx - 11, 15, 22, 9, Palette.Sand);
        r.Fill(cx - 12, 9, 24, 6, Palette.Peach);
        r.Fill(cx - 12, 14, 24, 1, Palette.Bark);
        r.Fill(cx - 12, 9, 1, 6, Palette.Bark);
        r.Fill(cx + 11, 9, 1, 6, Palette.Bark);
        r.Fill(cx - 11, 15, 1, 9, Palette.Bark);
        r.Fill(cx + 10, 15, 1, 9, Palette.Bark);
        Rgba roof = tier >= 5 ? Palette.Gold : Palette.Brick;
        for (int row = 0; row < 5; row++)
        {
            int half = 8 + row;
            r.Fill(cx - half, 4 + row, half * 2 + 1, 1, row == 4 ? Palette.Wine : roof);
        }

        r.Fill(cx - 6, 10, 3, 3, Palette.Slate);
        r.Fill(cx + 4, 10, 3, 3, Palette.Slate);
        r.Fill(cx - 8, 17, 4, 3, Palette.Slate);
        r.Fill(cx + 5, 17, 4, 3, Palette.Slate);
        r.Fill(cx - 2, 18, 4, 6, Palette.Bark);
        r.Set(cx + 1, 21, Palette.Gold);
        r.Fill(cx + 12, 13, 4, 1, Palette.Bark);
        r.Fill(cx + 13, 14, 4, 4, Palette.Wood);
        r.Fill(cx + 14, 15, 2, 2, Palette.Gold);
        if (tier >= 2)
        {
            r.Fill(cx - 16, 20, 4, 4, Palette.Wood);
            r.Fill(cx - 16, 21, 4, 1, Palette.Bark);
            r.Fill(cx - 15, 17, 3, 3, Palette.Wood);
            r.Fill(cx - 15, 18, 3, 1, Palette.Bark);
        }
    }

    public static void Workshop(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx - 12, 14, 24, 10, Palette.Wood);
        r.Fill(cx - 13, 12, 27, 2, tier >= 5 ? Palette.Gold : Palette.Slate);
        r.Fill(cx - 11, 10, 23, 2, tier >= 5 ? Palette.Yellow : Palette.Steel);
        r.Fill(cx - 8, 16, 11, 8, Palette.Ink);
        r.Fill(cx - 6, 20, 7, 1, Palette.Bark);
        r.Fill(cx - 5, 21, 1, 3, Palette.Bark);
        r.Fill(cx, 21, 1, 3, Palette.Bark);
        r.Fill(cx - 4, 19, 2, 1, Palette.Silver);
        r.Fill(cx + 6, 16, 5, 3, Palette.Gray);
        r.Fill(cx + 7, 15, 3, 5, Palette.Gray);
        r.Set(cx + 8, 17, Palette.Wood);
        r.Fill(cx + 12, 21, 5, 3, Palette.Bark);
        r.Set(cx + 13, 22, Palette.Peach);
        r.Set(cx + 15, 22, Palette.Peach);
        r.Set(cx + 14, 21, Palette.Peach);
        if (tier >= 2)
        {
            r.Fill(cx - 16, 6, 1, 18, Palette.Bark);
            r.Fill(cx - 16, 6, 7, 1, Palette.Bark);
            r.Fill(cx - 10, 7, 1, 4, Palette.Steel);
            r.Fill(cx - 11, 11, 3, 1, Palette.Steel);
        }
    }

    public static void Belfry(ArtRaster r, int cx, int tier, float time)
    {
        if (tier >= 2)
        {
            r.Fill(cx - 11, 17, 7, 7, Palette.Peach);
            r.Fill(cx - 12, 16, 9, 1, Palette.Brick);
            r.Fill(cx + 5, 17, 7, 7, Palette.Peach);
            r.Fill(cx + 4, 16, 9, 1, Palette.Brick);
        }

        r.Fill(cx - 4, 7, 9, 17, Palette.Sand);
        r.Fill(cx + 3, 7, 2, 17, Palette.Tan);
        r.Fill(cx - 4, 3, 9, 4, Palette.Ink);
        r.Fill(cx - 4, 3, 1, 4, Palette.Sand);
        r.Fill(cx + 4, 3, 1, 4, Palette.Sand);
        r.Fill(cx - 1, 4, 3, 2, Palette.Gold);
        Rgba roof = tier >= 5 ? Palette.Gold : Palette.NavyBlue;
        r.Fill(cx, 0, 1, 1, roof);
        r.Fill(cx - 2, 1, 5, 1, roof);
        r.Fill(cx - 5, 2, 11, 1, roof);
        r.Fill(cx - 2, 9, 5, 5, Palette.White);
        r.Set(cx, 11, Palette.Ink);
        int[][] hands = [[0, -2], [2, 0], [0, 2], [-2, 0]];
        int[] hand = hands[(int)(time / 2) % hands.Length];
        r.Set(cx + hand[0] / 2, 11 + hand[1] / 2, Palette.Ink);
        r.Fill(cx - 1, 20, 3, 4, Palette.Bark);
    }
}
