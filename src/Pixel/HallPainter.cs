using System;
using Godot;

namespace IdleBar.Pixel;

public static class HallPainter
{
    public const int MaxLevel = 5;

    private const int RoofRows = 3;

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
    private static readonly Color Banner = new("a8473b");
    private static readonly Color Gold = new("f2c14e");
    private static readonly Color Chimney = new("2b2f36");
    private static readonly Color Smoke = new("9aa3ad");

    public static int Height(int level) => level <= 1 ? 0 : 12 + level;

    public static void Paint(PixelCanvas canvas, int x, int width, int level, Ambience ambience)
    {
        if (level <= 1)
        {
            return;
        }

        int ground = LandscapePainter.GroundTop(canvas);
        int height = Height(level);
        int top = ground - height + 1;
        int wallTop = top + RoofRows;
        bool stone = level >= MaxLevel;
        bool timber = level >= 3;
        Color wall = stone ? Stone : timber ? Plaster : Plank;
        canvas.Fill(x + 1, wallTop, width - 2, ground - wallTop + 1, wall);
        for (int column = x + 1; column < x + width - 1; column++)
        {
            if (stone)
            {
                for (int row = wallTop + (column % 2); row <= ground; row += 2)
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
            canvas.Fill(x + 1, wallTop + 4, width - 2, 1, Beam);
        }

        for (int row = 0; row < RoofRows; row++)
        {
            int inset = RoofRows - 1 - row;
            canvas.Fill(x + inset, top + row, width - inset * 2, 1, row == 0 ? RoofLight : Roof);
        }

        PaintWindows(canvas, x, width, wallTop, level, ambience.Light.Lit);
        if (level >= 4)
        {
            canvas.Fill(x + width - 4, top - 2, 2, 3, Chimney);
            float age = ambience.Time * 0.5f % 1;
            canvas.Fill(x + width - 4 + (int)(age * 3), top - 3 - (int)(age * 5), 1, 1, Smoke with { A = 0.5f * (1 - age) });
        }

        if (stone)
        {
            int middle = x + width / 2;
            canvas.Fill(middle - 2, wallTop + 1, 4, 5, Banner);
            canvas.Fill(middle - 1, wallTop + 2, 2, 2, Gold);
        }
    }

    private static void PaintWindows(PixelCanvas canvas, int x, int width, int wallTop, int level, bool lit)
    {
        if (level < 3)
        {
            return;
        }

        Color glass = lit ? Window : DarkWindow;
        int rows = level >= 4 ? 2 : 1;
        for (int row = 0; row < rows; row++)
        {
            int y = wallTop + 1 + row * 5;
            canvas.Fill(x + 3, y, 2, 2, glass);
            canvas.Fill(x + width - 5, y, 2, 2, glass);
        }
    }
}