using System;

namespace IdleBar.Frontier;

public static class Shop
{
    private const int MaxBatch = 100_000;

    public static bool Available(FrontierState state, Purchase purchase) => purchase switch
    {
        Construction build => Economy.Unlocked(state, build.Plot) && state.Built[build.Plot] is null && state.PlotOf(build.Kind) is null,
        PlotPurchase raise => state.Built[raise.Plot] is not null,
        Recruitment recruit => Fighters.IsCompanion(recruit.Kind) && state.Level(recruit.Kind) == 0 && Fighters.Unlocked(state, recruit.Kind),
        _ => true,
    };

    public static double Cost(FrontierState state, Purchase purchase) => purchase switch
    {
        Recruitment => 0,
        ArmorPurchase => Economy.ArmorCost(state),
        Construction build => Economy.PlotCost(state, build.Plot),
        PlotPurchase raise => Economy.PlotCost(state, raise.Plot),
        _ => double.PositiveInfinity,
    };

    public static bool Affordable(FrontierState state, Purchase purchase) =>
        Available(state, purchase) && state.Gold >= Cost(state, purchase);

    public static double CostOf(FrontierState state, Purchase purchase, int count)
    {
        double growth = Growth(purchase);
        return Cost(state, purchase) * (Math.Pow(growth, count) - 1) / (growth - 1);
    }

    public static int MaxCount(FrontierState state, Purchase purchase)
    {
        if (!Affordable(state, purchase) || purchase is Construction or Recruitment)
        {
            return Affordable(state, purchase) ? 1 : 0;
        }

        double growth = Growth(purchase);
        double levels = Math.Log(1 + state.Gold * (growth - 1) / Cost(state, purchase)) / Math.Log(growth);
        int count = (int)Math.Min(MaxBatch, Math.Floor(levels));
        while (count > 1 && CostOf(state, purchase, count) > state.Gold)
        {
            count--;
        }

        while (count < MaxBatch && CostOf(state, purchase, count + 1) <= state.Gold)
        {
            count++;
        }

        return Math.Max(1, count);
    }

    public static bool Buy(FrontierState state, Purchase purchase, EventLog events)
    {
        if (!Affordable(state, purchase))
        {
            return false;
        }

        state.Gold -= Cost(state, purchase);
        switch (purchase)
        {
            case Recruitment recruit:
                state.Companions[(int)recruit.Kind - 1] = recruit.Class;
                state.Fighters[(int)recruit.Kind] = 1;
                state.Xp[(int)recruit.Kind] = 0;
                events.Add(new Trained(recruit.Kind, 1));
                break;
            case ArmorPurchase:
                state.Armor++;
                events.Add(new Armored(state.Armor));
                break;
            case Construction build:
                state.Built[build.Plot] = build.Kind;
                Raise(state, build.Plot, events);
                break;
            case PlotPurchase raise:
                Raise(state, raise.Plot, events);
                break;
        }

        return true;
    }

    private static double Growth(Purchase purchase) =>
        purchase is ArmorPurchase ? Balance.FighterCostGrowth : Balance.BuildingCostGrowth;

    private static void Raise(FrontierState state, int plot, EventLog events)
    {
        if (state.Built[plot] is not BuildingKind kind)
        {
            return;
        }

        int tier = Economy.Tier(state.Levels[plot]);
        state.Levels[plot]++;
        int level = state.Levels[plot];
        events.Add(new Built(plot, kind, level));
        if (Economy.Tier(level) > tier)
        {
            events.Add(new TierReached(plot, kind, Economy.Tier(level)));
        }
    }
}
