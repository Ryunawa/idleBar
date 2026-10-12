using System;

namespace IdleBar.Frontier;

public static class Shop
{
    private const int MaxBatch = 100_000;

    public static bool Available(FrontierState state, Purchase purchase) => purchase switch
    {
        BuildingPurchase building => Economy.Unlocked(state, building.Kind),
        _ => true,
    };

    public static double Cost(FrontierState state, Purchase purchase) => purchase switch
    {
        FighterPurchase fighter => Economy.FighterCost(state, fighter.Kind),
        BuildingPurchase building => Economy.BuildingCost(state, building.Kind),
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
        if (!Affordable(state, purchase))
        {
            return 0;
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
            case FighterPurchase fighter:
                state.Fighters[(int)fighter.Kind]++;
                events.Add(new Trained(fighter.Kind, state.Level(fighter.Kind)));
                break;
            case BuildingPurchase building:
                Raise(state, building.Kind, events);
                break;
        }

        return true;
    }

    private static double Growth(Purchase purchase) =>
        purchase is BuildingPurchase ? Balance.BuildingCostGrowth : Balance.FighterCostGrowth;

    private static void Raise(FrontierState state, BuildingKind kind, EventLog events)
    {
        int tier = Economy.Tier(state.Level(kind));
        state.Buildings[(int)kind]++;
        int level = state.Level(kind);
        events.Add(new Built(kind, level));
        if (Economy.Tier(level) > tier)
        {
            events.Add(new TierReached(kind, Economy.Tier(level)));
        }
    }
}
