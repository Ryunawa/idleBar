using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Actives
{
    public static double Cooldown(FrontierState state, FighterKind kind) =>
        Roles.Of(state.ClassOf(kind)).Cooldown / (kind == FighterKind.Hero ? 1 : Balance.CompanionShare) / (1 + Math.Min(Balance.RechargeCap, state.Gears[(int)kind].Recharge));

    public static double Charge(FrontierState state, FighterKind kind) =>
        1 - Math.Clamp(state.ActiveTimers[(int)kind] / Cooldown(state, kind), 0, 1);

    public static void Update(FrontierState state, Monster monster, double dt, EventLog events)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            int index = (int)fighter.Kind;
            state.Guards[index] = Math.Max(0, state.Guards[index] - dt);
            if (!Vitality.Up(state, fighter.Kind))
            {
                continue;
            }

            state.ActiveTimers[index] = Math.Max(0, state.ActiveTimers[index] - dt);
            if (state.ActiveTimers[index] <= 0 && state.Monster == monster)
            {
                Cast(state, fighter.Kind, monster, events);
                state.ActiveTimers[index] = Cooldown(state, fighter.Kind);
            }
        }
    }

    public static void Stagger(FrontierState state)
    {
        for (int index = 0; index < state.ActiveTimers.Length; index++)
        {
            state.ActiveTimers[index] = index * Balance.ActiveStagger;
        }
    }

    private static void Cast(FrontierState state, FighterKind kind, Monster monster, EventLog events)
    {
        HeroRole role = state.ClassOf(kind);
        double hit = Economy.Force(state, kind) * Roles.Of(role).Interval / Talents.Speed(state);
        double xp = Balance.XpActive * Cooldown(state, kind);
        HitSource source = Blows.Source(kind);
        int raised = 0;
        switch (role)
        {
            case HeroRole.Warrior:
                Battle.Damage(state, hit * Balance.StunHitFactor, source, events, big: true);
                monster.StrikeIn += Balance.StunSeconds;
                break;
            case HeroRole.Archer:
                for (int shot = 0; shot < Balance.VolleyShots && state.Monster == monster; shot++)
                {
                    Battle.Damage(state, hit * Balance.CriticalFactor, source, events, true);
                }

                break;
            case HeroRole.Mage:
                Battle.Damage(state, hit * Balance.BlastFactor, source, events, big: true);
                break;
            case HeroRole.Tank:
                state.Guards[(int)kind] = Balance.GuardSeconds;
                xp = 0;
                break;
            default:
                raised = Raise(state, events);
                xp += Balance.XpRevive * raised;
                Skirmish.Pray(state, kind);
                break;
        }

        events.Add(new ActiveUsed(kind, role, raised));
        Experience.Gain(state, kind, xp, monster.Stage, events);
    }

    private static int Raise(FrontierState state, EventLog events)
    {
        List<Combatant> fallen = [.. Team.Members(state).Where(member => !Team.Up(state, member))];
        foreach (Combatant member in fallen)
        {
            Team.Down(state, member, 0);
            Team.SetHealth(state, member, 1);
            events.Add(new Revived(member.Kind, member.AllyId));
        }

        return fallen.Count;
    }
}
