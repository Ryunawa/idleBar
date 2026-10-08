using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class HelperPainter
{
    private const float StepRate = 7f;
    private const float BlinkEvery = 4.3f;
    private const float BlinkSeconds = 0.14f;
    private const float WipeRate = 7f;
    private const int TrayWidth = 7;
    private const int TrayRow = 8;
    private const int DrinkSpacing = 4;

    private static readonly PatronLook Apprentice = new(0, new("ffdbac"), new("a0522d"), new("8a6a44"), new("6b5a3a"));
    private static readonly PatronLook Commis = new(3, new("e0ac69"), new("2b1d14"), new("40689c"), new("e8e2d0"));
    private static readonly PatronLook Waitress = new(5, new("f1c27d"), new("5a3825"), new("9c4040"), new("7a2f2f"));

    private static readonly Color Apron = new("efe6d2");
    private static readonly Color ApronShade = new("cfc5ad");
    private static readonly Color Pewter = PixelPalette.Resolve('n');
    private static readonly Color PewterDark = PixelPalette.Resolve('N');
    private static readonly Color Cloth = new("e8e2d0");

    public static PatronLook LookFor(int level) => level switch
    {
        >= 3 => Waitress,
        2 => Commis,
        _ => Apprentice,
    };

    public static Rect2I Bounds(CounterHelper helper, int top) =>
        new(Center(helper) - PatronSprites.Width / 2, top + TavernRows.PatronTop, PatronSprites.Width, TavernRows.CounterTop - TavernRows.PatronTop);

    public static void PaintBody(PixelCanvas canvas, int top, CounterHelper helper, float time)
    {
        if (!helper.Hired || float.IsNaN(helper.X))
        {
            return;
        }

        Gaze gaze = helper.Heading switch
        {
            < 0 => Gaze.Left,
            > 0 => Gaze.Right,
            _ => time % BlinkEvery < BlinkSeconds ? Gaze.Closed : Gaze.Front,
        };
        int bob = helper.Heading != 0 && helper.Task != HelperTask.Serving && (int)(time * StepRate) % 2 == 0 ? 1 : 0;
        int left = Center(helper) - PatronSprites.Width / 2;
        int y = top + TavernRows.PatronTop - bob;
        canvas.Draw(PatronSprites.For(LookFor(helper.Level), gaze), left, y);
        canvas.Fill(left + 3, y + 7, 3, 1, Apron);
        canvas.Fill(left + 2, y + 8, 5, 5, Apron);
        canvas.Fill(left + 2, y + 9, 5, 1, ApronShade);
        PaintHands(canvas, top, helper, time);
    }

    private static void PaintHands(PixelCanvas canvas, int top, CounterHelper helper, float time)
    {
        int center = Center(helper);
        IReadOnlyList<PreparedDrink> drinks = helper.Tray;
        if (drinks.Count > 0)
        {
            int side = helper.Heading < 0 ? -1 : 1;
            int width = TrayWidth + DrinkSpacing * (drinks.Count - 1);
            int tray = side > 0 ? center + 4 : center - 4 - width;
            int y = top + TavernRows.PatronTop + TrayRow;
            canvas.Fill(side > 0 ? center + 5 : center - 6, y + 1, 1, 2, LookFor(helper.Level).Skin);
            canvas.Fill(tray, y, width, 1, Pewter);
            canvas.Fill(tray + 1, y + 1, width - 2, 1, PewterDark);
            for (int index = drinks.Count - 1; index >= 0; index--)
            {
                DrinkPainter.Paint(canvas, drinks[index], tray + 1 + index * DrinkSpacing, y - 1);
            }

            return;
        }

        if (helper.Task == HelperTask.Wiping)
        {
            int sweep = (int)MathF.Round(MathF.Sin(time * WipeRate) * 2);
            int y = top + TavernRows.CounterTop - 1;
            canvas.Fill(center - 1 + sweep, y, 3, 1, Cloth);
            canvas.Fill(center + sweep, y - 1, 1, 1, LookFor(helper.Level).Skin);
        }
    }

    private static int Center(CounterHelper helper) => (int)MathF.Round(helper.X);
}
