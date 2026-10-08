using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public static class Lounge
{
    private const double MingleChance = 0.6;
    private const int MaxMinglers = 8;
    private const int MaxGroup = 3;
    private const float MinMingle = 45f;
    private const float MaxMingle = 120f;
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
        if (partner is not null && partner.Partner is not { Phase: PatronPhase.Mingling })
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
        List<Patron> mingling = patrons.Where(patron => patron.Phase == PatronPhase.Mingling).OrderBy(patron => patron.StandX).ToList();
        foreach (List<Patron> group in Groups(mingling).Where(group => group.Count < MaxGroup).OrderBy(_ => random.Next()))
        {
            (Patron End, int Side)[] ends = [(group[0], -1), (group[^1], 1)];
            foreach ((Patron end, int side) in random.Next(2) == 0 ? ends : ends.Reverse())
            {
                int x = end.StandX + side * PairGap;
                if (Fits(layout, x) && mingling.Except(group).All(other => Math.Abs(other.StandX - x) >= GroupClearance))
                {
                    return (x, end);
                }
            }
        }

        int from = layout.DoorX + TavernLayout.DoorWidth + EdgeMargin;
        List<int> spots = Enumerable.Range(0, Math.Max(0, (layout.Width - EdgeMargin - from) / Step))
            .Select(index => from + index * Step)
            .Where(x => Fits(layout, x)
                && mingling.All(other => Math.Abs(other.StandX - x) >= GroupClearance)
                && (Fits(layout, x + PairGap) || Fits(layout, x - PairGap)))
            .ToList();
        return spots.Count == 0 ? null : (spots[random.Next(spots.Count)], null);
    }

    private static IEnumerable<List<Patron>> Groups(IReadOnlyList<Patron> sorted)
    {
        List<Patron> group = [];
        foreach (Patron patron in sorted)
        {
            if (group.Count > 0 && patron.StandX - group[^1].StandX > PairGap)
            {
                yield return group;
                group = [];
            }

            group.Add(patron);
        }

        if (group.Count > 0)
        {
            yield return group;
        }
    }

    private static bool Fits(TavernLayout layout, int x) =>
        x >= layout.DoorX + TavernLayout.DoorWidth + EdgeMargin
        && x <= layout.Width - EdgeMargin
        && layout.SeatXs.All(seat => Math.Abs(seat - x) >= SeatClearance);
}
