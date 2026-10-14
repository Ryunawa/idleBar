using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class GearCase
{
    private const double DamageBase = 4;
    private const double HealthBase = 6;
    private const double WindowSeconds = 3 * 3600;
    private const int OfferCount = 4;

    private static readonly double[] Odds = [0.62, 0.28, 0.08, 0.02];
    private static readonly double[] Factors = [1, 1.6, 2.5, 4];

    private readonly Dictionary<(FighterKind Kind, string Slot), (int Level, int Rarity, int Upgrades)> _pieces = [];
    private readonly List<(string Slot, int Level, int Rarity, double Price)> _offers = [];
    private readonly Random _random;
    private long _window = -1;

    public GearCase(int seed)
    {
        _random = new Random(seed);
    }

    public int Count => _pieces.Count;

    public IReadOnlyList<(string Slot, int Level, int Rarity, double Price)> Offers => _offers;

    public void Restock(FrontierState state, double clock)
    {
        long window = (long)(clock / WindowSeconds);
        if (window == _window)
        {
            return;
        }

        _window = window;
        _offers.Clear();
        for (int index = 0; index < OfferCount; index++)
        {
            string slot = Items.Slots[_random.Next(Items.Slots.Count)];
            int rarity = Rarity(_random.NextDouble());
            _offers.Add((slot, state.MaxStage, rarity, GearPrices.Offer(state, state.MaxStage, rarity)));
        }
    }

    public double UpgradeCost(FrontierState state, FighterKind kind, string slot) =>
        _pieces.TryGetValue((kind, slot), out (int Level, int Rarity, int Upgrades) piece) ? GearPrices.Upgrade(state, piece.Rarity, piece.Upgrades, 1) : double.PositiveInfinity;

    public double UpgradeGain(FighterKind kind, string slot) =>
        _pieces.TryGetValue((kind, slot), out (int Level, int Rarity, int Upgrades) piece)
            ? Value(slot, piece.Level, piece.Rarity, piece.Upgrades + 1) - Value(slot, piece.Level, piece.Rarity, piece.Upgrades) : 0;

    public double OfferGain(FighterKind kind, int offer)
    {
        (string slot, int level, int rarity, double _) = _offers[offer];
        double now = _pieces.TryGetValue((kind, slot), out (int Level, int Rarity, int Upgrades) piece) ? Value(slot, piece.Level, piece.Rarity, piece.Upgrades) : 0;
        return Value(slot, level, rarity, 0) - now;
    }

    public bool Upgrade(FrontierGame game, FighterKind kind, string slot)
    {
        double cost = UpgradeCost(game.State, kind, slot);
        if (game.State.Gold < cost)
        {
            return false;
        }

        game.State.Gold -= cost;
        (int level, int rarity, int upgrades) = _pieces[(kind, slot)];
        _pieces[(kind, slot)] = (level, rarity, upgrades + 1);
        game.Equip(kind, GearOf(kind), GearLook.None);
        return true;
    }

    public bool Buy(FrontierGame game, FighterKind kind, int offer)
    {
        (string slot, int level, int rarity, double price) = _offers[offer];
        if (game.State.Gold < price)
        {
            return false;
        }

        game.State.Gold -= price;
        _offers.RemoveAt(offer);
        _pieces[(kind, slot)] = (level, rarity, 0);
        game.Equip(kind, GearOf(kind), GearLook.None);
        return true;
    }

    public Gear GearOf(FighterKind kind) => Gear.Of(_pieces.Where(pair => pair.Key.Kind == kind)
        .Select(pair => (Damages(pair.Key.Slot) ? Items.Damage : Items.Health, Value(pair.Key.Slot, pair.Value.Level, pair.Value.Rarity, pair.Value.Upgrades))));

    public static bool Damages(string slot) => slot != Items.Armor;

    private static int Rarity(double roll)
    {
        double total = 0;
        for (int rank = 0; rank < Odds.Length; rank++)
        {
            total += Odds[rank];
            if (roll < total)
            {
                return rank;
            }
        }

        return Odds.Length - 1;
    }

    private static double Value(string slot, int level, int rarity, int upgrades) =>
        Items.Upgraded((Damages(slot) ? DamageBase : HealthBase) * Factors[rarity] * (1 + Math.Min(level, 1000) / 100.0), upgrades);
}
