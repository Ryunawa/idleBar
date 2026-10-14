using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class GearCase
{
    private const double DamageBase = 4;
    private const double HealthBase = 6;

    private readonly Dictionary<(FighterKind Kind, string Slot), (int Level, int Upgrades)> _pieces = [];

    public int Count => _pieces.Count;

    public bool Has(FighterKind kind, string slot) => _pieces.ContainsKey((kind, slot));

    public double Cost(FrontierState state, FighterKind kind, string slot) =>
        _pieces.TryGetValue((kind, slot), out (int Level, int Upgrades) piece) ? GearPrices.Upgrade(state, 0, piece.Upgrades, 1) : GearPrices.Buy(state, 0);

    public double Gain(FrontierState state, FighterKind kind, string slot)
    {
        double now = _pieces.TryGetValue((kind, slot), out (int Level, int Upgrades) piece) ? Value(slot, piece.Level, piece.Upgrades) : 0;
        double next = Has(kind, slot) ? Value(slot, piece.Level, piece.Upgrades + 1) : Value(slot, state.MaxStage, 0);
        return next - now;
    }

    public bool Buy(FrontierGame game, FighterKind kind, string slot)
    {
        FrontierState state = game.State;
        double cost = Cost(state, kind, slot);
        if (state.Gold < cost)
        {
            return false;
        }

        state.Gold -= cost;
        _pieces[(kind, slot)] = _pieces.TryGetValue((kind, slot), out (int Level, int Upgrades) piece) ? (piece.Level, piece.Upgrades + 1) : (state.MaxStage, 0);
        game.Equip(kind, GearOf(kind), GearLook.None);
        return true;
    }

    public Gear GearOf(FighterKind kind) => Gear.Of(_pieces.Where(pair => pair.Key.Kind == kind)
        .Select(pair => (pair.Key.Slot == Items.Armor ? Items.Health : Items.Damage, Value(pair.Key.Slot, pair.Value.Level, pair.Value.Upgrades))));

    public static bool Damages(string slot) => slot != Items.Armor;

    private static double Value(string slot, int level, int upgrades) =>
        Items.Upgraded((slot == Items.Armor ? HealthBase : DamageBase) * (1 + System.Math.Min(level, 1000) / 100.0), upgrades);
}
