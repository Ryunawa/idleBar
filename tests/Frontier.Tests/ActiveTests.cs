using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class ActiveTests
{
    private const double Step = 0.01;

    [Fact]
    public void TheWarriorStunsTheMonster()
    {
        (FrontierState state, Monster monster, IReadOnlyList<FrontierEvent> events, double before) = Cast(HeroRole.Warrior);
        Assert.Equal(before + Balance.StunSeconds, monster.StrikeIn, 6);
        Assert.Equal(Hit(state) * Balance.StunHitFactor, events.OfType<Hit>().Single(hit => hit.Big).Amount, 6);
        Assert.Equal(Balance.StunCooldown, state.ActiveTimers[0], 6);
    }

    [Fact]
    public void TheRangerVolleysFiveCriticals()
    {
        (FrontierState state, _, IReadOnlyList<FrontierEvent> events, _) = Cast(HeroRole.Archer);
        Hit[] volley = [.. events.OfType<Hit>().Where(hit => hit.Critical)];
        Assert.Equal(Balance.VolleyShots, volley.Length);
        Assert.All(volley, hit => Assert.Equal(Hit(state) * Balance.CriticalFactor, hit.Amount, 6));
    }

    [Fact]
    public void TheMageBlasts()
    {
        (FrontierState state, _, IReadOnlyList<FrontierEvent> events, _) = Cast(HeroRole.Mage);
        Assert.Equal(Hit(state) * Balance.BlastFactor, events.OfType<Hit>().Single(hit => hit.Big).Amount, 6);
    }

    [Fact]
    public void TheGuardianRaisesHisRampart()
    {
        (FrontierState state, Monster monster, _, _) = Cast(HeroRole.Tank);
        Assert.Equal(Balance.GuardSeconds, state.Guards[0], 6);
        EventLog events = new();
        Skirmish.Update(state, monster, monster.StrikeIn + Step, events);
        Assert.Equal(Vitality.MonsterHit(monster.Stage, false) * Balance.GuardShare, events.Drain().OfType<Struck>().First().Damage, 6);
    }

    [Fact]
    public void TheClericRaisesTheFallenAndHealsTheTeam()
    {
        FrontierState state = new() { Role = HeroRole.Support, Fighters = [10, 10, 10] };
        state.Downed[(int)FighterKind.First] = 20;
        state.Health[(int)FighterKind.Second] = 0.5;
        Monster monster = Front(state);
        EventLog events = new();
        Actives.Update(state, monster, Step, events);
        IReadOnlyList<FrontierEvent> raised = events.Drain();
        Assert.Single(raised.OfType<Revived>());
        Assert.Contains(raised, frontierEvent => frontierEvent is ActiveUsed { Kind: FighterKind.Hero, Raised: 1 });
        Assert.True(Vitality.Up(state, FighterKind.First));
        double share = Balance.PrayerHeal * Vitality.MaxHp(state, FighterKind.Hero) / Vitality.MaxHp(state, FighterKind.Second);
        Assert.Equal(Math.Min(1, 0.5 + share), state.Health[(int)FighterKind.Second], 6);
        Assert.Equal(Balance.RaiseCooldown, state.ActiveTimers[0], 6);
    }

    [Fact]
    public void GearSpeedDoesNotWeakenTheActives()
    {
        (FrontierState plain, _, IReadOnlyList<FrontierEvent> before, _) = Cast(HeroRole.Mage);
        (FrontierState quick, _, IReadOnlyList<FrontierEvent> after, _) = Cast(HeroRole.Mage, new Gear(0, 0, 0, 0, 1));
        Assert.True(Economy.Interval(quick, FighterKind.Hero) < Economy.Interval(plain, FighterKind.Hero));
        Assert.Equal(before.OfType<Hit>().Single(hit => hit.Big).Amount, after.OfType<Hit>().Single(hit => hit.Big).Amount, 6);
    }

    [Fact]
    public void ACompanionsActiveComesBackHalfAsOften()
    {
        FrontierState state = new() { Role = HeroRole.Warrior, Fighters = [10, 10, 0] };
        state.Companions[0] = HeroRole.Mage;
        Assert.Equal(Balance.BlastCooldown / Balance.CompanionShare, Actives.Cooldown(state, FighterKind.First), 6);
        Assert.Equal(Balance.StunCooldown, Actives.Cooldown(state, FighterKind.Hero), 6);
    }

    private static double Hit(FrontierState state) => Economy.Force(state, FighterKind.Hero) * Roles.Of(state.Role).Interval / Talents.Speed(state);

    private static (FrontierState State, Monster Monster, IReadOnlyList<FrontierEvent> Events, double Before) Cast(HeroRole role, Gear? gear = null)
    {
        FrontierState state = new() { Role = role, Fighters = [10, 0, 0], Stage = 30, MaxStage = 30 };
        state.Gears[0] = gear ?? Gear.None;
        Monster monster = Front(state);
        double before = monster.StrikeIn;
        EventLog events = new();
        Actives.Update(state, monster, Step, events);
        IReadOnlyList<FrontierEvent> drained = events.Drain();
        Assert.Contains(drained, frontierEvent => frontierEvent is ActiveUsed { Kind: FighterKind.Hero });
        return (state, monster, drained, before);
    }

    private static Monster Front(FrontierState state)
    {
        Monster monster = new(1, 0, false, state.Stage, 1e15, 0);
        state.Monster = monster;
        return monster;
    }
}
