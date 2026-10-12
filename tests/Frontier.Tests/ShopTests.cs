using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class ShopTests
{
    [Fact]
    public void BuyingPaysTheCostAndRaisesTheLevel()
    {
        FrontierState state = new() { Gold = 1_000 };
        FrontierGame game = new(state, new Random(1));
        double cost = Economy.BuildingCost(state, BuildingKind.Farm);
        Assert.True(game.Buy(new BuildingPurchase(BuildingKind.Farm)));
        Assert.Equal(1_000 - cost, state.Gold, 6);
        Assert.Equal(1, state.Level(BuildingKind.Farm));
    }

    [Fact]
    public void ALockedBuildingCannotBeBought()
    {
        FrontierState state = new() { Gold = 1e9 };
        FrontierGame game = new(state, new Random(1));
        Assert.False(game.Buy(new BuildingPurchase(BuildingKind.Bakery)));
        Assert.Equal(0, state.Level(BuildingKind.Bakery));
    }

    [Fact]
    public void NotEnoughGoldBuysNothing()
    {
        FrontierState state = new() { Gold = 1 };
        FrontierGame game = new(state, new Random(1));
        Assert.False(game.Buy(new FighterPurchase(FighterKind.Archer)));
        Assert.Equal(1, state.Gold);
    }

    [Fact]
    public void TheTenthLevelReachesATier()
    {
        FrontierState state = new() { Gold = 1e12 };
        state.Buildings[(int)BuildingKind.Farm] = 9;
        FrontierGame game = new(state, new Random(1));
        game.Buy(new BuildingPurchase(BuildingKind.Farm));
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is TierReached { Kind: BuildingKind.Farm, Tier: 1 });
    }

    [Fact]
    public void BuyingManyStopsWhenTheGoldRunsOut()
    {
        FrontierState state = new() { Gold = 100 };
        FrontierGame game = new(state, new Random(1));
        int bought = game.BuyMany(new BuildingPurchase(BuildingKind.Farm), 10);
        Assert.InRange(bought, 1, 9);
        Assert.Equal(bought, state.Level(BuildingKind.Farm));
        Assert.True(state.Gold < Economy.BuildingCost(state, BuildingKind.Farm));
    }

    [Fact]
    public void RecruitingACompanionMakesHerFight()
    {
        FrontierState state = new() { Gold = 1e6 };
        FrontierGame game = new(state, new Random(1));
        Assert.Equal(0, Economy.Dps(state, FighterKind.Archer));
        game.Buy(new FighterPurchase(FighterKind.Archer));
        Assert.True(Economy.Dps(state, FighterKind.Archer) > 0);
    }

    [Theory]
    [InlineData(1_000)]
    [InlineData(123_456)]
    [InlineData(9.87e12)]
    public void MaxCountBuysExactlyWhatTheGoldAllows(double gold)
    {
        FrontierState state = new() { Gold = gold };
        FrontierGame game = new(state, new Random(1));
        BuildingPurchase farm = new(BuildingKind.Farm);
        int count = Shop.MaxCount(state, farm);
        Assert.Equal(count, game.BuyMany(farm, count));
        Assert.False(Shop.Affordable(state, farm));
    }

    [Fact]
    public void TenLevelsCostTheSumOfEachLevel()
    {
        FrontierState state = new() { Gold = 1e12 };
        FrontierGame game = new(state, new Random(1));
        FighterPurchase hero = new(FighterKind.Hero);
        double expected = Shop.CostOf(state, hero, 10);
        game.BuyMany(hero, 10);
        Assert.Equal(1e12 - expected, state.Gold, 3);
    }

    [Fact]
    public void MaxCountIsZeroWhenOneLevelIsTooExpensive()
    {
        FrontierState state = new() { Gold = 1 };
        Assert.Equal(0, Shop.MaxCount(state, new FighterPurchase(FighterKind.Mage)));
        Assert.Equal(0, Shop.MaxCount(new FrontierState { Gold = 1e30 }, new BuildingPurchase(BuildingKind.Portal)));
    }
}
