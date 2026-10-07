using System;
using Godot;

namespace IdleBar.Pixel;

public static class HallPainter
{
    public const int MaxLevel = 5;

    private const int RoofRows = 3;

    private static readonly int[] Widths = [16, 20, 24, 28, 32];
    private static readonly int[] Heights = [7, 8, 9, 10, 12];

    private static readonly Color Plank = new("6e5034");
    private static readonly Color PlankLine = new("4e331d");
    private static readonly Color Plaster = new("d8c8a0");
    private static readonly Color Beam = new("7a5230");
    private static readonly Color Stone = new("8a96a3");
    private static readonly Color StoneDark = new("5c636d");
    private static readonly Color Roof = new("7a2f27");
    private static readonly Color RoofLight = new("a8473b");
    private static readonly Color Window = new("f7d774");
    private static readonly Color DarkWindow = new("2b2f36");
    private static readonly Color Door = new("4e331d");
    private static readonly Color Banner = new("a8473b");
    private static readonly Color Gold = new("f2c14e");
    private static readonly Color Chimney = new("2b2f36");
    private static readonly Color Smoke = new("9aa3ad");

    public static int Width(int level) => Widths[Tier(level)];

    public static int Height(int level) => Heights[Tier(level)];

    public static void Paint(PixelCanvas canvas, int x, int level, Ambience ambience)
    {
        int tier = Tier(level);
        int width = Widths[tier];
        int ground = LandscapePainter.GroundTop(canvas);
        int top = ground - Heights[tier] + 1;
        int wallTop = top + RoofRows;
        bool stone = tier == MaxLevel - 1;
        bool timber = tier >= 2;
        Color wall = stone ? Stone : timber ? Plaster : Plank;
        canvas.Fill(x + 1, wallTop, width - 2, ground - wallTop + 1, wall);
        for (int column = x + 1; column < x + width - 1; column++)
        {
            if (stone)
            {
                for (int row = wallTop + column % 2; row <= ground; row += 2)
                {
                    canvas.Fill(column, row, 1, 1, StoneDark);
                }
            }
            else if (!timber && column % 3 == 0)
            {
                canvas.Fill(column, wallTop, 1, ground - wallTop + 1, PlankLine);
            }
        }

        if (timber && !stone)
        {
            canvas.Fill(x + 1, wallTop, 1, ground - wallTop + 1, Beam);
            canvas.Fill(x + width - 2, wallTop, 1, ground - wallTop + 1, Beam);
            canvas.Fill(x + 1, wallTop + 3, width - 2, 1, Beam);
        }

        for (int row = 0; row < RoofRows; row++)
        {
            int inset = RoofRows - 1 - row;
            canvas.Fill(x + inset, top + row, width - inset * 2, 1, row == 0 ? RoofLight : Roof);
        }

        int doorX = x + width / 2 - 2;
        canvas.Fill(doorX, ground - 3, 4, 4, Door);
        Color glass = ambience.Light.Lit ? Window : DarkWindow;
        if (tier >= 1)
        {
            canvas.Fill(x + 3, wallTop + 1, 2, 2, glass);
            canvas.Fill(x + width - 5, wallTop + 1, 2, 2, glass);
        }

        if (tier >= 2)
        {
            canvas.Fill(x + width - 5, top - 2, 2, 3, Chimney);
            float age = ambience.Time * 0.5f % 1;
            canvas.Fill(x + width - 5 + (int)(age * 3), top - 3 - (int)(age * 4), 1, 1, Smoke with { A = 0.5f * (1 - age) });
        }

        if (stone)
        {
            canvas.Fill(doorX - 4, wallTop + 1, 3, 4, Banner);
            canvas.Fill(doorX - 3, wallTop + 2, 1, 1, Gold);
            canvas.Fill(doorX + 5, wallTop + 1, 3, 4, Banner);
            canvas.Fill(doorX + 6, wallTop + 2, 1, 1, Gold);
        }
    }

    private static int Tier(int level) => Math.Clamp(level, 1, MaxLevel) - 1;
}