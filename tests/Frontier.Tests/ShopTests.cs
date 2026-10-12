using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class ShopTests
{
    [Fact]
    public void BuildingPaysThePlotCostAndPlacesTheBuilding()
    {
        FrontierState state = new() { Gold = 1_000 };
        FrontierGame game = new(state, new Random(1));
        double cost = Economy.PlotCost(state, 0);
        Assert.True(game.Buy(new Construction(0, BuildingKind.Forge)));
        Assert.Equal(1_000 - cost, state.Gold, 6);
        Assert.Equal(BuildingKind.Forge, state.Built[0]);
        Assert.Equal(1, state.Level(BuildingKind.Forge));
    }

    [Fact]
    public void ALockedPlotCannotBeBuilt()
    {
        FrontierState state = new() { Gold = 1e9 };
        FrontierGame game = new(state, new Random(1));
        Assert.False(game.Buy(new Construction(2, BuildingKind.Bakery)));
        Assert.Null(state.Built[2]);
    }

    [Fact]
    public void EachBuildingIsBuiltOnlyOnce()
    {
        FrontierState state = new() { Gold = 1e9 };
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.Buy(new Construction(0, BuildingKind.Forge)));
        Assert.False(game.Buy(new Construction(1, BuildingKind.Forge)));
        Assert.False(game.Buy(new Construction(0, BuildingKind.Mill)));
        Assert.True(game.Buy(new Construction(1, BuildingKind.Mill)));
    }

    [Fact]
    public void GoldComesFromThePlotNotFromTheBuilding()
    {
        FrontierState forge = new();
        Village.Build(forge, BuildingKind.Forge, 4, 1);
        FrontierState farm = new();
        Village.Build(farm, BuildingKind.Farm, 4, 1);
        Assert.Equal(Economy.PlotGold(farm, 1), Economy.PlotGold(forge, 1), 6);
        Assert.True(Economy.PlotGold(forge, 1) > Economy.PlotGoldAt(forge, 0, 4));
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
        Village.Build(state, BuildingKind.Farm, 9);
        FrontierGame game = new(state, new Random(1));
        game.Buy(new PlotPurchase(0));
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is TierReached { Plot: 0, Kind: BuildingKind.Farm, Tier: 1 });
    }

    [Fact]
    public void BuyingManyStopsWhenTheGoldRunsOut()
    {
        FrontierState state = new() { Gold = 300 };
        Village.Build(state, BuildingKind.Farm, 1);
        FrontierGame game = new(state, new Random(1));
        int bought = game.BuyMany(new PlotPurchase(0), 10);
        Assert.InRange(bought, 1, 9);
        Assert.Equal(1 + bought, state.Level(BuildingKind.Farm));
        Assert.True(state.Gold < Economy.PlotCost(state, 0));
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
        Village.Build(state, BuildingKind.Farm, 1);
        FrontierGame game = new(state, new Random(1));
        PlotPurchase farm = new(0);
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
    public void MaxCountIsZeroWhenNothingCanBeBought()
    {
        Assert.Equal(0, Shop.MaxCount(new FrontierState { Gold = 1 }, new FighterPurchase(FighterKind.Mage)));
        Assert.Equal(0, Shop.MaxCount(new FrontierState { Gold = 1e30 }, new PlotPurchase(0)));
        Assert.Equal(0, Shop.MaxCount(new FrontierState { Gold = 1e30 }, new Construction(6, BuildingKind.Portal)));
    }
}
