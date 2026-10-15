using System;
using System.Linq;
using System.Text.Json;
using IdleBar.Cloud;
using IdleBar.Frontier;
using IdleBar.Online;
using Xunit;

namespace IdleBar.Tests;

public sealed class SkillSaveTests
{
    [Fact]
    public void AFormatFiveSaveKeepsTheTreeAndTheSlots()
    {
        FrontierState state = new() { Stage = 40, MaxStage = 40, BestStage = 61 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Fury);
        game.Learn(SkillKind.LastingRage);
        game.Learn(SkillKind.LastingRage);
        game.Learn(SkillKind.GoldRain);
        game.Place(2, SkillKind.Fury);
        game.Place(0, SkillKind.GoldRain);
        state.Cooldowns[(int)SkillKind.Fury] = 42.5;
        state.Cooldowns[(int)SkillKind.GoldRain] = 7;

        FrontierSave? save = SaveCodec.Deserialize(SaveCodec.Serialize(SaveCodec.Capture(state)));
        Assert.NotNull(save);
        Assert.Equal(5, save.Version);
        FrontierState restored = SaveCodec.Restore(save);

        Assert.Equal(2, SkillTree.Level(restored, SkillKind.LastingRage));
        Assert.Equal(4, SkillTree.Spent(restored));
        Assert.Equal([SkillKind.GoldRain, null, SkillKind.Fury], restored.Slots);
        Assert.Equal(["fury", "lasting_rage", "gold_rain"], save.Skills);
        Assert.Equal([42.5, 0, 7], save.SkillCooldowns);
        Assert.Equal([0, 42.5, 7], save.Cooldowns);
        Assert.Equal(42.5, restored.Cooldowns[(int)SkillKind.Fury]);
        Assert.Equal(7, restored.Cooldowns[(int)SkillKind.GoldRain]);
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(15, 1)]
    [InlineData(27, 2)]
    [InlineData(31, 3)]
    [InlineData(120, 3)]
    public void AFormatFourSaveKeepsItsSkillsWithinItsPoints(int best, int granted)
    {
        SkillKind[] legacy = [SkillKind.Smite, SkillKind.Fury, SkillKind.GoldRain];
        FrontierSave old = SaveCodec.Capture(new FrontierState { Stage = 1, MaxStage = 1, BestStage = best }) with
        {
            Version = 4, Cooldowns = [5, 66.5, 1_700], Skills = null, SkillLevels = null, Slots = null,
        };

        FrontierState restored = SaveCodec.Restore(old);

        double[] cooldowns = [5, 66.5, 1_700];
        Assert.Equal(granted, SkillTree.Spent(restored));
        for (int slot = 0; slot < legacy.Length; slot++)
        {
            Assert.Equal(slot < granted ? legacy[slot] : null, restored.Slots[slot]);
            Assert.Equal(slot < granted, SkillTree.Known(restored, legacy[slot]));
            Assert.Equal(slot < granted ? cooldowns[slot] : 0, SkillCasts.Wait(restored, legacy[slot]));
        }

        Assert.Equal(cooldowns.Take(granted), SaveCodec.Capture(restored).SkillCooldowns);
    }

    [Fact]
    public void ThePlacementsOfTheRunTravelWithTheSave()
    {
        FrontierState state = new() { Stage = 40, MaxStage = 40, BestStage = 61 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Smite);
        game.Learn(SkillKind.Fury);
        game.Learn(SkillKind.GoldRain);
        game.Place(0, SkillKind.Fury);
        game.Place(1, SkillKind.GoldRain);
        game.Place(1, null);

        FrontierSave? save = SaveCodec.Deserialize(SaveCodec.Serialize(SaveCodec.Capture(state)));
        Assert.NotNull(save);
        Assert.Equal([false, true, true], save.Placed);
        FrontierState restored = SaveCodec.Restore(save);
        Assert.Equal(state.PlacedThisRun, restored.PlacedThisRun);
        Assert.True(SkillTree.RestartsIfPlaced(restored, SkillKind.GoldRain));
        Assert.Equal(save.Placed, Data(state).ToSave().Placed);
        Assert.Equal([false, true, true], SaveArguments.From(save, 1, Guid.Empty).PPlaced);
        Assert.Contains("\"p_placed\":[false,true,true]", JsonSerializer.Serialize(SaveArguments.From(save, 1, Guid.Empty), SupabaseJson.Options));

        FrontierState slotted = SaveCodec.Restore(save with { Placed = null });
        Assert.True(slotted.PlacedThisRun[(int)SkillKind.Fury]);
        Assert.False(slotted.PlacedThisRun[(int)SkillKind.GoldRain]);
        FrontierState granted = SaveCodec.Restore(save with { Version = 4, Cooldowns = [0, 0, 0], Skills = null, SkillLevels = null, Slots = null, Placed = null });
        Assert.True(granted.PlacedThisRun[(int)SkillKind.Smite] && granted.PlacedThisRun[(int)SkillKind.GoldRain]);
    }

    [Fact]
    public void ARedistributedSkillKeepsItsCooldownThroughAReload()
    {
        FrontierState state = new() { Stage = 5, MaxStage = 5, BestStage = 41 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.GoldRain);
        game.Place(0, SkillKind.GoldRain);
        Assert.True(game.UseSkill(0));
        double cooldown = SkillCasts.Wait(state, SkillKind.GoldRain);
        Assert.True(game.Redistribute());

        FrontierSave? save = SaveCodec.Deserialize(SaveCodec.Serialize(SaveCodec.Capture(state)));
        Assert.NotNull(save);
        Assert.Equal(["gold_rain"], save.Skills);
        Assert.Equal([0], save.SkillLevels);
        Assert.Equal([false], save.Placed);
        FrontierGame reloaded = new(SaveCodec.Restore(save), new Random(1));
        Assert.Equal(0, SkillTree.Spent(reloaded.State));
        Assert.Equal(cooldown, SkillCasts.Wait(reloaded.State, SkillKind.GoldRain));
        Assert.True(reloaded.Learn(SkillKind.GoldRain));
        Assert.True(reloaded.Place(0, SkillKind.GoldRain));
        Assert.False(reloaded.UseSkill(0));
        Assert.Equal(cooldown, SkillCasts.Wait(SaveCodec.Restore(Data(state).ToSave()), SkillKind.GoldRain));
    }

    [Fact]
    public void AnImpossibleTreeIsTrimmedWhenLoaded()
    {
        FrontierSave save = SaveCodec.Capture(new FrontierState { Stage = 30, MaxStage = 30, BestStage = 35 }) with
        {
            Skills = ["storm", "smite", "thunder", "castle", "fury", "lasting_rage", "gold_rain"],
            SkillLevels = [1, 1, 9, 1, 1, 1, 1],
            Slots = ["thunder", "smite", "smite"],
        };

        FrontierState restored = SaveCodec.Restore(save);

        Assert.Equal(3, SkillTree.Spent(restored));
        Assert.Equal(0, SkillTree.Level(restored, SkillKind.Thunder));
        Assert.True(SkillTree.Known(restored, SkillKind.GoldRain));
        Assert.True(SkillTree.Known(restored, SkillKind.Fury));
        Assert.False(SkillTree.Known(restored, SkillKind.Storm));
        Assert.Equal([null, SkillKind.Smite, null], restored.Slots);
    }

    [Fact]
    public void ServerDataWithoutATreeIsMigratedLikeAnOldSave()
    {
        FrontierData data = Data(new FrontierState { Stage = 30, MaxStage = 30, BestStage = 31 }) with { Cooldowns = [0, 120, 0], Skills = null, SkillLevels = null, Slots = null };
        Assert.Equal(SaveCodec.LevelCurveVersion, data.ToSave().Version);
        Assert.Equal([SkillKind.Smite, SkillKind.Fury, SkillKind.GoldRain], SaveCodec.Restore(data.ToSave()).Slots);
        Assert.Equal(120, SkillCasts.Wait(SaveCodec.Restore(data.ToSave()), SkillKind.Fury));
        FrontierData empty = Data(new FrontierState { Stage = 30, MaxStage = 30, BestStage = 30 });
        Assert.Equal(0, SkillTree.Spent(SaveCodec.Restore(empty.ToSave())));
    }

    [Fact]
    public void TheSaveArgumentsCarryTheTree()
    {
        FrontierState state = new() { BestStage = 21 };
        FrontierGame game = new(state, new Random(1));
        game.Learn(SkillKind.Smite);
        game.Learn(SkillKind.Thunder);
        game.Place(1, SkillKind.Smite);
        state.Cooldowns[(int)SkillKind.Smite] = 30;
        SaveArguments arguments = SaveArguments.From(SaveCodec.Capture(state), 3, Guid.Empty);
        Assert.Equal(["smite", "thunder"], arguments.PSkills);
        Assert.Equal([1, 1], arguments.PSkillLevels);
        Assert.Equal([null, "smite", null], arguments.PSlots);
        Assert.Equal([30, 0, 0], arguments.PCooldowns);
        Assert.Equal([30, 0], arguments.PSkillCooldowns);
        string json = JsonSerializer.Serialize(arguments, SupabaseJson.Options);
        Assert.Contains("\"p_skills\":[\"smite\",\"thunder\"]", json);
        Assert.Contains("\"p_skill_levels\":[1,1]", json);
        Assert.Contains("\"p_slots\":[null,\"smite\",null]", json);
        Assert.Contains("\"p_cooldowns\":[30,0,0]", json);
        Assert.Contains("\"p_skill_cooldowns\":[30,0]", json);
    }

    private static FrontierData Data(FrontierState state)
    {
        FrontierSave save = SaveCodec.Capture(state);
        return new FrontierData(
            1, DateTimeOffset.UnixEpoch, 0, save.Gold, save.LifetimeGold, save.Souls, save.Retreats, save.Stage, save.MaxStage,
            save.BestStage, save.Kills, save.BossHeld, save.MonstersKilled, save.BossesKilled, save.Chickens, save.Clicks, save.SkillsUsed,
            save.Fighters, save.Plots, save.Levels, save.Cooldowns, save.Achievements,
            Skills: save.Skills, SkillLevels: save.SkillLevels, Slots: save.Slots, Placed: save.Placed, SkillCooldowns: save.SkillCooldowns);
    }
}
