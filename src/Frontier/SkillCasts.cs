using System;
using System.Linq;

namespace IdleBar.Frontier;

public static class SkillCasts
{
    public static bool Use(FrontierState state, int slot, EventLog events)
    {
        if (slot < 0 || slot >= FrontierState.SlotCount || state.Slots[slot] is not SkillKind kind || Wait(state, kind) > 0 || !Ready(state, kind))
        {
            return false;
        }

        Cast(state, kind, events);
        state.Cooldowns[(int)kind] = Economy.SkillCooldown(state, kind);
        state.SkillsUsed++;
        return true;
    }

    public static double Wait(FrontierState state, SkillKind kind) => state.Cooldowns[(int)kind];

    public static bool Ready(FrontierState state, SkillKind kind) => kind switch
    {
        SkillKind.Smite or SkillKind.Judgment => Battle.Fighting(state),
        SkillKind.Rally => Fallen(state) > 0,
        SkillKind.Lure => state.Chicken is null,
        _ => true,
    };

    public static double Duration(FrontierState state, SkillKind kind) => kind switch
    {
        SkillKind.Fury => Skills.Of(kind).Duration + Balance.RageSeconds * SkillTree.Level(state, SkillKind.LastingRage),
        SkillKind.HolyShield => Skills.Of(kind).Duration + Balance.TenacitySeconds * SkillTree.Level(state, SkillKind.Tenacity),
        _ => Skills.Of(kind).Duration,
    };

    public static double Left(FrontierState state, SkillKind kind) => kind switch
    {
        SkillKind.Fury => state.FuryLeft,
        SkillKind.Storm => state.StormLeft,
        SkillKind.HolyShield => state.ShieldLeft,
        SkillKind.Harvest => state.HarvestLeft,
        _ => 0,
    };

    public static double SmiteHits(FrontierState state) => Balance.SmiteHits + Balance.ThunderHits * SkillTree.Level(state, SkillKind.Thunder);

    public static double RainSeconds(FrontierState state) =>
        Balance.GoldRainSeconds + Balance.DownpourSeconds * SkillTree.Level(state, SkillKind.Downpour);

    public static int Fallen(FrontierState state) => Fighters.All.Count(fighter => state.Level(fighter.Kind) > 0 && !Vitality.Up(state, fighter.Kind));

    public static void Elapse(FrontierState state, double dt)
    {
        for (int node = 0; node < state.Cooldowns.Length; node++)
        {
            state.Cooldowns[node] = Math.Max(0, state.Cooldowns[node] - dt);
        }

        state.FuryLeft = Math.Max(0, state.FuryLeft - dt);
        state.StormLeft = Math.Max(0, state.StormLeft - dt);
        state.ShieldLeft = Math.Max(0, state.ShieldLeft - dt);
        state.HarvestLeft = Math.Max(0, state.HarvestLeft - dt);
    }

    public static void Stop(FrontierState state)
    {
        state.FuryLeft = 0;
        state.StormLeft = 0;
        state.ShieldLeft = 0;
        state.HarvestLeft = 0;
    }

    private static void Cast(FrontierState state, SkillKind kind, EventLog events)
    {
        double amount = 0;
        switch (kind)
        {
            case SkillKind.Smite:
                events.Add(new SkillCast(kind));
                Battle.Damage(state, Economy.HeroPower(state) * Balance.SmiteInterval * SmiteHits(state), HitSource.Smite, events);
                return;
            case SkillKind.Judgment when state.Monster is Monster monster:
                events.Add(new SkillCast(kind));
                Battle.Damage(state, monster.MaxHp * (monster.Boss ? Balance.JudgmentBossShare : Balance.JudgmentShare), HitSource.Judgment, events, big: true);
                return;
            case SkillKind.Fury:
                state.FuryLeft = Duration(state, kind);
                break;
            case SkillKind.Storm:
                state.StormLeft = Duration(state, kind);
                break;
            case SkillKind.HolyShield:
                state.ShieldLeft = Duration(state, kind);
                break;
            case SkillKind.Harvest:
                state.HarvestLeft = Duration(state, kind);
                break;
            case SkillKind.Rally:
                amount = Rally(state, events);
                break;
            case SkillKind.GoldRain:
                double seconds = RainSeconds(state);
                amount = Math.Max(Economy.GoldPerSecond(state) * seconds, Economy.KillGold(state, false) * Balance.GoldRainKills * seconds / Balance.GoldRainSeconds);
                state.Earn(amount);
                break;
            case SkillKind.Lure:
                GoldenChicken.Lure(state, events);
                break;
        }

        events.Add(new SkillCast(kind, amount));
    }

    private static int Rally(FrontierState state, EventLog events)
    {
        int raised = 0;
        foreach (FighterInfo fighter in Fighters.All)
        {
            int index = (int)fighter.Kind;
            if (state.Level(fighter.Kind) > 0 && state.Downed[index] > 0)
            {
                state.Downed[index] = 0;
                state.Health[index] = Balance.RallyHealth;
                events.Add(new Revived(fighter.Kind));
                raised++;
            }
        }

        return raised;
    }
}
