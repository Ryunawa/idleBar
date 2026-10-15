using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed record GearPiece(
    string Slot, HeroRole? Role, int Level, int Rarity, string StatA, double RollA, string? StatB, double RollB, int Stage, bool Bought, int Upgrades = 0)
{
    public double Rank => Level + 6 * Rarity;

    public static GearPiece Draw(Random random, FrontierState state, IReadOnlyList<int> odds, bool bought)
    {
        int rarity = ItemRolls.Rarity(odds, random.NextDouble());
        string slot = Items.Slots[random.Next(Items.Slots.Count)];
        int level = ItemRolls.Level(Math.Max(1, Math.Min(state.Fighters.Max(), state.BestStage + 10)), random);
        return Roll(random, slot, level, rarity, Math.Max(1, state.MaxStage), bought);
    }

    public static GearPiece Roll(Random random, string slot, int level, int rarity, int stage, bool bought)
    {
        HeroRole? role = slot == Items.Weapon ? Roles.All[random.Next(Roles.All.Count)].Role : null;
        IReadOnlyList<string> pool = ItemStats.Pool(slot, role);
        string first = pool[random.Next(pool.Count)];
        string[] rest = [.. pool.Where(stat => stat != first)];
        string? second = rarity > 0 && rest.Length > 0 ? rest[random.Next(rest.Length)] : null;
        return new GearPiece(slot, role, level, rarity, first, random.NextDouble(), second, random.NextDouble(), stage, bought);
    }

    public bool Fits(FrontierState state, FighterKind kind) =>
        state.Level(kind) > 0 && Level <= state.Level(kind) && (Role is not HeroRole role || role == state.ClassOf(kind));

    public IEnumerable<(string Stat, double Percent)> Stats(int extra = 0)
    {
        yield return (StatA, Items.Upgraded(ItemRolls.Value(StatA, Rarity, Level, RollA), Upgrades + extra));
        if (StatB is string second)
        {
            yield return (second, Items.Upgraded(ItemRolls.Value(second, Rarity, Level, RollB, true), Upgrades + extra));
        }
    }
}
