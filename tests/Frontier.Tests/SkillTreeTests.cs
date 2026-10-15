using System;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class SkillTreeTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(10, 0)]
    [InlineData(11, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(151, 15)]
    [InlineData(281, 28)]
    public void TheRecordGivesOnePointPerBossBeaten(int best, int points) =>
        Assert.Equal(points, SkillTree.Points(new FrontierState { BestStage = best }));

    [Fact]
    public void TheWholeTreeCostsTwentyEightPoints()
    {
        Assert.Equal(28, Skills.All.Sum(skill => skill.Cost * skill.MaxLevel));
        Assert.Equal(9, Skills.All.Count(skill => skill.Active));
        Assert.All(Skills.All.Where(skill => !skill.Active), skill => Assert.Equal(3, skill.MaxLevel));
        Assert.Equal(2, Skills.Of(SkillKind.Judgment).Cost);
    }

    [Fact]
    public void BeatingABossForTheFirstTimeEarnsAPoint()
    {
        FrontierState state = new() { Stage = 10, MaxStage = 10, BestStage = 10, Kills = Balance.MonstersPerStage - 1 };
        FrontierGame game = BattleTests.Strong(state);
        BattleTests.Run(game, 10);
        Assert.InRange(state.BestStage, 11, 12);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is PointEarned { Points: 1 });
    }

    [Fact]
    public void EachNodeNeedsThePreviousOneOfItsBranch()
    {
        FrontierGame game = new(new FrontierState { BestStage = 51 }, new Random(1));
        Assert.Equal("Demande Orage", SkillTree.Blocker(game.State, SkillKind.Storm));
        Assert.False(game.Learn(SkillKind.Storm));
        Assert.True(game.Learn(SkillKind.Smite));
        Assert.True(game.Learn(SkillKind.Thunder));
        Assert.True(game.Learn(SkillKind.Storm));
        Assert.Equal(3, SkillTree.Spent(game.State));
        Assert.Equal(2, SkillTree.Free(game.State));
    }

    [Fact]
    public void PointsRunOutAndUpgradesStopAtThree()
    {
        FrontierGame game = new(new FrontierState { BestStage = 49 }, new Random(1));
        Assert.True(game.Learn(SkillKind.Smite));
        Assert.True(game.Learn(SkillKind.Thunder));
        Assert.True(game.Learn(SkillKind.Thunder));
        Assert.True(game.Learn(SkillKind.Thunder));
        Assert.Equal("Niveau maximum", SkillTree.Blocker(game.State, SkillKind.Thunder));
        Assert.Equal("Apprise", SkillTree.Blocker(game.State, SkillKind.Smite));
        Assert.Equal("Demande 1 point libre", SkillTree.Blocker(game.State, SkillKind.Fury));
        Assert.Equal(51, SkillTree.NextPointStage(game.State));
    }

    [Fact]
    public void JudgmentCostsTwoPoints()
    {
        FrontierState state = new() { BestStage = 50 };
        state.Learned[(int)SkillKind.Smite] = 1;
        state.Learned[(int)SkillKind.Thunder] = 1;
        state.Learned[(int)SkillKind.Storm] = 1;
        state.Learned[(int)SkillKind.LightningRod] = 1;
        FrontierGame game = new(state, new Random(1));
        Assert.Equal("Demande 2 points libres", SkillTree.Blocker(state, SkillKind.Judgment));
        state.BestStage = 61;
        Assert.True(game.Learn(SkillKind.Judgment));
        Assert.Equal(0, SkillTree.Free(state));
    }

    [Fact]
    public void PointsAndSkillsSurviveARetreat()
    {
        FrontierState state = new() { Stage = 60, MaxStage = 60, BestStage = 60, LifetimeGold = 1e12 };
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.Learn(SkillKind.Fury));
        Assert.True(game.Place(0, SkillKind.Fury));
        Assert.True(game.Retreat() > 0);
        Assert.Equal(5, SkillTree.Points(state));
        Assert.True(SkillTree.Known(state, SkillKind.Fury));
        Assert.Equal(SkillKind.Fury, state.Slots[0]);
    }

    [Fact]
    public void RedistributingIsFreeButOnlyAtTheStartOfARun()
    {
        FrontierState state = new() { Stage = 11, MaxStage = 11, BestStage = 41 };
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.Learn(SkillKind.GoldRain));
        Assert.True(game.Learn(SkillKind.Downpour));
        Assert.True(game.Place(2, SkillKind.GoldRain));
        Assert.False(game.Redistribute());
        Assert.Equal(2, SkillTree.Spent(state));
        state.MaxStage = Balance.RedistributeStage;
        state.Stage = Balance.RedistributeStage;
        state.Cooldowns[(int)SkillKind.GoldRain] = 300;
        Assert.True(game.Redistribute());
        Assert.Equal(0, SkillTree.Spent(state));
        Assert.Equal(4, SkillTree.Free(state));
        Assert.All(state.Slots, slot => Assert.Null(slot));
        Assert.Equal(300, state.Cooldowns[(int)SkillKind.GoldRain]);
        Assert.False(game.Redistribute());
    }

    [Fact]
    public void OnlyLearnedActivesGoInTheSlots()
    {
        FrontierGame game = new(new FrontierState { Stage = 30, MaxStage = 30, BestStage = 30 }, new Random(1));
        Assert.False(game.Place(0, SkillKind.Smite));
        game.Learn(SkillKind.Smite);
        game.Learn(SkillKind.Thunder);
        Assert.False(game.Place(0, SkillKind.Thunder));
        Assert.False(game.Place(3, SkillKind.Smite));
        Assert.True(game.Place(0, SkillKind.Smite));
        Assert.False(game.Place(0, SkillKind.Smite));
        Assert.True(game.Place(0, null));
        Assert.Null(game.State.Slots[0]);
    }
}
