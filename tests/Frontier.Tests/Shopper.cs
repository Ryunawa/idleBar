using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class Shopper
{
    private const double FighterWeight = 2;
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

    public void Restock(double clock)
    {
        Gear.Restock(_game.State, clock);
        Gear.Wear(_game);
    }

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

    public void Loot() => Gear.Loot(_game);

    private IEnumerable<(double Cost, double Value, Func<bool> Apply)> Options(FrontierState state)
    {
        GearWorth worth = new(state);
        double rate = FighterWeight * CombatRate(state) * Economy.TotalDps(state);
        foreach (FighterInfo fighter in Fighters.All)
        {
            for (int slot = 0; slot < Items.Slots.Count; slot++)
            {
                int index = slot;
                if (Gear.Upgrade(worth, state, (int)fighter.Kind, slot) is (double cost, double gain))
                {
                    yield return (cost, rate * Math.Max(0, gain) / cost, () => Gear.Upgrade(_game, (int)fighter.Kind, index));
                }
            }
        }

        for (int offer = 0; offer < Gear.Offers; offer++)
        {
            int index = offer;
            double price = Gear.Price(index);
            yield return (price, rate * Gear.OfferGain(worth, state, index) / price, () => Gear.Buy(_game, index));
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
