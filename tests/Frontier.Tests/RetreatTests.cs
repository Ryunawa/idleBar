using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class RetreatTests
{
    [Fact]
    public void NoSoulsBeforeStageFifty()
    {
        Assert.Equal(0, RetreatRules.SoulsFor(49));
        Assert.Equal(25, RetreatRules.SoulsFor(50));
        Assert.Equal(225, RetreatRules.SoulsFor(150));
    }

    [Fact]
    public void ARetreatTooEarlyChangesNothing()
    {
        FrontierState state = new() { Stage = 30, MaxStage = 30, Gold = 500 };
        FrontierGame game = new(state, new Random(1));
        Assert.Equal(0, game.Retreat());
        Assert.Equal(30, state.Stage);
        Assert.Equal(500, state.Gold);
    }

    [Fact]
    public void ARetreatResetsTheRunAndKeepsTheLegacy()
    {
        FrontierState state = new() { Stage = 62, MaxStage = 62, BestStage = 62, Gold = 1e6, LifetimeGold = 5e6 };
        Village.Build(state, BuildingKind.Farm, 40);
        state.Fighters[(int)FighterKind.Archer] = 12;
        state.Achievements.Add("boss-1");
        FrontierGame game = new(state, new Random(1));
        int souls = game.Retreat();
        Assert.Equal(RetreatRules.SoulsFor(62), souls);
        Assert.Equal(souls, state.Souls);
        Assert.Equal(1, state.Retreats);
        Assert.Equal(1, state.Stage);
        Assert.Equal(0, state.Gold);
        Assert.Equal(0, state.Level(BuildingKind.Farm));
        Assert.Equal(0, state.Level(FighterKind.Archer));
        Assert.Equal(1, state.Level(FighterKind.Hero));
        Assert.Equal(62, state.BestStage);
        Assert.Equal(5e6, state.LifetimeGold);
        Assert.Contains("boss-1", state.Achievements);
        Assert.True(state.IntendantUnlocked);
    }

    [Fact]
    public void TheIntendantStaysOffUntilTheFirstRetreat()
    {
        FrontierState state = new();
        FrontierGame game = new(state, new Random(1));
        game.SetIntendant(true);
        Assert.False(state.IntendantOn);
        state.IntendantUnlocked = true;
        game.SetIntendant(true);
        Assert.True(state.IntendantOn);
    }
}
