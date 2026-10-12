using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace IdleBar.Frontier;

public static class SaveCodec
{
    public const int CurrentVersion = 1;

    public static JsonSerializerOptions Options { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public static FrontierSave Capture(FrontierState state) => new(
        CurrentVersion,
        state.Gold,
        state.LifetimeGold,
        state.Souls,
        state.Retreats,
        state.Stage,
        state.MaxStage,
        state.BestStage,
        state.Kills,
        [.. state.Fighters],
        [.. state.Buildings],
        [.. state.Cooldowns],
        state.IntendantUnlocked,
        state.IntendantOn,
        [.. state.Achievements.Order(StringComparer.Ordinal)],
        state.MonstersKilled,
        state.BossesKilled,
        state.Chickens,
        state.Clicks,
        state.SkillsUsed,
        state.BossHeld);

    public static FrontierState Restore(FrontierSave save)
    {
        int stage = Math.Max(1, save.Stage);
        int maxStage = Math.Max(stage, save.MaxStage);
        FrontierState state = new()
        {
            Gold = Math.Max(0, save.Gold),
            LifetimeGold = Math.Max(0, save.LifetimeGold),
            Souls = Math.Max(0, save.Souls),
            Retreats = Math.Max(0, save.Retreats),
            Stage = stage,
            MaxStage = maxStage,
            BestStage = Math.Max(maxStage, save.BestStage),
            Kills = Math.Clamp(save.Kills, 0, Balance.MonstersPerStage - 1),
            BossHeld = save.BossHeld && Economy.IsBossStage(stage + 1),
            Fighters = Fit(save.Fighters, FrontierState.FighterCount),
            Buildings = Fit(save.Buildings, FrontierState.BuildingCount),
            Cooldowns = Fit(save.Cooldowns, FrontierState.SkillCount),
            IntendantUnlocked = save.IntendantUnlocked,
            IntendantOn = save.IntendantOn && save.IntendantUnlocked,
            MonstersKilled = save.MonstersKilled,
            BossesKilled = save.BossesKilled,
            Chickens = save.Chickens,
            Clicks = save.Clicks,
            SkillsUsed = save.SkillsUsed,
        };
        state.Fighters[(int)FighterKind.Hero] = Math.Max(1, state.Fighters[(int)FighterKind.Hero]);
        state.Achievements.UnionWith(save.Achievements);
        return state;
    }

    public static bool Valid(FrontierSave save) =>
        double.IsFinite(save.Gold) && double.IsFinite(save.LifetimeGold) && save.Cooldowns.All(double.IsFinite);

    public static string Serialize(FrontierSave save) => JsonSerializer.Serialize(save, Options);

    public static FrontierSave? Deserialize(string json)
    {
        FrontierSave? save = JsonSerializer.Deserialize<FrontierSave>(json, Options);
        bool complete = save is { Version: > 0, Fighters: not null, Buildings: not null, Cooldowns: not null, Achievements: not null };
        return complete ? save : null;
    }

    private static int[] Fit(IReadOnlyList<int> values, int length) =>
        [.. Enumerable.Range(0, length).Select(index => index < values.Count ? Math.Max(0, values[index]) : 0)];

    private static double[] Fit(IReadOnlyList<double> values, int length) =>
        [.. Enumerable.Range(0, length).Select(index => index < values.Count && double.IsFinite(values[index]) ? Math.Max(0, values[index]) : 0)];
}
