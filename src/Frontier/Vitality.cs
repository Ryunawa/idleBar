using System;

namespace IdleBar.Frontier;

public static class Vitality
{
    public static bool Up(FrontierState state, FighterKind kind) => state.Level(kind) > 0 && state.Downed[(int)kind] <= 0;

    public static bool AnyUp(FrontierState state) => Team.AnyUp(state);

    public static bool Ranged(FrontierState state, FighterKind kind) => Roles.Of(state.ClassOf(kind)).Ranged;

    public static double ArmorFactor(int armor) => 1 + Balance.ArmorBonus * armor;

    public static double MaxHp(FrontierState state, FighterKind kind) =>
        BaseHp(state, kind) * Roles.Of(state.ClassOf(kind)).Hp * (1 + state.Gears[(int)kind].Health);

    public static double AllyMaxHp(FrontierState state, Ally ally) =>
        BaseHp(state, FighterKind.Hero) * ally.Ratio * Roles.Of(ally.Role).Hp;

    public static double Hp(FrontierState state, FighterKind kind) => MaxHp(state, kind) * state.Health[(int)kind];

    public static double StrikeSeconds(bool boss) => boss ? Balance.BossStrikeSeconds : Balance.MonsterStrikeSeconds;

    public static double MonsterHit(int stage, bool boss) =>
        Economy.MonsterHp(stage) * (boss ? Balance.BossHit : Balance.MonsterHit) * Math.Clamp(stage / Balance.HitRampStages, Balance.HitRampFloor, 1);

    public static double ReviveSeconds(FrontierState state, FighterKind kind) =>
        Fighters.Of(kind).Revive;

    private static double BaseHp(FrontierState state, FighterKind kind)
    {
        int level = state.Level(kind);
        if (level <= 0)
        {
            return 0;
        }

        double hp = Fighters.Of(kind).Hp * level * Math.Pow(Balance.MilestoneFactor, level / Balance.MilestoneLevels)
            * Economy.Wealth(state) * ArmorFactor(state.Armor);
        return kind == FighterKind.Hero
            ? hp * (1 + Balance.ForgeBonus * state.Level(BuildingKind.Forge))
            : hp * (1 + Balance.BarracksHpBonus * state.Level(BuildingKind.Barracks));
    }
}
