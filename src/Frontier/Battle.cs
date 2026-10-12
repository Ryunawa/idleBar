using System;

namespace IdleBar.Frontier;

public static class Battle
{
    public static bool Fighting(FrontierState state) => state.Monster is { Approach: <= 0, Hp: > 0 };

    public static void Update(FrontierState state, double dt, EventLog events)
    {
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
                Fail(state, events);
                return;
            }
        }

        foreach (FighterInfo fighter in Fighters.All)
        {
            Attack(state, fighter.Kind, monster, dt, events);
        }
    }

    public static void Damage(FrontierState state, double amount, HitSource source, EventLog events)
    {
        if (!Fighting(state) || state.Monster is not Monster monster)
        {
            return;
        }

        monster.Hp -= amount;
        events.Add(new Hit(source, amount));
        if (monster.Hp <= 0)
        {
            Kill(state, monster, events);
        }
    }

    private static void Attack(FrontierState state, FighterKind kind, Monster monster, double dt, EventLog events)
    {
        if (state.Level(kind) <= 0)
        {
            return;
        }

        int index = (int)kind;
        double interval = Economy.Interval(state, kind);
        HitSource source = kind switch
        {
            FighterKind.Archer => HitSource.Archer,
            FighterKind.Mage => HitSource.Mage,
            _ => HitSource.Hero,
        };
        state.Timers[index] += dt;
        while (state.Timers[index] >= interval && state.Monster == monster)
        {
            state.Timers[index] -= interval;
            Damage(state, Economy.Dps(state, kind) * interval, source, events);
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
        if (monster.Boss)
        {
            state.BossesKilled++;
            Advance(state, events);
            return;
        }

        state.Kills++;
        if (state.Kills >= Balance.MonstersPerStage && state.BossHeld)
        {
            state.Kills = 0;
        }
        else if (state.Kills >= Balance.MonstersPerStage)
        {
            Advance(state, events);
        }
    }

    public static bool CallBoss(FrontierState state, EventLog events)
    {
        if (!state.BossHeld)
        {
            return false;
        }

        state.BossHeld = false;
        events.Add(new BossCalled());
        state.Monster = null;
        state.RespawnIn = Balance.RespawnSeconds;
        Advance(state, events);
        return true;
    }

    private static void Advance(FrontierState state, EventLog events)
    {
        state.Stage++;
        state.Kills = 0;
        if (state.Stage > state.MaxStage)
        {
            state.MaxStage = state.Stage;
            state.BestStage = Math.Max(state.BestStage, state.Stage);
            foreach (BuildingInfo building in Buildings.All)
            {
                if (building.Unlock == state.MaxStage)
                {
                    events.Add(new SlotConquered(building.Kind));
                }
            }
        }

        events.Add(new StageReached(state.Stage, Economy.IsBossStage(state.Stage)));
    }

    private static void Fail(FrontierState state, EventLog events)
    {
        events.Add(new BossFailed());
        state.Monster = null;
        state.RespawnIn = Balance.BossRetrySeconds;
        state.Stage = Math.Max(1, state.Stage - 1);
        state.Kills = 0;
        state.BossHeld = true;
    }

    private static void Spawn(FrontierState state)
    {
        bool boss = Economy.IsBossStage(state.Stage);
        double hp = Economy.MonsterHp(state.Stage) * (boss ? Balance.BossHpFactor : 1);
        int template = boss ? Biomes.BossTemplate(state.Stage) : Biomes.Template(state.Stage, state.Kills);
        state.MonsterSequence++;
        state.Monster = new Monster(state.MonsterSequence, template, boss, state.Stage, hp, boss ? Balance.BossApproachSeconds : Balance.ApproachSeconds);
    }

    private static void Charge(FrontierState state, double dt)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            int index = (int)fighter.Kind;
            state.Timers[index] = Math.Min(state.Timers[index] + dt, Economy.Interval(state, fighter.Kind) * Balance.ChargeShare);
        }
    }
}
