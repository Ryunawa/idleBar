using System;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record FrontierData(
    int Revision,
    DateTimeOffset SavedAt,
    double AwaySeconds,
    double Gold,
    double LifetimeGold,
    int Souls,
    int Retreats,
    int Stage,
    int MaxStage,
    int BestStage,
    int Kills,
    bool BossHeld,
    bool IntendantUnlocked,
    bool IntendantOn,
    long MonstersKilled,
    long BossesKilled,
    long Chickens,
    long Clicks,
    long SkillsUsed,
    IReadOnlyList<int> Fighters,
    IReadOnlyList<string?> Plots,
    IReadOnlyList<int> Levels,
    IReadOnlyList<double> Cooldowns,
    IReadOnlyList<string> Achievements,
    Guid? SavedBy = null)
{
    public FrontierSave ToSave() => new(
        SaveCodec.CurrentVersion, Gold, LifetimeGold, Souls, Retreats, Stage, MaxStage, BestStage, Kills, BossHeld,
        Fighters, Plots, Levels, Cooldowns, IntendantUnlocked, IntendantOn, Achievements,
        MonstersKilled, BossesKilled, Chickens, Clicks, SkillsUsed);
}
