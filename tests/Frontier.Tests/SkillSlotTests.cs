using System;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class SkillSlotTests
{
    [Fact]
    public void ANewlyLearnedActiveIsReadyMidRun()
    {
        FrontierState state = new() { Stage = 30, MaxStage = 30, BestStage = 31 };
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.Learn(SkillKind.Fury));
        Assert.True(game.Place(1, SkillKind.Fury));
        Assert.Equal(0, SkillCasts.Wait(state, SkillKind.Fury));
        Assert.True(state.PlacedThisRun[(int)SkillKind.Fury]);
        Assert.True(game.UseSkill(1));
        Assert.Equal(Economy.SkillCooldown(state, SkillKind.Fury), SkillCasts.Wait(state, SkillKind.Fury));
    }

    [Fact]
    public void AtTheStartOfARunPlacingIsFree()
    {
        FrontierState state = new() { Stage = 1, MaxStage = 1, BestStage = 30 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Fury);
        game.Learn(SkillKind.GoldRain);
        Assert.False(game.UseSkill(0));
        game.Place(0, SkillKind.Fury);
        Assert.True(game.UseSkill(0));
        state.Cooldowns[(int)SkillKind.Fury] = 100;
        Assert.True(game.Place(0, SkillKind.GoldRain));
        Assert.True(game.UseSkill(0));
        Assert.False(SkillTree.RestartsIfPlaced(state, SkillKind.Fury));
        Assert.True(game.Place(0, SkillKind.Fury));
        Assert.Equal(100, SkillCasts.Wait(state, SkillKind.Fury));
        Assert.False(game.UseSkill(0));
        Assert.True(game.Place(1, SkillKind.GoldRain));
        Assert.Equal(Economy.SkillCooldown(state, SkillKind.GoldRain), SkillCasts.Wait(state, SkillKind.GoldRain));
        Assert.True(state.PlacedThisRun[(int)SkillKind.Fury] && state.PlacedThisRun[(int)SkillKind.GoldRain]);
    }

    [Fact]
    public void ASkillPlacedAgainMidRunRestartsItsFullCooldown()
    {
        FrontierState state = new() { Stage = 30, MaxStage = 30, BestStage = 31 };
        Village.Build(state, BuildingKind.Chapel, 50);
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Fury);
        game.Learn(SkillKind.GoldRain);
        Assert.True(game.Place(0, SkillKind.Fury));
        Assert.True(game.Place(0, SkillKind.GoldRain));
        Assert.Equal(0, SkillCasts.Wait(state, SkillKind.Fury));
        Assert.Equal(0, SkillCasts.Wait(state, SkillKind.GoldRain));
        Assert.True(SkillTree.RestartsIfPlaced(state, SkillKind.Fury));
        Assert.False(SkillTree.RestartsIfPlaced(state, SkillKind.GoldRain));
        Assert.True(game.Place(1, SkillKind.Fury));
        Assert.Equal(900 / 2.0, SkillCasts.Wait(state, SkillKind.Fury), 6);
        Assert.False(game.UseSkill(1));
        state.Cooldowns[(int)SkillKind.GoldRain] = 5_000;
        Assert.True(game.Place(0, null));
        Assert.True(game.Place(0, SkillKind.GoldRain));
        Assert.Equal(5_000, SkillCasts.Wait(state, SkillKind.GoldRain));
    }

    [Fact]
    public void MovingOrSwappingPlacedSkillsChangesNoCooldown()
    {
        FrontierState state = new() { Stage = 30, MaxStage = 30, BestStage = 31 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Fury);
        game.Learn(SkillKind.GoldRain);
        game.Place(0, SkillKind.Fury);
        game.Place(1, SkillKind.GoldRain);
        Assert.True(game.UseSkill(0));
        state.Cooldowns[(int)SkillKind.GoldRain] = 40;
        double fury = SkillCasts.Wait(state, SkillKind.Fury);
        Assert.True(game.Place(0, SkillKind.GoldRain));
        Assert.Equal([SkillKind.GoldRain, SkillKind.Fury, null], state.Slots);
        Assert.True(game.Place(2, SkillKind.Fury));
        Assert.Equal([SkillKind.GoldRain, null, SkillKind.Fury], state.Slots);
        Assert.Equal(fury, SkillCasts.Wait(state, SkillKind.Fury));
        Assert.Equal(40, SkillCasts.Wait(state, SkillKind.GoldRain));
        Assert.False(game.UseSkill(2));
        Assert.True(game.Place(2, null));
        BattleTests.Run(game, 60);
        Assert.InRange(SkillCasts.Wait(state, SkillKind.Fury), fury - 60.1, fury - 59.9);
    }

    [Fact]
    public void ARetreatKeepsOnlyTheSlottedSkillsAsPlaced()
    {
        FrontierState state = new() { Stage = 60, MaxStage = 60, BestStage = 60, LifetimeGold = 1e12 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Fury);
        game.Learn(SkillKind.GoldRain);
        game.Place(0, SkillKind.Fury);
        game.Place(1, SkillKind.GoldRain);
        game.Place(1, null);
        Assert.True(game.Retreat() > 0);
        Assert.True(state.PlacedThisRun[(int)SkillKind.Fury]);
        Assert.False(state.PlacedThisRun[(int)SkillKind.GoldRain]);
        state.Stage = 20;
        state.MaxStage = 20;
        Assert.True(game.Place(1, SkillKind.GoldRain));
        Assert.Equal(0, SkillCasts.Wait(state, SkillKind.GoldRain));
        Assert.True(game.Place(0, null));
        Assert.True(game.Place(0, SkillKind.Fury));
        Assert.Equal(Economy.SkillCooldown(state, SkillKind.Fury), SkillCasts.Wait(state, SkillKind.Fury));
    }

    [Fact]
    public void RedistributingClearsThePlacements()
    {
        FrontierState state = new() { Stage = 5, MaxStage = 5, BestStage = 41 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Fury);
        game.Place(0, SkillKind.Fury);
        Assert.True(game.Redistribute());
        Assert.DoesNotContain(true, state.PlacedThisRun);
    }

    [Fact]
    public void TheSlotsWarnBeforeARestart()
    {
        FrontierState state = new() { Stage = 30, MaxStage = 30, BestStage = 31 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Fury);
        game.Learn(SkillKind.GoldRain);
        game.Place(0, SkillKind.Fury);
        game.Place(0, SkillKind.GoldRain);
        Assert.Equal(
            [new SkillChoice(null, "Vide"), new SkillChoice(SkillKind.Fury, "Furie · recharge complète", SkillChoice.Restart), new SkillChoice(SkillKind.GoldRain, "Pluie d'or")],
            SkillChoice.For(state));
        Assert.Contains("repart avec sa recharge complète", LaneHints.Describe(new LaneTarget(TargetKind.Skill, 1), state));
        state.MaxStage = Balance.RedistributeStage;
        Assert.DoesNotContain("recharge complète", LaneHints.Describe(new LaneTarget(TargetKind.Skill, 1), state));
        Assert.Equal(new SkillChoice(SkillKind.Fury, "Furie"), SkillChoice.For(state)[1]);
    }
}
