namespace IdleBar.Scene;

public static class ArcanePainters
{
    public static void Tower(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx - 4, 6, 9, 18, Palette.Steel);
        r.Fill(cx + 3, 6, 2, 18, Palette.Slate);
        for (int y = 8; y < 24; y += 3)
        {
            r.Set(cx - 2 + y % 2, y, Palette.Slate);
            r.Set(cx + 1 + y % 2, y + 1, Palette.Slate);
        }

        Rgba roof = tier >= 5 ? Palette.Gold : Palette.Plum;
        Rgba light = tier >= 5 ? Palette.Yellow : Palette.Pink;
        for (int row = 0; row < 6; row++)
        {
            r.Fill(cx - row, row, row * 2 + 1, 1, roof);
            if (row > 0)
            {
                r.Fill(cx - row, row, row, 1, light);
            }
        }

        r.Fill(cx - 6, 6, 13, 1, roof);
        r.Fill(cx - 1, 9, 3, 3, Palette.Ink);
        r.Fill(cx - 1, 15, 3, 2, Palette.Ink);
        r.Fill(cx - 1, 20, 3, 4, Palette.Bark);
        r.Set(cx, 19, Palette.Bark);
        if (tier >= 2)
        {
            r.Fill(cx - 6, 13, 13, 1, Palette.Slate);
            for (int x = cx - 6; x <= cx + 6; x += 2)
            {
                r.Set(x, 12, Palette.Slate);
            }
        }
    }

    public static void Portal(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx - 11, 8, 3, 16, Palette.Gray);
        r.Fill(cx + 9, 8, 3, 16, Palette.Gray);
        r.Fill(cx - 9, 8, 1, 16, Palette.Steel);
        r.Fill(cx + 11, 8, 1, 16, Palette.Steel);
        int[][] arch = [[5, 6], [6, 8], [7, 10], [8, 11]];
        foreach (int[] row in arch)
        {
            r.Fill(cx - row[1], row[0], row[1] * 2 + 1, 1, tier >= 5 ? Palette.Gold : Palette.Gray);
        }

        r.Fill(cx - 6, 5, 13, 1, tier >= 5 ? Palette.Yellow : Palette.Silver);
        r.Fill(cx - 7, 8, 15, 1, Palette.Ink);
        r.Fill(cx - 8, 9, 17, 15, Palette.Ink);
        r.Set(cx, 5, Palette.Cyan);
        r.Fill(cx - 12, 23, 25, 1, Palette.Steel);
        if (tier >= 2)
        {
            r.Fill(cx - 15, 20, 2, 4, Palette.Steel);
            r.Fill(cx + 14, 20, 2, 4, Palette.Steel);
            r.Set(cx - 15, 21, Palette.Cyan);
            r.Set(cx + 15, 21, Palette.Cyan);
        }
    }

    public static void Plot(ArtRaster r, int cx)
    {
        r.Fill(cx - 13, 22, 27, 2, Palette.Bark);
        r.Fill(cx - 12, 21, 25, 1, Palette.Wood);
        r.Fill(cx - 14, 18, 1, 6, Palette.Wood);
        r.Fill(cx + 14, 18, 1, 6, Palette.Wood);
        for (int x = cx - 13; x < cx + 14; x += 2)
        {
            r.Set(x, 19, Palette.Sand);
        }

        r.Fill(cx - 1, 15, 2, 7, Palette.Bark);
        r.Fill(cx - 15, 8, 31, 8, Palette.Bark);
        r.Fill(cx - 14, 9, 29, 6, Palette.Wood);
        ArtFont.Draw(r, "À BÂTIR", cx - ArtFont.Width("À BÂTIR") / 2, 10, Palette.Soil);
    }
}
