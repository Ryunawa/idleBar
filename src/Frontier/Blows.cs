using System;

namespace IdleBar.Frontier;

public static class Blows
{
    public static void Swing(FrontierState state, FighterKind kind, Monster monster, double amount, EventLog events)
    {
        int index = (int)kind;
        state.CritMeters[index] += (int)Math.Round(state.Gears[index].Critical * 100);
        bool critical = state.CritMeters[index] >= 100;
        if (critical)
        {
            state.CritMeters[index] = Math.Min(state.CritMeters[index] - 100, 99);
        }

        Battle.Damage(state, amount * (critical ? Balance.CriticalFactor : 1), Source(kind), events, critical);
        if (state.ClassOf(kind) == HeroRole.Support)
        {
            Skirmish.Touch(state, Vitality.MaxHp(state, kind), state.Gears[index].Healing, events);
        }

        Experience.Gain(state, kind, Balance.XpSmall, monster.Stage, events);
    }

    public static void Assist(FrontierState state, Ally ally, double amount, EventLog events)
    {
        Battle.Damage(state, amount, HitSource.Ally, events, ally: ally.Id);
        if (ally.Role == HeroRole.Support)
        {
            Skirmish.Touch(state, Vitality.AllyMaxHp(state, ally), 0, events);
        }
    }

    public static HitSource Source(FighterKind kind) => kind switch
    {
        FighterKind.Hero => HitSource.Hero,
        FighterKind.First => HitSource.First,
        _ => HitSource.Second,
    };
}
