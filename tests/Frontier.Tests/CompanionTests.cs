using System;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class CompanionTests
{
    [Fact]
    public void RecruitingPicksTheCompanionsClass()
    {
        FrontierState state = new() { Stage = 31, MaxStage = 31 };
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.Buy(new Recruitment(FighterKind.First, HeroRole.Support)));
        Assert.False(game.Buy(new Recruitment(FighterKind.First, HeroRole.Tank)));
        Assert.True(game.Buy(new Recruitment(FighterKind.Second, HeroRole.Mage)));
        Assert.Equal(HeroRole.Support, state.ClassOf(FighterKind.First));
        Assert.Equal(1, state.Level(FighterKind.First));
        Assert.Equal(0, state.Gold);
    }

    [Fact]
    public void CompanionsWaitForTheirBoss()
    {
        FrontierState state = new() { Stage = 10, MaxStage = 10 };
        FrontierGame game = new(state, new Random(1));
        Assert.False(game.Buy(new Recruitment(FighterKind.First, HeroRole.Archer)));
        Front.Advance(state, game.Events);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is CompanionReady { Kind: FighterKind.First });
        Assert.True(game.Buy(new Recruitment(FighterKind.First, HeroRole.Archer)));
        Assert.False(game.Buy(new Recruitment(FighterKind.Second, HeroRole.Mage)));
    }

    [Fact]
    public void TheClassShapesDamageAndHealth()
    {
        FrontierState state = new() { Fighters = [10, 10, 10] };
        double damage = Economy.Dps(state, FighterKind.Second);
        double health = Vitality.MaxHp(state, FighterKind.Second);
        state.Companions[1] = HeroRole.Tank;
        RoleInfo tank = Roles.Of(HeroRole.Tank);
        RoleInfo mage = Roles.Of(HeroRole.Mage);
        Assert.Equal(tank.Dps / mage.Dps, Economy.Dps(state, FighterKind.Second) / damage, 6);
        Assert.Equal(tank.Hp / mage.Hp, Vitality.MaxHp(state, FighterKind.Second) / health, 6);
        Assert.Equal(tank.Interval / Talents.Speed(state), Economy.Interval(state, FighterKind.Second), 6);
        Assert.False(Vitality.Ranged(state, FighterKind.Second));
    }

    [Fact]
    public void TheSynergyCountsTheCompanions()
    {
        FrontierState state = new() { Role = HeroRole.Warrior, Fighters = [10, 0, 0] };
        Assert.Equal(1 + Balance.SynergyBonus, Economy.Synergy(state), 6);
        state.Fighters = [10, 10, 10];
        Assert.Equal(1 + 3 * Balance.SynergyBonus, Economy.Synergy(state), 6);
        state.Companions[0] = HeroRole.Warrior;
        state.Companions[1] = HeroRole.Warrior;
        Assert.Equal(1 + Balance.SynergyBonus, Economy.Synergy(state), 6);
    }

    [Fact]
    public void ATankCompanionDrawsTheEvenBlows()
    {
        FrontierState state = new() { Fighters = [10, 10, 10] };
        state.Companions[1] = HeroRole.Tank;
        Monster monster = new(1, 0, false, 5, 100, 0);
        for (int strike = 0; strike < 20; strike++)
        {
            monster.Strikes += 2;
            Assert.Equal(FighterKind.Second, Skirmish.Target(state, monster)?.Kind);
        }
    }

    [Fact]
    public void AnOldSaveKeepsTheArcherAndTheMage()
    {
        FrontierState state = new() { Fighters = [5, 5, 5] };
        FrontierSave save = SaveCodec.Capture(state) with { Companions = null };
        FrontierState restored = SaveCodec.Restore(save);
        Assert.Equal(HeroRole.Archer, restored.ClassOf(FighterKind.First));
        Assert.Equal(HeroRole.Mage, restored.ClassOf(FighterKind.Second));
    }
}
