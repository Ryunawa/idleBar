using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Experience
{
    public static double Unit(FrontierState state, int stage) => (Economy.MonsterHp(stage) * Balance.XpRatio + Balance.XpFlat) * Economy.Wealth(state);

    public static double ToNext(FrontierState state, FighterKind kind) =>
        Fighters.Of(kind).Cost * Math.Pow(Balance.XpGrowth, state.Level(kind));

    public static double Share(FrontierState state, FighterKind kind) =>
        state.Level(kind) <= 0 ? 0 : Math.Clamp(state.Xp[(int)kind] / ToNext(state, kind), 0, 1);

    public static double Signature(HeroRole role) => role switch
    {
        HeroRole.Warrior => Balance.XpStun,
        HeroRole.Archer => Balance.XpCritical,
        HeroRole.Mage => Balance.XpBigSpell,
        _ => Balance.XpSmall,
    };

    public static void Rescue(FrontierState state, Combatant revived, EventLog events)
    {
        foreach (FighterKind kind in Supports(state))
        {
            if (revived.Ally is not null || revived.Kind != kind)
            {
                Gain(state, kind, Balance.XpRevive, state.Stage, events);
            }
        }
    }

    public static void Mended(FrontierState state, double restored, EventLog events)
    {
        foreach (FighterKind kind in Supports(state))
        {
            Gain(state, kind, Balance.XpHeal * restored / Balance.HealShare, state.Stage, events);
        }
    }

    public static void Gain(FrontierState state, FighterKind kind, double weight, int stage, EventLog events)
    {
        int index = (int)kind;
        if (state.Level(kind) <= 0 || weight <= 0)
        {
            return;
        }

        state.Xp[index] += Unit(state, stage) * weight;
        int before = state.Level(kind);
        while (state.Xp[index] >= ToNext(state, kind))
        {
            state.Xp[index] -= ToNext(state, kind);
            state.Fighters[index]++;
        }

        if (state.Level(kind) > before)
        {
            events.Add(new LeveledUp(kind, state.Level(kind)));
        }
    }

    private static IEnumerable<FighterKind> Supports(FrontierState state) =>
        Fighters.All.Select(fighter => fighter.Kind).Where(kind => state.ClassOf(kind) == HeroRole.Support && Vitality.Up(state, kind));
}
