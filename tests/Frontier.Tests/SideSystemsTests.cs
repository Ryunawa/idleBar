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
    public void SkillsStayLockedUntilTheirStage()
    {
        FrontierState state = new() { Stage = 24, MaxStage = 24 };
        FrontierGame game = new(state, new Random(1));
        Assert.False(game.UseSkill(SkillKind.Fury));
        Assert.Equal(0, state.SkillsUsed);
        state.MaxStage = 25;
        Assert.True(game.UseSkill(SkillKind.Fury));
        Assert.False(Skills.Unlocked(state, SkillKind.GoldRain));
    }

    [Fact]
    public void ReachingAStageUnlocksItsSkill()
    {
        FrontierState state = new() { Stage = 14, MaxStage = 14, Kills = Balance.MonstersPerStage - 1 };
        state.Fighters[(int)FighterKind.Hero] = 500;
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 10);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is SkillUnlocked { Kind: SkillKind.Smite });
    }

    [Fact]
    public void AFrozenGameIgnoresThePlayer()
    {
        FrontierState state = new() { Gold = 1e9, Stage = 60, MaxStage = 60, BestStage = 60, BossHeld = false };
        FrontierGame game = new(state, new Random(1)) { Frozen = true };
        Assert.False(game.Buy(new ArmorPurchase()));
        Assert.Equal(0, game.BuyMany(new Construction(0, BuildingKind.Forge), 3));
        Assert.False(game.UseSkill(SkillKind.GoldRain));
        Assert.Equal(0, game.Retreat());
        Assert.Equal(1e9, state.Gold);
        game.Frozen = false;
        Assert.True(game.Buy(new ArmorPurchase()));
    }

    [Fact]
    public void ASkillCannotBeUsedTwiceInARow()
    {
        FrontierState state = new() { MaxStage = 50 };
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
