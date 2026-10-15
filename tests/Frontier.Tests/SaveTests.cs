using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class SaveTests
{
    [Fact]
    public void ASaveRoundTripKeepsTheProgress()
    {
        FrontierState state = new() { Gold = 1234.5, LifetimeGold = 99_999, Souls = 31, Retreats = 1, Stage = 47, MaxStage = 52, BestStage = 61, Kills = 3 };
        Village.Build(state, BuildingKind.Market, 8);
        state.Fighters[(int)FighterKind.Second] = 18;
        state.Learned[(int)SkillKind.Fury] = 1;
        state.Cooldowns[(int)SkillKind.Fury] = 120;
        state.Achievements.Add("boss-1");
        state.Companions[1] = HeroRole.Support;
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
        Assert.Equal(BuildingKind.Market, restored.Built[0]);
        Assert.Equal(18, restored.Level(FighterKind.Second));
        Assert.Equal(120, restored.Cooldowns[(int)SkillKind.Fury]);
        Assert.Contains("boss-1", restored.Achievements);
        Assert.Equal(HeroRole.Support, restored.ClassOf(FighterKind.Second));
        Assert.Equal(HeroRole.Archer, restored.ClassOf(FighterKind.First));
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
    public void AnOddSaveIsRepairedWhenLoaded()
    {
        FrontierSave save = SaveCodec.Capture(new FrontierState()) with { Plots = ["farm", "farm", "castle"], Levels = [3, 2], Fighters = [0], Stage = 0 };
        FrontierState restored = SaveCodec.Restore(save);
        Assert.Equal(FrontierState.PlotCount, restored.Built.Length);
        Assert.Equal(FrontierState.PlotCount, restored.Levels.Length);
        Assert.Equal(BuildingKind.Farm, restored.Built[0]);
        Assert.Equal(3, restored.Level(BuildingKind.Farm));
        Assert.Null(restored.Built[1]);
        Assert.Null(restored.Built[2]);
        Assert.Equal(0, restored.Levels[1]);
        Assert.Equal(1, restored.Level(FighterKind.Hero));
        Assert.Equal(1, restored.Stage);
    }

    [Fact]
    public void UnknownAchievementsAreDroppedOnLoad()
    {
        FrontierSave save = SaveCodec.Capture(new FrontierState()) with { Achievements = ["boss-1", "triche-1", "triche-2"] };
        FrontierState restored = SaveCodec.Restore(save);
        Assert.Equal(["boss-1"], restored.Achievements);
    }

    [Fact]
    public void TheHeroSurvivesASave()
    {
        FrontierState state = new() { Role = HeroRole.Support, Retreats = 3, RoleRetreats = 2, Armor = 14, Hero = new HeroLook("Aldric", 1, 2, 3, 4) };
        FrontierSave? save = SaveCodec.Deserialize(SaveCodec.Serialize(SaveCodec.Capture(state)));
        Assert.NotNull(save);
        FrontierState restored = SaveCodec.Restore(save);

        Assert.Equal(HeroRole.Support, restored.Role);
        Assert.Equal(2, restored.RoleRetreats);
        Assert.Equal(14, restored.Armor);
        Assert.Equal(state.Hero, restored.Hero);
        Assert.True(restored.CanChangeRole);
    }

    [Fact]
    public void ASaveFromBeforeTheHeroAsksForOne()
    {
        FrontierSave old = SaveCodec.Capture(new FrontierState { Stage = 12, MaxStage = 12 }) with { Version = 2, Role = null, Hero = null };
        FrontierSave? save = SaveCodec.Deserialize(SaveCodec.Serialize(old));
        Assert.NotNull(save);
        FrontierState restored = SaveCodec.Restore(save);

        Assert.Equal(12, restored.Stage);
        Assert.Equal(HeroRole.Warrior, restored.Role);
        Assert.Null(restored.Hero);
        Assert.True(restored.CanChangeRole);
    }

    [Fact]
    public void ASaveFromBeforeTheSteeperLevelsIsRescaled()
    {
        FrontierState state = new() { Stage = 80, MaxStage = 80, Fighters = [300, 150, 0] };
        state.Xp[0] = 5e9;
        FrontierState restored = SaveCodec.Restore(SaveCodec.Capture(state) with { Version = 3 });

        Assert.Equal(new[] { 104, 52, 0 }, restored.Fighters);
        Assert.All(restored.Xp, xp => Assert.Equal(0, xp));
        Assert.Equal(new[] { 300, 150, 0 }, SaveCodec.Restore(SaveCodec.Capture(state)).Fighters);
    }

    [Fact]
    public void ASaveFromTheJsonEraIsIgnored()
    {
        Assert.Null(SaveCodec.Deserialize("{\"version\":1,\"fighters\":[1,0,0],\"buildings\":[1,0,0,0,0,0,0],\"cooldowns\":[0,0,0],\"achievements\":[]}"));
    }
}
