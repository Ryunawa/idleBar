using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace IdleBar.Frontier;

public static class SaveCodec
{
    public const int CurrentVersion = 5;
    public const int OldestVersion = 2;
    public const int LevelCurveVersion = 4;
    public const int TreeVersion = 5;

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
        state.BossHeld,
        [.. state.Fighters],
        [.. state.Built.Select(kind => kind is BuildingKind built ? Buildings.Of(built).Id : null)],
        [.. state.Levels],
        SkillSave.LegacyCooldowns(state),
        [.. state.Achievements.Order(StringComparer.Ordinal)],
        state.MonstersKilled,
        state.BossesKilled,
        state.Chickens,
        state.Clicks,
        state.SkillsUsed,
        Roles.Of(state.Role).Id,
        state.RoleRetreats,
        state.Armor,
        state.Hero,
        [.. state.Companions.Select(role => role is HeroRole picked ? Roles.Of(picked).Id : null)],
        [.. state.Xp],
        SkillSave.Ids(state),
        SkillSave.Levels(state),
        SkillSave.SlotIds(state),
        SkillSave.PlacedFlags(state),
        SkillSave.Cooldowns(state));

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
            BossHeld = save.BossHeld && !Economy.IsBossStage(stage),
            Fighters = FighterLevels(save),
            MonstersKilled = save.MonstersKilled,
            BossesKilled = save.BossesKilled,
            Chickens = save.Chickens,
            Clicks = save.Clicks,
            SkillsUsed = save.SkillsUsed,
            Role = Roles.Parse(save.Role) ?? HeroRole.Warrior,
            RoleRetreats = Math.Clamp(save.RoleRetreats, 0, Math.Max(0, save.Retreats)),
            Armor = Math.Max(0, save.Armor),
            Hero = save.Hero is HeroLook hero && hero.IsValid() ? hero : null,
            Companions = [.. Enumerable.Range(0, FrontierState.FighterCount - 1).Select(index => Roles.Parse(save.Companions?.ElementAtOrDefault(index)))],
            Xp = save.Version >= LevelCurveVersion ? Fit(save.Xp ?? [], FrontierState.FighterCount) : new double[FrontierState.FighterCount],
        };
        state.Fighters[(int)FighterKind.Hero] = Math.Max(1, state.Fighters[(int)FighterKind.Hero]);
        PlaceBuildings(state, save);
        state.Achievements.UnionWith(save.Achievements.Where(Achievements.Known));
        if (save.Version >= TreeVersion)
        {
            SkillSave.Restore(state, save);
        }
        else
        {
            SkillSave.Grant(state, save.Cooldowns);
        }

        return state;
    }

    public static bool Valid(FrontierSave save) =>
        double.IsFinite(save.Gold) && double.IsFinite(save.LifetimeGold) && save.Cooldowns.All(double.IsFinite) && (save.Xp ?? []).All(double.IsFinite)
        && (save.SkillCooldowns ?? []).All(double.IsFinite);

    public static string Serialize(FrontierSave save) => JsonSerializer.Serialize(save, Options);

    public static FrontierSave? Deserialize(string json)
    {
        FrontierSave? save = JsonSerializer.Deserialize<FrontierSave>(json, Options);
        return Complete(save) ? save : null;
    }

    public static bool Complete(FrontierSave? save) =>
        save is { Version: >= OldestVersion and <= CurrentVersion, Fighters: not null, Plots: not null, Levels: not null, Cooldowns: not null, Achievements: not null };

    private static int[] FighterLevels(FrontierSave save)
    {
        int[] levels = Fit(save.Fighters, FrontierState.FighterCount);
        return save.Version >= LevelCurveVersion
            ? levels
            : [.. levels.Select(level => level > 0 ? Math.Max(1, (int)Math.Round(level / Balance.OldLevelsPerLevel)) : 0)];
    }

    private static void PlaceBuildings(FrontierState state, FrontierSave save)
    {
        int[] levels = Fit(save.Levels, FrontierState.PlotCount);
        for (int plot = 0; plot < FrontierState.PlotCount; plot++)
        {
            BuildingKind? kind = plot < save.Plots.Count ? Buildings.Parse(save.Plots[plot]) : null;
            if (kind is BuildingKind built && state.PlotOf(built) is null)
            {
                state.Built[plot] = built;
                state.Levels[plot] = Math.Max(1, levels[plot]);
            }
        }
    }

    private static int[] Fit(IReadOnlyList<int> values, int length) =>
        [.. Enumerable.Range(0, length).Select(index => index < values.Count ? Math.Max(0, values[index]) : 0)];

    private static double[] Fit(IReadOnlyList<double> values, int length) =>
        [.. Enumerable.Range(0, length).Select(index => index < values.Count && double.IsFinite(values[index]) ? Math.Max(0, values[index]) : 0)];
}
