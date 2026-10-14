using System;
using System.Collections.Generic;
using System.Linq;
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

    public Shopper(FrontierGame game, HeroRole first = HeroRole.Archer, HeroRole second = HeroRole.Mage, int seed = 1)
    {
        _game = game;
        Gear = new GearCase(seed);
        _first = first;
        _second = second;
    }

    public GearCase Gear { get; }

    public static (HeroRole First, HeroRole Second) Complement(HeroRole hero) => hero switch
    {
        HeroRole.Archer => (HeroRole.Support, HeroRole.Warrior),
        HeroRole.Mage => (HeroRole.Support, HeroRole.Archer),
        _ => (HeroRole.Mage, HeroRole.Archer),
    };

    public void Restock(double clock) => Gear.Restock(_game.State, clock);

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
                double cost = Gear.UpgradeCost(state, kind, slot);
                yield return (cost, Worth(state, kind, slot, Gear.UpgradeGain(kind, slot)) / cost, () => Gear.Upgrade(_game, kind, slot));
            }
        }

        for (int offer = 0; offer < Gear.Offers.Count; offer++)
        {
            int index = offer;
            FighterKind wearer = Fighters.All.Select(fighter => fighter.Kind).Where(kind => state.Level(kind) > 0)
                .MaxBy(kind => Worth(state, kind, Gear.Offers[index].Slot, Gear.OfferGain(kind, index)));
            double price = Gear.Offers[index].Price;
            yield return (price, Worth(state, wearer, Gear.Offers[index].Slot, Gear.OfferGain(wearer, index)) / price, () => Gear.Buy(_game, wearer, index));
        }

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

    private static double Worth(FrontierState state, FighterKind kind, string slot, double gain) =>
        FighterWeight * (GearCase.Damages(slot) ? 1 : ArmorWeight) * CombatRate(state) * Economy.Dps(state, kind) * Math.Max(0, gain) / 100;

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
        Balance.KillGoldRatio * Economy.Wealth(state) * (1 + Balance.MarketBonus * state.Level(BuildingKind.Market)) * Economy.Portal(state);
}
