using System;

namespace IdleBar.Frontier;

public static class Battle
{
    public static bool Fighting(FrontierState state) => state.Monster is { Approach: <= 0, Hp: > 0 };

    public static void Update(FrontierState state, double dt, EventLog events)
    {
        Skirmish.Recover(state, dt, events);
        Monster? monster = state.Monster;
        if (monster is null)
        {
            Charge(state, dt);
            state.RespawnIn -= dt;
            if (state.RespawnIn <= 0)
            {
                Spawn(state);
            }

            return;
        }

        if (monster.Approach > 0)
        {
            Charge(state, dt);
            monster.Approach -= dt;
            return;
        }

        if (monster.Boss)
        {
            monster.BossLeft -= dt;
            if (monster.BossLeft <= 0)
            {
                Front.Fail(state, events);
                return;
            }
        }

        foreach (FighterInfo fighter in Fighters.All)
        {
            Attack(state, fighter.Kind, monster, dt, events);
        }

        foreach (Ally ally in state.Allies)
        {
            Assist(state, ally, monster, dt, events);
        }

        if (state.Monster == monster)
        {
            Actives.Update(state, monster, dt, events);
        }

        if (state.Monster == monster)
        {
            Skirmish.Update(state, monster, dt, events);
        }
    }

    public static void Damage(FrontierState state, double amount, HitSource source, EventLog events, bool critical = false, long ally = 0, bool big = false)
    {
        if (!Fighting(state) || state.Monster is not Monster monster)
        {
            return;
        }

        amount *= monster.Boss ? 1 + Talents.BossDamage(state) : 1;
        monster.Hp -= amount;
        events.Add(new Hit(source, amount, critical, ally, big));
        if (monster.Hp <= 0)
        {
            Kill(state, monster, events);
        }
    }

    private static void Attack(FrontierState state, FighterKind kind, Monster monster, double dt, EventLog events)
    {
        if (!Vitality.Up(state, kind))
        {
            return;
        }

        int index = (int)kind;
        double interval = Economy.Interval(state, kind);
        state.Timers[index] += dt;
        while (state.Timers[index] >= interval && state.Monster == monster)
        {
            state.Timers[index] -= interval;
            Blows.Swing(state, kind, monster, Economy.Dps(state, kind) * interval, events);
        }
    }

    private static void Assist(FrontierState state, Ally ally, Monster monster, double dt, EventLog events)
    {
        if (ally.Downed > 0)
        {
            return;
        }

        double interval = Roles.Of(ally.Role).Interval / Talents.Speed(state);
        ally.Timer += dt;
        while (ally.Timer >= interval && state.Monster == monster)
        {
            ally.Timer -= interval;
            Blows.Assist(state, ally, Economy.AllyDps(state, ally) * interval, events);
        }
    }

    private static void Kill(FrontierState state, Monster monster, EventLog events)
    {
        double gold = Economy.KillGold(state, monster.Boss);
        state.Earn(gold);
        state.MonstersKilled++;
        events.Add(new Killed(gold, monster.Boss));
        state.Monster = null;
        state.RespawnIn = Balance.RespawnSeconds;
        Skirmish.Rest(state);
        if (monster.Boss)
        {
            state.BossesKilled++;
            Front.Advance(state, events);
            return;
        }

        state.Kills++;
        if (state.Kills >= Balance.MonstersPerStage && state.BossHeld && Economy.IsBossStage(state.Stage + 1))
        {
            state.Kills = 0;
        }
        else if (state.Kills >= Balance.MonstersPerStage)
        {
            Front.Advance(state, events);
        }
    }

    private static void Spawn(FrontierState state)
    {
        bool boss = Economy.IsBossStage(state.Stage);
        double hp = Economy.MonsterHp(state.Stage) * (boss ? Balance.BossHpFactor : 1);
        int template = boss ? Biomes.BossTemplate(state.Stage) : Biomes.Template(state.Stage, state.Kills);
        state.MonsterSequence++;
        state.Monster = new Monster(state.MonsterSequence, template, boss, state.Stage, hp, boss ? Balance.BossApproachSeconds : Balance.ApproachSeconds, Economy.BossSeconds(state));
    }

    private static void Charge(FrontierState state, double dt)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            int index = (int)fighter.Kind;
            state.Timers[index] = Math.Min(state.Timers[index] + dt, Economy.Interval(state, fighter.Kind) * Balance.ChargeShare);
        }

        foreach (Ally ally in state.Allies)
        {
            ally.Timer = Math.Min(ally.Timer + dt, Roles.Of(ally.Role).Interval / Talents.Speed(state) * Balance.ChargeShare);
        }
    }
}
