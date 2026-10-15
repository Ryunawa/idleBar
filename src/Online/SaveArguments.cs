using System;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record SaveArguments(
    int PRevision,
    Guid PSession,
    double PGold,
    double PLifetimeGold,
    long PSouls,
    int PRetreats,
    int PStage,
    int PMaxStage,
    int PBestStage,
    int PKills,
    bool PBossHeld,
    IReadOnlyList<int> PFighters,
    IReadOnlyList<string?> PPlots,
    IReadOnlyList<int> PLevels,
    IReadOnlyList<double> PCooldowns,
    IReadOnlyList<string> PAchievements,
    long PMonstersKilled,
    long PBossesKilled,
    long PChickens,
    long PClicks,
    long PSkillsUsed,
    int PArmor,
    IReadOnlyList<string?> PCompanions,
    IReadOnlyList<double> PXp,
    IReadOnlyList<string> PSkills,
    IReadOnlyList<int> PSkillLevels,
    IReadOnlyList<string?> PSlots,
    IReadOnlyList<bool> PPlaced,
    IReadOnlyList<double> PSkillCooldowns)
{
    public static SaveArguments From(FrontierSave save, int revision, Guid session) => new(
        revision, session, save.Gold, save.LifetimeGold, save.Souls, save.Retreats, save.Stage, save.MaxStage, save.BestStage,
        save.Kills, save.BossHeld, save.Fighters, save.Plots, save.Levels, save.Cooldowns,
        save.Achievements, save.MonstersKilled, save.BossesKilled, save.Chickens, save.Clicks, save.SkillsUsed, save.Armor, save.Companions ?? [], save.Xp ?? [],
        save.Skills ?? [], save.SkillLevels ?? [], save.Slots ?? [], save.Placed ?? [], save.SkillCooldowns ?? []);
}
