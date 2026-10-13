using System;

namespace IdleBar.Frontier;

public static class Skirmish
{
    private const double Golden = 0.6180339887498949;
    private const double Spread = 0.37;

    public static void Recover(FrontierState state, double dt, EventLog events)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            int index = (int)fighter.Kind;
            if (state.Downed[index] <= 0)
            {
                continue;
            }

            state.Downed[index] = Math.Max(0, state.Downed[index] - dt);
            if (state.Downed[index] <= 0)
            {
                state.Health[index] = 1;
                events.Add(new Revived(fighter.Kind));
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
        state.HealTimer = 0;
    }

    public static FighterKind? Target(FrontierState state, Monster monster)
    {
        if (state.Role == HeroRole.Tank && Vitality.Up(state, FighterKind.Hero))
        {
            return FighterKind.Hero;
        }

        double total = 0;
        foreach (FighterInfo fighter in Fighters.All)
        {
            total += Weight(state, fighter.Kind);
        }

        double pick = (monster.Id * Spread + monster.Strikes * Golden) % 1 * total;
        FighterKind? last = null;
        foreach (FighterInfo fighter in Fighters.All)
        {
            double weight = Weight(state, fighter.Kind);
            if (weight <= 0)
            {
                continue;
            }

            last = fighter.Kind;
            pick -= weight;
            if (pick < 0)
            {
                return fighter.Kind;
            }
        }

        return last;
    }

    private static double Weight(FrontierState state, FighterKind kind) =>
        !Vitality.Up(state, kind) ? 0 : Vitality.Ranged(state, kind) ? Balance.RangedWeight : Balance.MeleeWeight;

    private static void Heal(FrontierState state, double dt, EventLog events)
    {
        if (!Vitality.SupportUp(state))
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
        foreach (FighterInfo fighter in Fighters.All)
        {
            if (Vitality.Up(state, fighter.Kind))
            {
                int index = (int)fighter.Kind;
                state.Health[index] = Math.Min(1, state.Health[index] + share);
            }
        }
    }

    private static void Strike(FrontierState state, Monster monster, EventLog events)
    {
        monster.Strikes++;
        if (Target(state, monster) is not FighterKind target)
        {
            return;
        }

        int index = (int)target;
        double damage = Vitality.MonsterHit(monster.Stage, monster.Boss);
        state.Health[index] -= damage / Vitality.MaxHp(state, target);
        events.Add(new Struck(target, damage, monster.Boss));
        if (state.Health[index] > 0)
        {
            return;
        }

        state.Health[index] = 0;
        state.Downed[index] = Vitality.ReviveSeconds(state, target);
        events.Add(new Fell(target, state.Downed[index]));
        if (!Vitality.AnyUp(state))
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
