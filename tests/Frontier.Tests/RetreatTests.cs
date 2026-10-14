using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class RetreatTests
{
    [Fact]
    public void SoulsComeFromAllTheGoldEverEarned()
    {
        Assert.Equal(0, RetreatRules.TotalSouls(0));
        Assert.Equal(5, RetreatRules.TotalSouls(1e6));
        Assert.Equal(24, RetreatRules.TotalSouls(1e9));
        Assert.Equal(997, RetreatRules.TotalSouls(1e16));
    }

    [Fact]
    public void ARetreatOnlyAddsTheSoulsNotYetClaimed()
    {
        Assert.Equal(14, RetreatRules.Pending(new FrontierState { MaxStage = 60, LifetimeGold = 1e9, Souls = 10 }));
        Assert.Equal(0, RetreatRules.Pending(new FrontierState { MaxStage = 49, LifetimeGold = 1e9 }));
        Assert.Equal(0, RetreatRules.Pending(new FrontierState { MaxStage = 60, LifetimeGold = 1e9, Souls = 30 }));
    }

    [Fact]
    public void TheNextSoulNeedsTheAnnouncedGold()
    {
        FrontierState state = new() { MaxStage = 60, LifetimeGold = 3e9 };
        int total = RetreatRules.TotalSouls(state.LifetimeGold);
        double missing = RetreatRules.GoldToNextSoul(state);
        Assert.True(missing > 0);
        Assert.Equal(total, RetreatRules.TotalSouls(state.LifetimeGold + missing * 0.999));
        Assert.Equal(total + 1, RetreatRules.TotalSouls(state.LifetimeGold + missing * 1.001));
    }

    [Fact]
    public void RetreatingAgainWithoutNewGoldGivesNothing()
    {
        FrontierState state = new() { Stage = 55, MaxStage = 55, LifetimeGold = 1e9 };
        FrontierGame game = new(state, new Random(1));
        Assert.Equal(24, game.Retreat());
        state.MaxStage = 55;
        Assert.Equal(0, game.Retreat());
        Assert.Equal(1, state.Retreats);
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
        state.Fighters[(int)FighterKind.First] = 12;
        state.Companions[0] = HeroRole.Tank;
        state.Achievements.Add("boss-1");
        FrontierGame game = new(state, new Random(1));
        int souls = game.Retreat();
        Assert.Equal(RetreatRules.TotalSouls(5e6), souls);
        Assert.Equal(souls, state.Souls);
        Assert.Equal(1, state.Retreats);
        Assert.Equal(1, state.Stage);
        Assert.Equal(0, state.Gold);
        Assert.Equal(0, state.Level(BuildingKind.Farm));
        Assert.Equal(0, state.Level(FighterKind.First));
        Assert.Equal(1, state.Level(FighterKind.Hero));
        Assert.Equal(62, state.BestStage);
        Assert.Equal(5e6, state.LifetimeGold);
        Assert.Contains("boss-1", state.Achievements);
        Assert.All(state.Companions, role => Assert.Null(role));
    }
}
