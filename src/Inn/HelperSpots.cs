using System;
using System.Linq;

namespace IdleBar.Inn;

public static class HelperSpots
{
    private const int HomeGap = 6;
    private const int SeatClearance = 9;
    private const int Overhang = 14;
    private const int SpotTries = 8;

    public static int Home(TavernLayout layout) => layout.DoorX + TavernLayout.DoorWidth + HomeGap;

    public static int Wipe(TavernLayout layout, Random random)
    {
        int from = layout.SeatXs[0] - Overhang;
        int to = Math.Min(layout.SeatXs[^1] + Overhang, layout.Width - Overhang);
        for (int attempt = 0; attempt < SpotTries && to > from; attempt++)
        {
            int spot = random.Next(from, to);
            if (layout.SeatXs.All(seat => Math.Abs(seat - spot) >= SeatClearance))
            {
                return spot;
            }
        }

        return Home(layout);
    }
}
