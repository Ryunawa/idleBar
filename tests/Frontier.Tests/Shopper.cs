using System;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class Shopper
{
    private const double FighterWeight = 2;
    private const double ArmorWeight = 0.5;
    private const double Reach = 20;

    private static readonly BuildingKind[] Preference =
    [
        BuildingKind.Forge, BuildingKind.Bakery, BuildingKind.Workshop, BuildingKind.Market, BuildingKind.Barracks,
        BuildingKind.Tower, BuildingKind.Belfry, BuildingKind.Portal, BuildingKind.Chapel, BuildingKind.Mill,
        BuildingKind.Farm, BuildingKind.Tavern,
    ];

    private readonly FrontierGame _game;
    private readonly HeroRole _first;
    private readonly HeroRole _second;

    public Shopper(FrontierGame game, HeroRole first = HeroRole.Archer, HeroRole second = HeroRole.Mage)
    {
        _game = game;
        _first = first;
        _second = second;
    }

    public GearCase Gear { get; } = new();

    public static (HeroRole First, HeroRole Second) Complement(HeroRole hero) => hero switch
    {
        HeroRole.Tank => (HeroRole.Support, HeroRole.Mage),
        HeroRole.Support => (HeroRole.Mage, HeroRole.Archer),
        _ => (HeroRole.Support, HeroRole.Tank),
    };

    public void Recruit()
    {
        _game.Buy(new Recruitment(FighterKind.First, _first));
        _game.Buy(new Recruitment(FighterKind.Second, _second));
    }

    public bool BuyOne()
    {
        FrontierState state = _game.State;
        double reach = state.Gold + Income(state) * Reach;
        (double Cost, double Value, Func<bool> Apply)? best = null;
        (double Cost, double Value, Func<bool> Apply)? cheapest = null;
        foreach ((double Cost, double Value, Func<bool> Apply) option in Options(state))
        {
            if (cheapest is null || option.Cost < cheapest.Value.Cost)
            {
                cheapest = option;
            }

            if (option.Cost <= reach && (best is null || option.Value > best.Value.Value))
            {
                best = option;
            }
        }

        return (best ?? cheapest) is { } pick && state.Gold >= pick.Cost && pick.Apply();
    }

    private IEnumerable<(double Cost, double Value, Func<bool> Apply)> Options(FrontierState state)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            FighterKind kind = fighter.Kind;
            if (state.Level(kind) <= 0)
            {
                continue;
            }

            foreach (string slot in Items.Slots)
            {
                double cost = Gear.Cost(state, kind, slot);
                double weight = GearCase.Damages(slot) ? 1 : ArmorWeight;
                double value = FighterWeight * weight * CombatRate(state) * Economy.Dps(state, kind) * Gear.Gain(state, kind, slot) / 100 / cost;
                yield return (cost, value, () => Gear.Buy(_game, kind, slot));
            }
        }

        double armor = Economy.ArmorCost(state);
        double armorGain = Vitality.ArmorFactor(state.Armor + 1) / Vitality.ArmorFactor(state.Armor) - 1;
        yield return (armor, FighterWeight * ArmorWeight * CombatRate(state) * Economy.TotalDps(state) * armorGain / armor, () => _game.Buy(new ArmorPurchase()));
        for (int plot = 0; plot < FrontierState.PlotCount; plot++)
        {
            int index = plot;
            if (state.Built[plot] is not null)
            {
                double cost = Economy.PlotCost(state, plot);
                yield return (cost, Plots.Of(plot).Gold * Economy.Wealth(state) / cost, () => _game.Buy(new PlotPurchase(index)));
            }
            else if (Economy.Unlocked(state, plot) && Next(state) is BuildingKind kind)
            {
                yield return (Economy.PlotCost(state, plot), double.PositiveInfinity, () => _game.Buy(new Construction(index, kind)));
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

    private static double Income(FrontierState state) => Economy.GoldPerSecond(state) + CombatRate(state) * Economy.TotalDps(state);

    private static double CombatRate(FrontierState state) =>
        Balance.KillGoldRatio * Economy.Wealth(state) * (1 + Balance.MarketBonus * state.Level(BuildingKind.Market));
}
