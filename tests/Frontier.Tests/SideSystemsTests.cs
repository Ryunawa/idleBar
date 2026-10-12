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
        state.Buildings[(int)BuildingKind.Farm] = 5;
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
        state.Buildings[(int)BuildingKind.Bakery] = 1;
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, state.ConvoyTimer + Balance.ConvoyTravel + 1);
        Assert.Equal(1, state.Supply);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is Supplied { Stacks: 1 });
    }

    [Fact]
    public void TheIntendantBuildsEachNewBuildingFirst()
    {
        FrontierState state = new() { Gold = 50 };
        Purchase? choice = Intendant.Choose(state);
        Assert.Equal(new BuildingPurchase(BuildingKind.Farm), choice);
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
