using System;
using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed class FrontierGame
{
    private readonly Random _random;
    private double _pending;
    private double _achievementTimer;

    public FrontierGame(FrontierState state, Random random)
    {
        State = state;
        _random = random;
    }

    public FrontierState State { get; }

    public EventLog Events { get; } = new();

    public bool Frozen { get; set; }

    public void Advance(double delta)
    {
        if (delta >= Balance.LongGap)
        {
            Absence.Apply(State, delta, Events);
            return;
        }

        _pending += delta;
        while (_pending >= Balance.Step)
        {
            _pending -= Balance.Step;
            Tick(Balance.Step);
        }
    }

    public void Absent(double seconds) => Absence.Apply(State, seconds, Events);

    public bool Strike()
    {
        if (Frozen || !Battle.Fighting(State))
        {
            return false;
        }

        State.Clicks++;
        int stage = State.Stage;
        Battle.Damage(State, Economy.ClickDamage(State), HitSource.Click, Events);
        Experience.Gain(State, FighterKind.Hero, Balance.XpSmall, stage, Events);
        return true;
    }

    public bool CatchChicken() => !Frozen && GoldenChicken.Catch(State, _random, Events);

    public bool Buy(Purchase purchase) => !Frozen && Shop.Buy(State, purchase, Events);

    public int BuyMany(Purchase purchase, int count)
    {
        int bought = 0;
        while (bought < count && Buy(purchase))
        {
            bought++;
        }

        return bought;
    }

    public int Retreat() => Frozen ? 0 : RetreatRules.Apply(State, Events);

    public bool CallBoss() => !Frozen && Front.CallBoss(State, Events);

    public void Adopt(HeroLook look, HeroRole role, int roleRetreats)
    {
        State.Hero = look;
        State.Role = role;
        State.RoleRetreats = Math.Clamp(roleRetreats, 0, State.Retreats);
    }

    public void Reinforce(IReadOnlyList<Ally> allies, IReadOnlyList<Boost> boosts, bool announce = true) =>
        Help.Sync(State, allies, boosts, announce ? Events : new EventLog());

    public void Sight(TitanView titan, bool announce = true) => Titans.Sight(State, titan, announce ? Events : new EventLog());

    public void Equip(FighterKind kind, Gear gear, GearLook worn)
    {
        State.Gears[(int)kind] = gear;
        State.Worn[(int)kind] = worn;
    }

    public void Discover(int rarity, string name) => Events.Add(new LootFound(rarity, name));

    public double Recycle(int rarity, bool bought, int level)
    {
        if (Frozen)
        {
            return 0;
        }

        double gold = Items.SalvageGold(State, rarity, bought, level);
        State.Earn(gold);
        Events.Add(new Recycled(gold));
        return gold;
    }

    public bool UseSkill(int slot) => !Frozen && SkillCasts.Use(State, slot, Events);

    public bool Learn(SkillKind kind) => !Frozen && SkillTree.Learn(State, kind, Events);

    public bool Place(int slot, SkillKind? kind) => !Frozen && SkillTree.Place(State, slot, kind, Events);

    public bool Redistribute() => !Frozen && SkillTree.Redistribute(State, Events);

    private void Tick(double dt)
    {
        FrontierState state = State;
        state.Time += dt;
        state.Earn(Economy.GoldPerSecond(state) * dt);
        SkillCasts.Elapse(state, dt);
        state.FrenzyLeft = Math.Max(0, state.FrenzyLeft - dt);
        Help.Elapse(state, dt, Events);
        Convoys.Update(state, dt, Events);
        GoldenChicken.Update(state, dt, _random, Events);
        Battle.Update(state, dt, Events);
        _achievementTimer -= dt;
        if (_achievementTimer <= 0)
        {
            _achievementTimer = Balance.AchievementSeconds;
            Achievements.Check(state, Events);
        }
    }
}
