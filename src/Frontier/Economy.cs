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
            FighterKind.Mage => dps * (1 + Balance.TowerBonus * state.Level(BuildingKind.Tower)),
            _ => dps,
        };
    }

    public static double Buff(FrontierState state) =>
        (state.FuryLeft > 0 ? Balance.FuryFactor : 1) * (1 + Balance.SupplyBonus * state.Supply);

    public static double Dps(FrontierState state, FighterKind kind) => BaseDps(state, kind, state.Level(kind)) * Buff(state);

    public static double TotalDps(FrontierState state)
    {
        double total = 0;
        foreach (FighterInfo fighter in Fighters.All)
        {
            total += Dps(state, fighter.Kind);
        }

        return total;
    }

    public static double Interval(FrontierState state, FighterKind kind) =>
        Fighters.Of(kind).Interval / (kind == FighterKind.Hero && state.FuryLeft > 0 ? Balance.FuryFactor : 1);

    public static double ClickDamage(FrontierState state) => Math.Max(1, Dps(state, FighterKind.Hero) * Balance.ClickShare);

    public static double PlotGold(FrontierState state, int plot) => PlotGoldAt(state, plot, state.Levels[plot]);

    public static double PlotGoldAt(FrontierState state, int plot, int level)
    {
        double portal = 1 + Balance.PortalBonus * state.Level(BuildingKind.Portal);
        return Plots.Of(plot).Gold * level * Math.Pow(Balance.TierFactor, Tier(level)) * Wealth(state) * portal;
    }

    public static double AbsenceCap(FrontierState state) =>
        Math.Min(Balance.AbsenceMaxSeconds, Balance.AbsenceCapSeconds + state.Level(BuildingKind.Mill) * Balance.MillAbsenceSeconds);

    public static double ChickenPace(FrontierState state) =>
        1 + Math.Min(Balance.FarmChickenMax, Balance.FarmChickenBonus * state.Level(BuildingKind.Farm));

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
        BaseGoldPerSecond(state) * (state.FrenzyLeft > 0 ? Balance.FrenzyGold : 1);

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

    public static double KillGold(FrontierState state, bool boss)
    {
        double gold = (MonsterHp(state.Stage) * Balance.KillGoldRatio + Balance.KillGoldFlat)
            * Wealth(state)
            * (1 + Balance.MarketBonus * state.Level(BuildingKind.Market));
        return gold * (boss ? Balance.BossGoldFactor : 1) * (state.FrenzyLeft > 0 ? Balance.FrenzyGold : 1);
    }

    public static double FighterCost(FrontierState state, FighterKind kind) =>
        Fighters.Of(kind).Cost * Math.Pow(Balance.FighterCostGrowth, state.Level(kind));

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
