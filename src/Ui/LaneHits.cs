using System;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public static class LaneHits
{
    private const float DoorReach = 7f;

    public static int Station(Tavern tavern, Vector2I pixel, int top)
    {
        for (int index = 0; index < tavern.Stations.Count; index++)
        {
            if (StationPainter.Bounds(tavern.Stations[index], top).HasPoint(pixel))
            {
                return index;
            }
        }

        return -1;
    }

    public static Patron? Patron(Tavern tavern, Vector2I pixel, int top) =>
        tavern.Patrons.FirstOrDefault(patron =>
            new Rect2I((int)MathF.Round(patron.X) - PatronSprites.Width / 2, top + TavernRows.PatronTop, PatronSprites.Width, TavernRows.CounterTop - TavernRows.PatronTop)
                .HasPoint(pixel));

    public static StreetWalk? Walk(Street? street, Vector2I pixel, int top) =>
        street?.Current is StreetWalk walk && PasserbyPainter.Pane(walk.WindowX, top).Grow(1).HasPoint(pixel) ? walk : null;

    public static bool DoorOpen(Tavern tavern) =>
        tavern.Patrons.Any(patron =>
            patron.Phase is PatronPhase.Entering or PatronPhase.Leaving && Math.Abs(patron.X - tavern.Layout.DoorCenter) < DoorReach);
}
