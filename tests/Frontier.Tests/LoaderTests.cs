using System;
using System.Text.Json;
using IdleBar.Frontier;
using IdleBar.Online;
using Xunit;

namespace IdleBar.Tests;

public sealed class LoaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AnEmptyServerSaveStartsANewGameWithTheGift()
    {
        LoadPlan plan = FrontierLoader.Plan(Data("{}", 0, 3600, 10, "Le Pot d'Étain"), null, Now);
        Assert.Equal(10, plan.State.Souls);
        Assert.Equal(1, plan.State.Stage);
        Assert.Equal("Le Pot d'Étain", plan.State.VeteranName);
        Assert.Equal(0, plan.Away);
        Assert.False(plan.Unsent);
    }

    [Fact]
    public void AServerSaveIsRestoredWithItsAbsence()
    {
        string json = SaveCodec.Serialize(SaveCodec.Capture(new FrontierState { Stage = 33, MaxStage = 33, Gold = 900 }));
        LoadPlan plan = FrontierLoader.Plan(Data(json, 4, 7200, 0, null), null, Now);
        Assert.Equal(33, plan.State.Stage);
        Assert.Equal(7200, plan.Away);
        Assert.False(plan.Unsent);
    }

    [Fact]
    public void AnUnsentLocalCopyOfTheSameRevisionWins()
    {
        string json = SaveCodec.Serialize(SaveCodec.Capture(new FrontierState { Stage = 20, MaxStage = 20 }));
        LocalSave local = new("player", 4, true, Now.AddMinutes(-30), SaveCodec.Capture(new FrontierState { Stage = 26, MaxStage = 26 }));
        LoadPlan plan = FrontierLoader.Plan(Data(json, 4, 7200, 0, null), local, Now);
        Assert.Equal(26, plan.State.Stage);
        Assert.Equal(1800, plan.Away, 3);
        Assert.True(plan.Unsent);
    }

    [Fact]
    public void AStaleLocalCopyLosesToANewerServerSave()
    {
        string json = SaveCodec.Serialize(SaveCodec.Capture(new FrontierState { Stage = 40, MaxStage = 40 }));
        LocalSave local = new("player", 3, true, Now.AddMinutes(-30), SaveCodec.Capture(new FrontierState { Stage = 26, MaxStage = 26 }));
        LoadPlan plan = FrontierLoader.Plan(Data(json, 5, 60, 0, null), local, Now);
        Assert.Equal(40, plan.State.Stage);
        Assert.Equal(60, plan.Away);
    }

    private static FrontierData Data(string save, int revision, double away, int gift, string? name) =>
        new(JsonDocument.Parse(save).RootElement.Clone(), revision, Now, away, gift, name);
}
