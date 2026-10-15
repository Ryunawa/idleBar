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
        monster.StrikeIn -= dt;
        while (monster.StrikeIn <= 0 && state.Monster == monster)
        {
            monster.StrikeIn += Vitality.StrikeSeconds(monster.Boss) / (1 - Talents.Intimidation(state));
            Strike(state, monster, events);
        }
    }

    public static void Rest(FrontierState state) => Mend(state, member => Balance.KillHeal * (1 + Team.Gear(state, member).Healing));

    public static void Pray(FrontierState state, FighterKind healer)
    {
        double amount = Balance.PrayerHeal * Vitality.MaxHp(state, healer) * (1 + state.Gears[(int)healer].Healing);
        Mend(state, member => amount / Team.MaxHp(state, member));
    }

    public static void Touch(FrontierState state, double healerHp, double healing, EventLog events)
    {
        Combatant? weakest = null;
        foreach (Combatant member in Team.Members(state))
        {
            if (Team.Up(state, member) && Team.Health(state, member) < 1 && (weakest is not Combatant other || Team.Health(state, member) < Team.Health(state, other)))
            {
                weakest = member;
            }
        }

        if (weakest is Combatant target)
        {
            double amount = Balance.ClericTouch * healerHp * (1 + healing);
            Team.SetHealth(state, target, Math.Min(1, Team.Health(state, target) + amount / Team.MaxHp(state, target)));
            events.Add(new Mended(target.Kind, target.AllyId));
        }
    }

    public static void Restore(FrontierState state)
    {
        Array.Fill(state.Health, 1);
        Array.Clear(state.Downed);
        foreach (Ally ally in state.Allies)
        {
            ally.Health = 1;
            ally.Downed = 0;
        }
    }

    public static Combatant? Target(FrontierState state, Monster monster)
    {
        if (Talents.Guardian(state, monster) is Combatant guardian)
        {
            return guardian;
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

    private static double Mend(FrontierState state, Func<Combatant, double> share)
    {
        double restored = 0;
        foreach (Combatant member in Team.Members(state))
        {
            if (Team.Up(state, member))
            {
                double health = Team.Health(state, member);
                double healed = Math.Min(1, health + share(member));
                restored += healed - health;
                Team.SetHealth(state, member, healed);
            }
        }

        return restored;
    }

    private static void Strike(FrontierState state, Monster monster, EventLog events)
    {
        monster.Strikes++;
        if (state.ShieldLeft > 0 || Target(state, monster) is not Combatant target)
        {
            return;
        }

        double damage = Vitality.MonsterHit(monster.Stage, monster.Boss) * (target.Ally is null && state.Guards[(int)target.Kind] > 0 ? Balance.GuardShare : 1);
        double health = Team.Health(state, target) - damage / Team.MaxHp(state, target);
        Team.SetHealth(state, target, Math.Max(0, health));
        events.Add(new Struck(target.Kind, damage, monster.Boss, target.AllyId));
        if (target.Ally is null && state.ClassOf(target.Kind) == HeroRole.Tank)
        {
            Experience.Gain(state, target.Kind, monster.Boss ? Balance.XpBossTaken : Balance.XpTaken, monster.Stage, events);
        }

        if (health > 0)
        {
            return;
        }

        double lost = target.Ally is null ? Experience.Lose(state, target.Kind, Balance.FallXp) : 0;
        double seconds = Team.ReviveSeconds(state, target);
        Team.Down(state, target, seconds);
        events.Add(new Fell(target.Kind, seconds, target.AllyId, lost));
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
