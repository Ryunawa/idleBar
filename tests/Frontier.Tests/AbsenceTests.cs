using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class AbsenceTests
{
    [Fact]
    public void AbsencePaysTheVillageAndAQuarterOfTheFront()
    {
        FrontierState state = new() { Stage = 30, MaxStage = 30 };
        Village.Build(state, BuildingKind.Farm, 10);
        FrontierGame game = new(state, new Random(1));
        double rate = Economy.BaseGoldPerSecond(state) + Balance.AbsenceFrontShare * Economy.FrontGoldPerSecond(state);
        Assert.True(Economy.FrontGoldPerSecond(state) > 0);
        game.Absent(600);
        Assert.Equal(rate * 600, state.Gold, 6);
        Assert.Equal(30, state.Stage);
        Assert.Equal(0, state.MonstersKilled);
    }

    [Fact]
    public void AbsenceIsCappedAtEightHours()
    {
        FrontierState state = new();
        Village.Build(state, BuildingKind.Farm, 10);
        FrontierGame game = new(state, new Random(1));
        game.Absent(48 * 3600);
        Assert.Equal(Economy.AbsenceGoldPerSecond(state) * Balance.AbsenceCapSeconds, state.Gold, 3);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is Returned { Seconds: 48 * 3600, Counted: Balance.AbsenceCapSeconds });
    }

    [Fact]
    public void ALongGapBetweenFramesCountsAsAbsence()
    {
        FrontierState state = new();
        Village.Build(state, BuildingKind.Farm, 10);
        FrontierGame game = new(state, new Random(1));
        game.Advance(3_600);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is Returned { Seconds: 3_600 });
        Assert.Equal(Economy.AbsenceGoldPerSecond(state) * 3_600, state.Gold, 3);
    }

    [Fact]
    public void CooldownsRunDuringAbsence()
    {
        FrontierState state = new();
        state.Cooldowns[(int)SkillKind.GoldRain] = 1_000;
        state.StormLeft = 10;
        state.FrenzyLeft = 10;
        FrontierGame game = new(state, new Random(1));
        game.Absent(400);
        Assert.Equal(600, state.Cooldowns[(int)SkillKind.GoldRain], 6);
        Assert.Equal(0, state.StormLeft);
        Assert.Equal(0, state.FrenzyLeft);
    }
}
