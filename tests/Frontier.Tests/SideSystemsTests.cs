using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class SideSystemsTests
{
    [Fact]
    public void TheChickenComesAfterItsFirstDelay()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        BattleTests.Run(game, Balance.ChickenFirst + 1);
        Assert.NotNull(game.State.Chicken);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is ChickenAppeared);
    }

    [Fact]
    public void CatchingTheChickenStartsAFrenzy()
    {
        FrontierState state = new() { Chicken = 1 };
        Village.Build(state, BuildingKind.Farm, 5);
        FrontierGame game = new(state, new Random(1));
        double calm = Economy.GoldPerSecond(state);
        Assert.True(game.CatchChicken());
        Assert.Null(state.Chicken);
        Assert.Equal(calm * Balance.FrenzyGold, Economy.GoldPerSecond(state), 6);
        Assert.False(game.CatchChicken());
    }

    [Fact]
    public void TheBakerySuppliesTheFront()
    {
        FrontierState state = new();
        Village.Build(state, BuildingKind.Bakery, 1);
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, state.ConvoyTimer + Balance.ConvoyTravel + 1);
        Assert.Equal(1, state.Supply);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is Supplied { Stacks: 1 });
    }

    [Fact]
    public void TheIntendantFillsAFreePlotFirst()
    {
        FrontierState state = new() { Gold = 1_000 };
        Assert.Equal(new Construction(0, BuildingKind.Forge), Intendant.Choose(state));
        Village.Build(state, BuildingKind.Forge, 1, 0);
        Assert.Equal(new Construction(1, BuildingKind.Bakery), Intendant.Choose(state));
    }

    [Fact]
    public void TheFarmBringsTheChickenTwiceAsOften()
    {
        FrontierState state = new();
        Assert.Equal(1, Economy.ChickenPace(state));
        Village.Build(state, BuildingKind.Farm, 40);
        Assert.Equal(1.4, Economy.ChickenPace(state), 6);
        Village.Build(state, BuildingKind.Farm, 300);
        Assert.Equal(2, Economy.ChickenPace(state), 6);
    }

    [Fact]
    public void ThePortalRaisesTheGoldOfEveryPlot()
    {
        FrontierState state = new();
        Village.Build(state, BuildingKind.Farm, 10, 0);
        double before = Economy.PlotGold(state, 0);
        Village.Build(state, BuildingKind.Portal, 50, 1);
        Assert.Equal(before * 1.5, Economy.PlotGold(state, 0), 6);
    }

    [Fact]
    public void ASkillCannotBeUsedTwiceInARow()
    {
        FrontierState state = new();
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(SkillKind.Fury));
        Assert.False(game.UseSkill(SkillKind.Fury));
        Assert.Equal(Skills.Of(SkillKind.Fury).Duration, state.FuryLeft);
        Assert.False(game.UseSkill(SkillKind.Smite));
    }

    [Fact]
    public void AchievementsUnlockOnceAndAddTwoPercent()
    {
        FrontierState state = new() { BossesKilled = 1 };
        double before = Economy.Wealth(state);
        FrontierGame game = new(state, new Random(1));
        game.Advance(Balance.Step);
        Assert.Contains("boss-1", state.Achievements);
        Assert.Equal(before * (1 + Balance.AchievementBonus), Economy.Wealth(state), 6);
        Assert.Single(game.Events.Drain(), frontierEvent => frontierEvent is AchievementUnlocked);
    }
}
