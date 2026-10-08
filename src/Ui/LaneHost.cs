using System;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public static class LaneHost
{
    private const int Offset = 6;

    public static int X(Tavern tavern) => tavern.Layout.StationXs[^1] + Offset;

    public static void PaintBody(PixelCanvas canvas, int top, Tavern tavern, PatronLook? look)
    {
        if (look is not null)
        {
            canvas.Draw(PatronSprites.For(look, Gaze.Front), X(tavern) - PatronSprites.Width / 2, top + TavernRows.PatronTop);
        }
    }

    public static void PaintName(PixelCanvas canvas, Font font, int top, Tavern tavern, PatronLook? look, string? name)
    {
        if (look is not null && name is not null)
        {
            LaneOverlay.PaintTag(canvas, font, top, X(tavern), new NameTag(name, BarPalette.Gold, false));
        }
    }

    public static int? Anchor(Tavern tavern, Guid author, Func<Patron, Guid?> playerOf, Guid? host)
    {
        if (tavern.Patrons.FirstOrDefault(patron => patron.Phase != PatronPhase.Gone && playerOf(patron) == author) is Patron speaker)
        {
            return (int)MathF.Round(speaker.X);
        }

        return author == host ? X(tavern) : null;
    }

    public static string DescribeHelper(int level)
    {
        string who = level switch
        {
            >= 3 => "Ta serveuse",
            2 => "Ton commis",
            _ => "Ton apprenti",
        };
        return $"{who} · sert les clients qui attendent depuis {CounterHelper.Delay(level):0} s";
    }
}
