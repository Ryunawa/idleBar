namespace IdleBar.Scene;

public static class TownPainters
{
    public static void Forge(ArtRaster r, int cx, int tier)
    {
        r.Fill(cx + 3, 3, 4, 10, Palette.Steel);
        r.Fill(cx + 6, 3, 1, 10, Palette.Slate);
        r.Fill(cx + 2, 2, 6, 1, Palette.Slate);
        r.Fill(cx - 12, 13, 22, 11, Palette.Gray);
        for (int y = 15; y < 24; y += 3)
        {
            r.Fill(cx - 12, y, 22, 1, Palette.Steel);
        }

        for (int row = 0; row < 3; row++)
        {
            for (int x = cx - 12 + (row % 2 == 1 ? 3 : 0); x < cx + 10; x += 6)
            {
                r.Fill(x, 16 + row * 3, 1, 2, Palette.Steel);
            }
        }

        r.Fill(cx - 13, 12, 24, 2, tier >= 5 ? Palette.Gold : Palette.Slate);
        r.Fill(cx - 9, 16, 8, 8, Palette.Ink);
        r.Fill(cx + 11, 20, 6, 1, Palette.Navy);
        r.Fill(cx + 13, 21, 2, 2, Palette.Navy);
        r.Fill(cx + 12, 23, 4, 1, Palette.Navy);
        if (tier >= 2)
        {
            r.Fill(cx - 17, 15, 1, 9, Palette.Bark);
            r.Fill(cx - 17, 15, 4, 1, Palette.Bark);
            r.Fill(cx - 15, 16, 1, 5, Palette.Silver);
            r.Fill(cx - 14, 16, 1, 6, Palette.Silver);
            r.Set(cx - 15, 21, Palette.Gold);
            r.Set(cx - 14, 22, Palette.Gold);
        }
    }

    public static void Market(ArtRaster r, int cx, int tier)
    {
        Rgba[] left = [Palette.Red, Palette.Green, Palette.Orange, Palette.Red, Palette.Yellow, Palette.Green];
        Rgba[] right = [Palette.Orange, Palette.Orange, Palette.Green, Palette.Rose, Palette.Yellow, Palette.Red];
        Stall(r, cx - 17, tier >= 5 ? Palette.Gold : Palette.Red, Palette.Wine, left);
        Stall(r, cx + 2, tier >= 5 ? Palette.Gold : Palette.Blue, Palette.NavyBlue, right);
        if (tier >= 2)
        {
            for (int x = cx - 16; x < cx + 16; x += 4)
            {
                r.Set(x, 9 - (x / 4 % 2), Palette.Yellow);
            }
        }
    }

    private static void Stall(ArtRaster r, int x, Rgba main, Rgba dark, Rgba[] goods)
    {
        r.Fill(x, 13, 1, 11, Palette.Bark);
        r.Fill(x + 14, 13, 1, 11, Palette.Bark);
        r.Fill(x, 10, 15, 1, dark);
        for (int k = 0; k < 15; k++)
        {
            r.Fill(x + k, 11, 1, 3, k / 2 % 2 == 1 ? main : Palette.White);
        }

        for (int k = 0; k < 15; k += 2)
        {
            r.Set(x + k, 14, main);
        }

        r.Fill(x + 6, 15, 3, 2, Palette.Skin);
        r.Fill(x + 6, 15, 3, 1, Palette.Soil);
        r.Fill(x + 5, 17, 5, 2, dark);
        r.Fill(x + 1, 19, 13, 2, Palette.Wood);
        r.Fill(x + 1, 21, 13, 3, Palette.Bark);
        for (int k = 0; k < goods.Length; k++)
        {
            r.Set(x + 2 + k * 2, 18, goods[k]);
        }
    }
}
