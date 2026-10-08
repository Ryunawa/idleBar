using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public static class Lounge
{
    private const double MingleChance = 0.4;
    private const int MaxMinglers = 4;
    private const float MinMingle = 25f;
    private const float MaxMingle = 60f;
    private const int SeatClearance = 18;
    private const int GroupClearance = 26;
    private const int PairGap = 11;
    private const int EdgeMargin = 12;
    private const int Step = 3;

    public static bool TryMingle(Patron patron, TavernLayout layout, IReadOnlyList<Patron> patrons, Random random)
    {
        if (patron.Visit is not null
            || random.NextDouble() >= MingleChance
            || patrons.Count(other => other.Phase == PatronPhase.Mingling) >= MaxMinglers
            || Spot(layout, patrons, random) is not var (x, partner))
        {
            return false;
        }

        patron.Mingle(x, MinMingle + (float)random.NextDouble() * (MaxMingle - MinMingle), partner);
        if (partner is not null)
        {
            partner.Partner = patron;
        }

        return true;
    }

    public static void Part(Patron patron)
    {
        if (patron.Partner is Patron partner && partner.Partner == patron)
        {
            partner.Partner = null;
        }

        patron.Leave();
    }

    public static (int X, Patron? Partner)? Spot(TavernLayout layout, IReadOnlyList<Patron> patrons, Random random)
    {
        List<Patron> mingling = patrons.Where(patron => patron.Phase == PatronPhase.Mingling).ToList();
        if (mingling.FirstOrDefault(patron => patron.Partner is null) is Patron lone)
        {
            int first = random.Next(2) == 0 ? 1 : -1;
            foreach (int side in new[] { first, -first })
            {
                int x = lone.StandX + side * PairGap;
                if (Free(layout, mingling, x, lone))
                {
                    return (x, lone);
                }
            }
        }

        int from = layout.DoorX + TavernLayout.DoorWidth + EdgeMargin;
        List<int> spots = Enumerable.Range(0, Math.Max(0, (layout.Width - EdgeMargin - from) / Step))
            .Select(index => from + index * Step)
            .Where(x => Free(layout, mingling, x, null) && (Fits(layout, x + PairGap) || Fits(layout, x - PairGap)))
            .ToList();
        return spots.Count == 0 ? null : (spots[random.Next(spots.Count)], null);
    }

    private static bool Free(TavernLayout layout, IReadOnlyList<Patron> mingling, int x, Patron? partner) =>
        Fits(layout, x) && mingling.Where(other => other != partner).All(other => Math.Abs(other.StandX - x) >= GroupClearance);

    private static bool Fits(TavernLayout layout, int x) =>
        x >= layout.DoorX + TavernLayout.DoorWidth + EdgeMargin
        && x <= layout.Width - EdgeMargin
        && layout.SeatXs.All(seat => Math.Abs(seat - x) >= SeatClearance);
}
