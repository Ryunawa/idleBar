using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class AllyTests
{
    [Fact]
    public void AnAllyArrivesAndFightsBesideTheTeam()
    {
        FrontierGame game = new(Sturdy(61), new Random(1));
        game.Reinforce([Ally(7, HeroRole.Warrior)], []);
        BattleTests.Run(game, 5);
        IReadOnlyList<FrontierEvent> events = game.Events.Drain();
        Assert.Contains(events, frontierEvent => frontierEvent is AllyArrived { Id: 7, Name: "Brune" });
        Assert.Contains(events, frontierEvent => frontierEvent is Hit { Source: HitSource.Ally, Ally: 7 });
    }

    [Fact]
    public void AnAllyHitsWithItsShareOfTheHerosPower()
    {
        FrontierState state = new() { Fighters = [40, 0, 0] };
        Ally ally = Ally(1, HeroRole.Warrior, 1.2);
        state.Allies.Add(ally);
        Assert.Equal(1.2, Economy.AllyDps(state, ally) / Economy.Dps(state, FighterKind.Hero), 6);
        Assert.Equal(1.2 * Roles.Of(HeroRole.Tank).Hp, Vitality.AllyMaxHp(state, Ally(2, HeroRole.Tank, 1.2)) / Vitality.MaxHp(state, FighterKind.Hero), 6);
    }

    [Fact]
    public void ATankAllyDrawsTheEvenBlows()
    {
        FrontierState state = new() { Fighters = [10, 10, 10] };
        state.Allies.Add(Ally(4, HeroRole.Tank));
        Monster monster = new(1, 0, false, 5, 100, 0);
        for (int strike = 0; strike < 20; strike++)
        {
            monster.Strikes += 2;
            Assert.Equal(4, Skirmish.Target(state, monster)?.AllyId);
        }
    }

    [Fact]
    public void ASupportAllyBlessesAtHalfStrength()
    {
        FrontierState state = new() { Fighters = [10, 10, 0] };
        Assert.Equal(0, Talents.Blessing(state), 6);
        state.Allies.Add(Ally(5, HeroRole.Support));
        Assert.Equal(Balance.SupportBlessing * Balance.CompanionShare, Talents.Blessing(state), 6);
    }

    [Fact]
    public void AFallenSupportHeroWaitsFortyFiveSeconds()
    {
        FrontierState state = new() { Role = HeroRole.Support, Stage = 25, MaxStage = 25 };
        state.Health[(int)FighterKind.Hero] = 0.01;
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 30);
        Fell fell = game.Events.Drain().OfType<Fell>().First();
        Assert.Equal(45, fell.Seconds);
    }

    [Fact]
    public void TheSynergyCountsEveryRoleOnTheFront()
    {
        FrontierState state = new() { Role = HeroRole.Warrior };
        Assert.Equal(1 + Balance.SynergyBonus, Economy.Synergy(state), 6);
        state.Allies.AddRange([Ally(1, HeroRole.Tank), Ally(2, HeroRole.Mage), Ally(3, HeroRole.Warrior)]);
        Assert.Equal(1 + 3 * Balance.SynergyBonus, Economy.Synergy(state), 6);
    }

    [Fact]
    public void TheFrontHoldsWhileAnAllyStands()
    {
        FrontierState state = new() { Fighters = [10, 0, 0] };
        state.Downed[(int)FighterKind.Hero] = 20;
        Assert.False(Team.AnyUp(state));
        state.Allies.Add(Ally(1, HeroRole.Archer));
        Assert.True(Team.AnyUp(state));
    }

    [Fact]
    public void AlliesLeaveWhenTheirTimeIsUp()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        game.Reinforce([Ally(9, HeroRole.Mage, left: 2)], []);
        BattleTests.Run(game, 3);
        Assert.Empty(game.State.Allies);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is AllyLeft { Id: 9 });
    }

    [Fact]
    public void ANewReadingKeepsAnAllysWounds()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        game.Reinforce([Ally(3, HeroRole.Tank), Ally(4, HeroRole.Mage)], []);
        game.State.Allies[0].Health = 0.4;
        game.Events.Drain();
        game.Reinforce([Ally(3, HeroRole.Tank, left: 600)], []);
        Ally kept = Assert.Single(game.State.Allies);
        Assert.Equal(0.4, kept.Health);
        Assert.Equal(600, kept.Left);
        Assert.DoesNotContain(game.Events.Drain(), frontierEvent => frontierEvent is AllyArrived);
    }

    [Fact]
    public void BoostsRaiseGoldAndDamage()
    {
        FrontierState state = new();
        Village.Build(state, BuildingKind.Farm, 10);
        double gold = Economy.GoldPerSecond(state);
        double damage = Economy.Dps(state, FighterKind.Hero);
        FrontierGame game = new(state, new Random(1));
        game.Reinforce([], [new Boost("convoy", 50, 0, 1800), new Boost("titan", 50, 50, 7200)]);
        Assert.Equal(2, Economy.GoldPerSecond(state) / gold, 6);
        Assert.Equal(1.5, Economy.Dps(state, FighterKind.Hero) / damage, 6);
        Assert.Equal(2, game.Events.Drain().OfType<Boosted>().Count());
        game.Absent(3600);
        Boost left = Assert.Single(state.Boosts);
        Assert.Equal("titan", left.Source);
    }

    private static FrontierState Sturdy(int stage) =>
        new() { Fighters = [1, 1, 1], Stage = stage, MaxStage = stage, Armor = int.MaxValue / 2 };

    private static Ally Ally(long id, HeroRole role, double ratio = 1, double left = 1800) =>
        new(id, new HeroLook("Brune", 1, 1, 1, 1), role, ratio, left);
}
