using System;
using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed class FrontierGame
{
    private readonly Random _random;
    private double _pending;
    private double _intendantTimer;
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
        Battle.Damage(State, Economy.ClickDamage(State), HitSource.Click, Events);
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

    public bool Embody(HeroLook look, HeroRole role)
    {
        if (Frozen || !look.IsValid() || (role != State.Role && !State.CanChangeRole))
        {
            return false;
        }

        if (State.Hero is null || role != State.Role)
        {
            State.RoleRetreats = State.Retreats;
        }

        State.Role = role;
        State.Hero = look;
        return true;
    }

    public void Reinforce(IReadOnlyList<Ally> allies, IReadOnlyList<Boost> boosts) => Help.Sync(State, allies, boosts, Events);

    public void SetIntendant(bool on) => State.IntendantOn = Frozen ? State.IntendantOn : on && State.IntendantUnlocked;

    public bool UseSkill(SkillKind kind)
    {
        int index = (int)kind;
        if (Frozen || !Skills.Unlocked(State, kind) || State.Cooldowns[index] > 0)
        {
            return false;
        }

        switch (kind)
        {
            case SkillKind.Smite when !Battle.Fighting(State):
                return false;
            case SkillKind.Smite:
                Events.Add(new Smote());
                double hit = Economy.HeroPower(State) * Fighters.Of(FighterKind.Hero).Interval * Balance.SmiteHits;
                Battle.Damage(State, hit, HitSource.Smite, Events);
                break;
            case SkillKind.Fury:
                State.FuryLeft = Skills.Of(kind).Duration;
                Events.Add(new FuryStarted());
                break;
            case SkillKind.GoldRain:
                double gold = Math.Max(Economy.GoldPerSecond(State) * Balance.GoldRainSeconds, Economy.KillGold(State, false) * Balance.GoldRainKills);
                State.Earn(gold);
                Events.Add(new GoldRained(gold));
                break;
        }

        State.Cooldowns[index] = Economy.SkillCooldown(State, kind);
        State.SkillsUsed++;
        return true;
    }

    private void Tick(double dt)
    {
        FrontierState state = State;
        state.Time += dt;
        state.Earn(Economy.GoldPerSecond(state) * dt);
        for (int index = 0; index < state.Cooldowns.Length; index++)
        {
            state.Cooldowns[index] = Math.Max(0, state.Cooldowns[index] - dt);
        }

        state.FuryLeft = Math.Max(0, state.FuryLeft - dt);
        state.FrenzyLeft = Math.Max(0, state.FrenzyLeft - dt);
        Help.Elapse(state, dt, Events);
        Convoys.Update(state, dt, Events);
        GoldenChicken.Update(state, dt, _random, Events);
        Battle.Update(state, dt, Events);
        if (state.IntendantOn)
        {
            _intendantTimer -= dt;
            if (_intendantTimer <= 0)
            {
                _intendantTimer = Balance.IntendantSeconds;
                Intendant.Run(state, Events);
            }
        }

        _achievementTimer -= dt;
        if (_achievementTimer <= 0)
        {
            _achievementTimer = Balance.AchievementSeconds;
            Achievements.Check(state, Events);
        }
    }
}
