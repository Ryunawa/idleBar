using System;

namespace IdleBar.Frontier;

public static class Blows
{
    public static void Swing(FrontierState state, FighterKind kind, Monster monster, double amount, EventLog events)
    {
        int index = (int)kind;
        HeroRole role = state.ClassOf(kind);
        state.CritMeters[index] += (role == HeroRole.Archer ? 100 / Balance.CriticalEvery : 0) + (int)Math.Round(state.Gears[index].Critical * 100);
        bool critical = state.CritMeters[index] >= 100;
        if (critical)
        {
            state.CritMeters[index] = Math.Min(state.CritMeters[index] - 100, 99);
        }

        bool signature = Strike(state, role, ++state.Strikes[index], critical, monster, amount, Source(kind), 0, events);
        Experience.Gain(state, kind, signature ? Experience.Signature(role) : Balance.XpSmall, monster.Stage, events);
    }

    public static void Assist(FrontierState state, Ally ally, Monster monster, double amount, EventLog events)
    {
        ally.Shots++;
        bool critical = ally.Role == HeroRole.Archer && ally.Shots % Balance.CriticalEvery == 0;
        Strike(state, ally.Role, ally.Shots, critical, monster, amount, HitSource.Ally, ally.Id, events);
    }

    private static bool Strike(FrontierState state, HeroRole role, long strike, bool critical, Monster monster, double amount, HitSource source, long ally, EventLog events)
    {
        bool mage = role == HeroRole.Mage;
        bool big = mage && strike % Balance.SpellEvery == 0;
        bool stunned = role == HeroRole.Warrior && strike % Balance.StunEvery == 0 && Skirmish.Stun(monster);
        double share = mage ? big ? Balance.BigSpell : Balance.SmallSpell : 1;
        double factor = share * (critical ? Balance.CriticalFactor : 1) * (mage && monster.Boss ? Balance.MageBossFactor : 1);
        Battle.Damage(state, amount * factor, source, events, critical, ally, big || stunned);
        return role switch
        {
            HeroRole.Warrior => stunned,
            HeroRole.Archer => critical,
            HeroRole.Mage => big,
            _ => false,
        };
    }

    private static HitSource Source(FighterKind kind) => kind switch
    {
        FighterKind.Hero => HitSource.Hero,
        FighterKind.First => HitSource.First,
        _ => HitSource.Second,
    };
}
