using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class SaveTests
{
    [Fact]
    public void ASaveRoundTripKeepsTheProgress()
    {
        FrontierState state = new() { Gold = 1234.5, LifetimeGold = 99_999, Souls = 31, Retreats = 1, Stage = 47, MaxStage = 52, BestStage = 61, Kills = 3 };
        state.Buildings[(int)BuildingKind.Market] = 8;
        state.Fighters[(int)FighterKind.Mage] = 18;
        state.Cooldowns[(int)SkillKind.Fury] = 120;
        state.Achievements.Add("boss-1");
        state.IntendantUnlocked = true;
        state.IntendantOn = true;
        state.MonstersKilled = 400;

        FrontierSave? save = SaveCodec.Deserialize(SaveCodec.Serialize(SaveCodec.Capture(state)));
        Assert.NotNull(save);
        FrontierState restored = SaveCodec.Restore(save);

        Assert.Equal(1234.5, restored.Gold);
        Assert.Equal(31, restored.Souls);
        Assert.Equal(47, restored.Stage);
        Assert.Equal(52, restored.MaxStage);
        Assert.Equal(61, restored.BestStage);
        Assert.Equal(3, restored.Kills);
        Assert.Equal(8, restored.Level(BuildingKind.Market));
        Assert.Equal(18, restored.Level(FighterKind.Mage));
        Assert.Equal(120, restored.Cooldowns[(int)SkillKind.Fury]);
        Assert.Contains("boss-1", restored.Achievements);
        Assert.True(restored.IntendantOn);
        Assert.Equal(400, restored.MonstersKilled);
    }

    [Fact]
    public void TheJsonUsesSnakeCase()
    {
        string json = SaveCodec.Serialize(SaveCodec.Capture(new FrontierState()));
        Assert.Contains("\"lifetime_gold\"", json);
        Assert.Contains("\"max_stage\"", json);
    }

    [Fact]
    public void NonFiniteGoldIsInvalid()
    {
        FrontierSave save = SaveCodec.Capture(new FrontierState { Gold = double.PositiveInfinity });
        Assert.False(SaveCodec.Valid(save));
        Assert.True(SaveCodec.Valid(SaveCodec.Capture(new FrontierState { Gold = 10 })));
    }

    [Fact]
    public void AnOlderSaveWithFewerBuildingsStillLoads()
    {
        FrontierSave save = SaveCodec.Capture(new FrontierState()) with { Buildings = [3, 2], Fighters = [0], Stage = 0 };
        FrontierState restored = SaveCodec.Restore(save);
        Assert.Equal(FrontierState.BuildingCount, restored.Buildings.Length);
        Assert.Equal(3, restored.Level(BuildingKind.Farm));
        Assert.Equal(0, restored.Level(BuildingKind.Portal));
        Assert.Equal(1, restored.Level(FighterKind.Hero));
        Assert.Equal(1, restored.Stage);
    }
}
