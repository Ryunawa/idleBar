using System;

namespace IdleBar.Frontier;

public static class Economy
{
    public static double SoulMultiplier(int souls) => 1 + Balance.SoulBonus * souls;

    public static double Wealth(FrontierState state) =>
        SoulMultiplier(state.Souls) * (1 + Balance.AchievementBonus * state.Achievements.Count);

    public static double BaseDps(FrontierState state, FighterKind kind, int level)
    {
        if (level <= 0)
        {
            return 0;
        }

        double dps = Fighters.Of(kind).Dps * level * Math.Pow(Balance.MilestoneFactor, level / Balance.MilestoneLevels) * Wealth(state);
        return kind switch
        {
            FighterKind.Hero => dps * (1 + Balance.ForgeBonus * state.Level(BuildingKind.Forge)),
            FighterKind.Second => dps * (1 + Balance.TowerBonus * state.Level(BuildingKind.Tower)),
            FighterKind.First => dps * (1 + Balance.BarracksBonus * state.Level(BuildingKind.Barracks)),
            _ => dps,
        };
    }

    public static double Synergy(FrontierState state) => 1 + Balance.SynergyBonus * Roles.OnFront(state).Count;

    public static double Buff(FrontierState state) =>
        (state.FuryLeft > 0 ? Balance.FuryFactor : 1) * (1 + Balance.SupplyBonus * state.Supply) * Synergy(state) * Help.DamageFactor(state)
        * (Team.SupportUp(state) ? 1 + Balance.SupportBlessing : 1);

    public static double HeroPower(FrontierState state) =>
        BaseDps(state, FighterKind.Hero, state.Level(FighterKind.Hero)) * Buff(state) * (1 + state.Gears[(int)FighterKind.Hero].Damage);

    public static double Dps(FrontierState state, FighterKind kind) =>
        BaseDps(state, kind, state.Level(kind)) * Buff(state) * Roles.Of(state.ClassOf(kind)).Dps * (1 + state.Gears[(int)kind].Damage);

    public static double AllyDps(FrontierState state, Ally ally) =>
        BaseDps(state, FighterKind.Hero, state.Level(FighterKind.Hero)) * ally.Ratio * Roles.Of(ally.Role).Dps * Buff(state);

    public static double TotalDps(FrontierState state)
    {
        double total = 0;
        foreach (FighterInfo fighter in Fighters.All)
        {
            total += Dps(state, fighter.Kind);
        }

        foreach (Ally ally in state.Allies)
        {
            total += AllyDps(state, ally);
        }

        return total;
    }

    public static double Interval(FrontierState state, FighterKind kind) =>
        Roles.Of(state.ClassOf(kind)).Interval / (kind == FighterKind.Hero && state.FuryLeft > 0 ? Balance.FuryFactor : 1) / (1 + state.Gears[(int)kind].Speed);

    public static double ClickDamage(FrontierState state) =>
        Math.Max(1, HeroPower(state) * Balance.ClickShare * (1 + Balance.TavernBonus * state.Level(BuildingKind.Tavern)));

    public static double SkillCooldown(FrontierState state, SkillKind kind) =>
        Skills.Of(kind).Cooldown / (1 + Math.Min(Balance.ChapelMax, Balance.ChapelBonus * state.Level(BuildingKind.Chapel)));

    public static double BossSeconds(FrontierState state) =>
        Balance.BossSeconds + Math.Min(Balance.BelfryMax, Balance.BelfrySeconds * state.Level(BuildingKind.Belfry));

    public static double PlotGold(FrontierState state, int plot) => PlotGoldAt(state, plot, state.Levels[plot]);

    public static double PlotGoldAt(FrontierState state, int plot, int level) =>
        Plots.Of(plot).Gold * level * Math.Pow(Balance.TierFactor, Tier(level)) * Wealth(state) * Portal(state);

    public static double Portal(FrontierState state) => 1 + Balance.PortalBonus * state.Level(BuildingKind.Portal);

    public static double AbsenceCap(FrontierState state) =>
        Math.Min(Balance.AbsenceMaxSeconds, Balance.AbsenceCapSeconds + state.Level(BuildingKind.Mill) * Balance.MillAbsenceSeconds);

    public static double FrontGoldPerSecond(FrontierState state) =>
        BaseKillGold(state, false) / (MonsterHp(state.Stage) / Math.Max(1e-9, TotalDps(state)) + Balance.RespawnSeconds + Balance.ApproachSeconds);

    public static double AbsenceGoldPerSecond(FrontierState state) =>
        BaseGoldPerSecond(state) + Balance.AbsenceFrontShare * FrontGoldPerSecond(state);

    public static double ChickenPace(FrontierState state) =>
        1 + Math.Min(Balance.FarmChickenMax, Balance.FarmChickenBonus * state.Level(BuildingKind.Farm));

    public static double ConvoyPace(FrontierState state) =>
        1 + Math.Min(Balance.BakeryMax, Balance.BakeryBonus * state.Level(BuildingKind.Bakery));

    public static double BaseGoldPerSecond(FrontierState state)
    {
        double total = 0;
        for (int plot = 0; plot < FrontierState.PlotCount; plot++)
        {
            total += PlotGold(state, plot);
        }

        return total;
    }

    public static double GoldPerSecond(FrontierState state) =>
        BaseGoldPerSecond(state) * (state.FrenzyLeft > 0 ? Balance.FrenzyGold : 1) * Help.GoldFactor(state);

    public static double MonsterHp(int stage)
    {
        double exponent = 0;
        int from = 1;
        foreach (GrowthSegment segment in Balance.HpCurve)
        {
            if (stage <= from)
            {
                break;
            }

            int to = Math.Min(stage, segment.UpTo);
            exponent += (to - from) * Math.Log(segment.Growth);
            from = to;
        }

        return Balance.MonsterHpBase * Math.Exp(exponent);
    }

    public static bool IsBossStage(int stage) => stage % Balance.BossEvery == 0;

    public static double KillGold(FrontierState state, bool boss) =>
        BaseKillGold(state, boss) * (state.FrenzyLeft > 0 ? Balance.FrenzyGold : 1) * Help.GoldFactor(state);

    public static double BaseKillGold(FrontierState state, bool boss) =>
        (MonsterHp(state.Stage) * Balance.KillGoldRatio + Balance.KillGoldFlat)
            * Wealth(state)
            * (1 + Balance.MarketBonus * state.Level(BuildingKind.Market))
            * Portal(state)
            * (boss ? Balance.BossGoldFactor : 1);

    public static double ArmorCost(FrontierState state) =>
        Balance.ArmorCost * Math.Pow(Balance.FighterCostGrowth, state.Armor) * Discount(state);

    public static double Discount(FrontierState state) =>
        1 - Math.Min(Balance.WorkshopMax, Balance.WorkshopDiscount * state.Level(BuildingKind.Workshop));

    public static double PlotCost(FrontierState state, int plot) =>
        Plots.Of(plot).Cost * Math.Pow(Balance.BuildingCostGrowth, state.Levels[plot]);

    public static bool Unlocked(FrontierState state, int plot) => state.MaxStage >= Plots.Of(plot).Unlock;

    public static int Tier(int level)
    {
        int tier = 0;
        foreach (int threshold in Balance.Tiers)
        {
            if (level >= threshold)
            {
                tier++;
            }
        }

        return tier;
    }
}
