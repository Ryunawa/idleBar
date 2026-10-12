using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class BattleTests
{
    [Fact]
    public void FiveKillsAdvanceTheStage()
    {
        FrontierGame game = Strong(new FrontierState());
        Run(game, 30);
        Assert.True(game.State.Stage > 1);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is StageReached { Stage: 2 });
    }

    [Fact]
    public void AFailedBossSendsBackOneStage()
    {
        FrontierState state = new() { Stage = 10, MaxStage = 10 };
        FrontierGame game = new(state, new Random(1));
        Run(game, Balance.BossSeconds + 5);
        Assert.Equal(9, state.Stage);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is BossFailed);
    }

    [Fact]
    public void TheBelfryGivesTheBossMoreTime()
    {
        FrontierState state = new() { Stage = 10, MaxStage = 10 };
        Village.Build(state, BuildingKind.Belfry, 12);
        FrontierGame game = new(state, new Random(1));
        Run(game, Balance.BossSeconds + 5);
        Assert.Equal(10, state.Stage);
        Assert.Equal(Balance.BossSeconds + 12, state.Monster?.BossTotal);
        Run(game, 10);
        Assert.Equal(9, state.Stage);
        Assert.True(state.BossHeld);
    }

    [Fact]
    public void BeatingABossConquersTheNextSlot()
    {
        FrontierState state = new() { Stage = 10, MaxStage = 10 };
        FrontierGame game = Strong(state);
        Run(game, 10);
        IReadOnlyList<FrontierEvent> events = game.Events.Drain();
        Assert.True(state.MaxStage >= 11);
        Assert.Contains(events, frontierEvent => frontierEvent is PlotConquered { Plot: 2 });
        Assert.True(Economy.Unlocked(state, 2));
    }

    [Fact]
    public void AStrikeNeedsAMonsterInFront()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        Assert.False(game.Strike());
        Run(game, 1.5);
        Assert.True(game.Strike());
        Assert.Equal(1, game.State.Clicks);
    }

    [Fact]
    public void KillsPayGold()
    {
        FrontierGame game = Strong(new FrontierState());
        Run(game, 10);
        double paid = game.Events.Drain().OfType<Killed>().Sum(killed => killed.Gold);
        Assert.True(paid > 0);
        Assert.True(game.State.Gold >= paid);
    }

    internal static FrontierGame Strong(FrontierState state)
    {
        state.Fighters[(int)FighterKind.Hero] = 200;
        return new FrontierGame(state, new Random(1));
    }

    internal static void Run(FrontierGame game, double seconds)
    {
        for (double elapsed = 0; elapsed < seconds; elapsed += 1)
        {
            game.Advance(1);
        }
    }
}
