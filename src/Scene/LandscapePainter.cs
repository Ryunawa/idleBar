using System;

namespace IdleBar.Scene;

public static class LandscapePainter
{
    private const int MinGap = 26;
    private const int MaxGap = 64;
    private const int FrontMargin = 26;

    public static void Paint(ArtRaster r, FrontierLayout layout)
    {
        if (layout.Narrow)
        {
            return;
        }

        int start = layout.Crier ? layout.CrierX + 16 : layout.PalisadeX + 14;
        int end = layout.FrontX - FrontMargin;
        int x = start;
        int item = 0;
        while (x < end)
        {
            int kind = (int)(Noise.Unit(item, 51) * 7);
            int width = Width(kind);
            if (x + width > end)
            {
                break;
            }

            Draw(r, kind, x);
            x += width + MinGap + (int)(Noise.Unit(item, 52) * (MaxGap - MinGap));
            item++;
        }

        if (end - start > 40)
        {
            Signpost(r, end - 2);
        }
    }

    private static int Width(int kind) => kind switch
    {
        0 => 13,
        1 => 7,
        2 => 12,
        3 => 16,
        4 => 4,
        5 => 7,
        _ => 9,
    };

    private static void Draw(ArtRaster r, int kind, int x)
    {
        switch (kind)
        {
            case 0:
                r.Fill(x + 5, 17, 3, 7, Palette.Bark);
                r.Fill(x + 2, 9, 9, 8, Palette.Leaf);
                r.Fill(x, 11, 13, 5, Palette.Leaf);
                r.Fill(x + 3, 8, 7, 1, Palette.Leaf);
                r.Fill(x + 3, 10, 3, 2, Palette.Green);
                r.Fill(x + 1, 15, 11, 1, Palette.Pine);
                break;
            case 1:
                r.Fill(x, 20, 7, 4, Palette.Gold);
                r.Fill(x + 1, 19, 5, 1, Palette.Gold);
                r.Fill(x, 20, 7, 1, Palette.Yellow);
                r.Fill(x + 3, 21, 1, 3, Palette.Peach);
                break;
            case 2:
                for (int post = 0; post < 12; post += 4)
                {
                    r.Fill(x + post, 19, 1, 5, Palette.Wood);
                }

                r.Fill(x, 20, 12, 1, Palette.Wood);
                r.Fill(x, 22, 12, 1, Palette.Wood);
                break;
            case 3:
                for (int stalk = 0; stalk < 16; stalk++)
                {
                    int height = 2 + (stalk + x) % 2;
                    r.Fill(x + stalk, 24 - height, 1, height, stalk % 3 == 0 ? Palette.Yellow : Palette.Gold);
                }

                break;
            case 4:
                r.Fill(x, 22, 4, 2, Palette.Gray);
                r.Fill(x + 1, 21, 2, 1, Palette.Silver);
                break;
            case 5:
                r.Fill(x, 19, 7, 5, Palette.Gray);
                r.Fill(x + 1, 20, 5, 1, Palette.Navy);
                r.Fill(x, 14, 1, 5, Palette.Bark);
                r.Fill(x + 6, 14, 1, 5, Palette.Bark);
                r.Fill(x - 1, 13, 9, 1, Palette.Brick);
                r.Set(x + 3, 16, Palette.Steel);
                break;
            default:
                r.Fill(x + 4, 13, 1, 11, Palette.Bark);
                r.Fill(x, 15, 9, 1, Palette.Bark);
                r.Fill(x + 3, 12, 3, 2, Palette.Skin);
                r.Fill(x + 2, 11, 5, 1, Palette.Gold);
                r.Fill(x + 3, 16, 3, 4, Palette.Blue);
                break;
        }
    }

    private static void Signpost(ArtRaster r, int x)
    {
        r.Fill(x, 14, 1, 10, Palette.Bark);
        r.Fill(x - 7, 14, 13, 6, Palette.Bark);
        r.Fill(x - 6, 15, 11, 4, Palette.Wood);
        r.Set(x + 6, 16, Palette.Wood);
        r.Set(x + 6, 17, Palette.Wood);
        ArtFont.Draw(r, ">", x - 1, 15, Palette.Soil);
    }
}
