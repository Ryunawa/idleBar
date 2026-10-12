using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed record FrontierSave(
    int Version,
    double Gold,
    double LifetimeGold,
    int Souls,
    int Retreats,
    int Stage,
    int MaxStage,
    int BestStage,
    int Kills,
    IReadOnlyList<int> Fighters,
    IReadOnlyList<int> Buildings,
    IReadOnlyList<double> Cooldowns,
    bool IntendantUnlocked,
    bool IntendantOn,
    IReadOnlyList<string> Achievements,
    long MonstersKilled,
    long BossesKilled,
    long Chickens,
    long Clicks,
    long SkillsUsed);
