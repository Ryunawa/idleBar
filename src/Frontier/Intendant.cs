using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Intendant
{
    private const double FighterWeight = 2;
    private const int CompanionHeroLevel = 5;

    public static bool Run(FrontierState state, EventLog events) =>
        Choose(state) is Purchase purchase && Shop.Buy(state, purchase, events);

    public static Purchase? Choose(FrontierState state)
    {
        double reach = state.Gold + Income(state) * Balance.IntendantReach;
        Purchase? best = null;
        double bestValue = double.NegativeInfinity;
        Purchase? cheapest = null;
        double cheapestCost = double.PositiveInfinity;
        foreach (Purchase purchase in Candidates(state))
        {
            double cost = Shop.Cost(state, purchase);
            if (cost < cheapestCost)
            {
                cheapest = purchase;
                cheapestCost = cost;
            }

            if (cost > reach)
            {
                continue;
            }

            double value = Value(state, purchase, cost);
            if (value > bestValue)
            {
                best = purchase;
                bestValue = value;
            }
        }

        return best ?? cheapest;
    }

    private static IEnumerable<Purchase> Candidates(FrontierState state)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            if (fighter.Kind == FighterKind.Hero || state.Level(fighter.Kind) > 0 || state.Level(FighterKind.Hero) >= CompanionHeroLevel)
            {
                yield return new FighterPurchase(fighter.Kind);
            }
        }

        foreach (BuildingInfo building in Buildings.All)
        {
            if (Economy.Unlocked(state, building.Kind))
            {
                yield return new BuildingPurchase(building.Kind);
            }
        }
    }

    private static double Value(FrontierState state, Purchase purchase, double cost) => purchase switch
    {
        FighterPurchase fighter => FighterWeight * CombatRate(state) * DpsGain(state, fighter.Kind) / cost,
        BuildingPurchase building when state.Level(building.Kind) == 0 => double.PositiveInfinity,
        BuildingPurchase building => BuildingGain(state, building.Kind) / cost,
        _ => 0,
    };

    private static double Income(FrontierState state) => Economy.GoldPerSecond(state) + CombatRate(state) * Economy.TotalDps(state);

    private static double CombatRate(FrontierState state) =>
        Balance.KillGoldRatio * Economy.Wealth(state) * (1 + Balance.MarketBonus * state.Level(BuildingKind.Market));

    private static double DpsGain(FrontierState state, FighterKind kind)
    {
        int level = state.Level(kind);
        return Economy.BaseDps(state, kind, level + 1) - Economy.BaseDps(state, kind, level);
    }

    private static double BuildingGain(FrontierState state, BuildingKind kind) =>
        Buildings.Of(kind).Gold * Economy.Wealth(state);
}
