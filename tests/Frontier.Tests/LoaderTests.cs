using System;
using IdleBar.Frontier;
using IdleBar.Online;
using Xunit;

namespace IdleBar.Tests;

public sealed class LoaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AFreshServerGameStartsAtTheGate()
    {
        LoadPlan plan = FrontierLoader.Plan(Data(new FrontierState(), 0, 2), null, Now);
        Assert.Equal(1, plan.State.Stage);
        Assert.Equal(1, plan.State.Level(FighterKind.Hero));
        Assert.All(plan.State.Built, kind => Assert.Null(kind));
        Assert.Equal(0, plan.State.Souls);
        Assert.False(plan.Unsent);
    }

    [Fact]
    public void AServerSaveIsRestoredWithItsAbsence()
    {
        FrontierState state = new() { Stage = 33, MaxStage = 33, BestStage = 33, Gold = 900 };
        Village.Build(state, BuildingKind.Forge, 5, 0);
        LoadPlan plan = FrontierLoader.Plan(Data(state, 4, 7200), null, Now);
        Assert.Equal(33, plan.State.Stage);
        Assert.Equal(BuildingKind.Forge, plan.State.Built[0]);
        Assert.Equal(5, plan.State.Level(BuildingKind.Forge));
        Assert.Equal(7200, plan.Away);
        Assert.False(plan.Unsent);
    }

    [Fact]
    public void AnUnsentLocalCopyOfTheSameRevisionWins()
    {
        LocalSave local = new("player", 4, true, Now.AddMinutes(-30), SaveCodec.Capture(new FrontierState { Stage = 26, MaxStage = 26 }));
        LoadPlan plan = FrontierLoader.Plan(Data(new FrontierState { Stage = 20, MaxStage = 20 }, 4, 7200), local, Now);
        Assert.Equal(26, plan.State.Stage);
        Assert.Equal(1800, plan.Away, 3);
        Assert.True(plan.Unsent);
    }

    [Fact]
    public void AStaleLocalCopyLosesToANewerServerSave()
    {
        LocalSave local = new("player", 3, true, Now.AddMinutes(-30), SaveCodec.Capture(new FrontierState { Stage = 26, MaxStage = 26 }));
        LoadPlan plan = FrontierLoader.Plan(Data(new FrontierState { Stage = 40, MaxStage = 40 }, 5, 60), local, Now);
        Assert.Equal(40, plan.State.Stage);
        Assert.Equal(60, plan.Away);
    }

    [Fact]
    public void TheSaveArgumentsCarryEveryPlot()
    {
        FrontierState state = new();
        Village.Build(state, BuildingKind.Mill, 2, 1);
        SaveArguments arguments = SaveArguments.From(SaveCodec.Capture(state), 7, Guid.Empty);
        Assert.Equal(7, arguments.PRevision);
        Assert.Equal([null, "mill", null, null, null, null, null, null, null, null, null, null], arguments.PPlots);
        Assert.Equal([0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], arguments.PLevels);
    }

    [Fact]
    public void ALostReplyDoesNotRollTheGameBack()
    {
        Guid mine = Guid.NewGuid();
        LocalSave local = new("player", 4, true, Now.AddMinutes(-30), SaveCodec.Capture(new FrontierState { Stage = 26, MaxStage = 26 }), mine);
        LoadPlan plan = FrontierLoader.Plan(Data(new FrontierState { Stage = 20, MaxStage = 20 }, 5, 7200, mine), local, Now);
        Assert.Equal(26, plan.State.Stage);
        Assert.True(plan.Unsent);
    }

    [Fact]
    public void AnotherPcSaveStillWins()
    {
        LocalSave local = new("player", 4, true, Now.AddMinutes(-30), SaveCodec.Capture(new FrontierState { Stage = 26, MaxStage = 26 }), Guid.NewGuid());
        LoadPlan plan = FrontierLoader.Plan(Data(new FrontierState { Stage = 40, MaxStage = 40 }, 5, 60, Guid.NewGuid()), local, Now);
        Assert.Equal(40, plan.State.Stage);
        Assert.False(plan.Unsent);
    }

    private static FrontierData Data(FrontierState state, int revision, double away, Guid? savedBy = null)
    {
        FrontierSave save = SaveCodec.Capture(state);
        return new FrontierData(
            revision, Now, away, save.Gold, save.LifetimeGold, save.Souls, save.Retreats, save.Stage, save.MaxStage,
            save.BestStage, save.Kills, save.BossHeld, save.IntendantUnlocked, save.IntendantOn, save.MonstersKilled,
            save.BossesKilled, save.Chickens, save.Clicks, save.SkillsUsed, save.Fighters, save.Plots, save.Levels,
            save.Cooldowns, save.Achievements, savedBy);
    }
}
