using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Intendant
{
    private const double FighterWeight = 2;
    private const int CompanionHeroLevel = 5;
    private const double ArmorWeight = 0.5;

    private static readonly BuildingKind[] Preference =
    [
        BuildingKind.Forge, BuildingKind.Bakery, BuildingKind.Workshop, BuildingKind.Market, BuildingKind.Barracks,
        BuildingKind.Tower, BuildingKind.Belfry, BuildingKind.Portal, BuildingKind.Chapel, BuildingKind.Mill,
        BuildingKind.Farm, BuildingKind.Tavern,
    ];

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

        if (state.Level(FighterKind.Hero) >= CompanionHeroLevel)
        {
            yield return new ArmorPurchase();
        }

        for (int plot = 0; plot < FrontierState.PlotCount; plot++)
        {
            if (state.Built[plot] is not null)
            {
                yield return new PlotPurchase(plot);
            }
            else if (Economy.Unlocked(state, plot) && Next(state) is BuildingKind kind)
            {
                yield return new Construction(plot, kind);
            }
        }
    }

    private static BuildingKind? Next(FrontierState state)
    {
        foreach (BuildingKind kind in Preference)
        {
            if (state.PlotOf(kind) is null)
            {
                return kind;
            }
        }

        return null;
    }

    private static double Value(FrontierState state, Purchase purchase, double cost) => purchase switch
    {
        FighterPurchase fighter => FighterWeight * CombatRate(state) * DpsGain(state, fighter.Kind) / cost,
        ArmorPurchase => FighterWeight * ArmorWeight * CombatRate(state) * Economy.TotalDps(state) * ArmorGain(state) / cost,
        Construction => double.PositiveInfinity,
        PlotPurchase raise => PlotGain(state, raise.Plot) / cost,
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

    private static double ArmorGain(FrontierState state) =>
        Vitality.ArmorFactor(state.Armor + 1) / Vitality.ArmorFactor(state.Armor) - 1;

    private static double PlotGain(FrontierState state, int plot) =>
        Plots.Of(plot).Gold * Economy.Wealth(state);
}
