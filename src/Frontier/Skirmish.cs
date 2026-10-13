using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Skirmish
{
    private const double Golden = 0.6180339887498949;
    private const double Spread = 0.37;

    public static void Recover(FrontierState state, double dt, EventLog events)
    {
        foreach (Combatant member in Team.Members(state))
        {
            double downed = member.Ally?.Downed ?? state.Downed[(int)member.Kind];
            if (downed <= 0)
            {
                continue;
            }

            downed = Math.Max(0, downed - dt);
            Team.Down(state, member, downed);
            if (downed <= 0)
            {
                Team.SetHealth(state, member, 1);
                events.Add(new Revived(member.Kind, member.AllyId));
            }
        }
    }

    public static void Update(FrontierState state, Monster monster, double dt, EventLog events)
    {
        Heal(state, dt, events);
        monster.StrikeIn -= dt;
        while (monster.StrikeIn <= 0 && state.Monster == monster)
        {
            monster.StrikeIn += Vitality.StrikeSeconds(monster.Boss);
            Strike(state, monster, events);
        }
    }

    public static void Stun(Monster monster) => monster.StrikeIn += Balance.StunSeconds;

    public static void Rest(FrontierState state) => Mend(state, Balance.KillHeal);

    public static void Restore(FrontierState state)
    {
        Array.Fill(state.Health, 1);
        Array.Clear(state.Downed);
        foreach (Ally ally in state.Allies)
        {
            ally.Health = 1;
            ally.Downed = 0;
        }

        state.HealTimer = 0;
    }

    public static Combatant? Target(FrontierState state, Monster monster)
    {
        if (Team.Tank(state) is Combatant tank)
        {
            return tank;
        }

        List<Combatant> members = [.. Team.Members(state).Where(member => Team.Up(state, member))];
        double pick = (monster.Id * Spread + monster.Strikes * Golden) % 1 * members.Sum(member => Weight(state, member));
        foreach (Combatant member in members)
        {
            pick -= Weight(state, member);
            if (pick < 0)
            {
                return member;
            }
        }

        return members.Count > 0 ? members[^1] : null;
    }

    private static double Weight(FrontierState state, Combatant member) =>
        Team.Ranged(state, member) ? Balance.RangedWeight : Balance.MeleeWeight;

    private static void Heal(FrontierState state, double dt, EventLog events)
    {
        if (!Team.SupportUp(state))
        {
            state.HealTimer = 0;
            return;
        }

        state.HealTimer += dt;
        if (state.HealTimer >= Balance.HealSeconds)
        {
            state.HealTimer -= Balance.HealSeconds;
            Mend(state, Balance.HealShare);
            events.Add(new Healed());
        }
    }

    private static void Mend(FrontierState state, double share)
    {
        foreach (Combatant member in Team.Members(state))
        {
            if (Team.Up(state, member))
            {
                Team.SetHealth(state, member, Math.Min(1, Team.Health(state, member) + share));
            }
        }
    }

    private static void Strike(FrontierState state, Monster monster, EventLog events)
    {
        monster.Strikes++;
        if (Target(state, monster) is not Combatant target)
        {
            return;
        }

        double damage = Vitality.MonsterHit(monster.Stage, monster.Boss);
        double health = Team.Health(state, target) - damage / Team.MaxHp(state, target);
        Team.SetHealth(state, target, Math.Max(0, health));
        events.Add(new Struck(target.Kind, damage, monster.Boss, target.AllyId));
        if (health > 0)
        {
            return;
        }

        Team.Down(state, target, Fighters.Of(target.Kind).Revive);
        double seconds = Team.ReviveSeconds(state, target);
        Team.Down(state, target, seconds);
        events.Add(new Fell(target.Kind, seconds, target.AllyId));
        if (!Team.AnyUp(state))
        {
            Rout(state, monster, events);
        }
    }

    private static void Rout(FrontierState state, Monster monster, EventLog events)
    {
        Restore(state);
        Front.Recede(state, monster.Boss);
        events.Add(new Routed(state.Stage, monster.Boss));
    }
}
