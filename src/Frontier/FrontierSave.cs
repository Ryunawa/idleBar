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
    bool BossHeld,
    IReadOnlyList<int> Fighters,
    IReadOnlyList<string?> Plots,
    IReadOnlyList<int> Levels,
    IReadOnlyList<double> Cooldowns,
    IReadOnlyList<string> Achievements,
    long MonstersKilled,
    long BossesKilled,
    long Chickens,
    long Clicks,
    long SkillsUsed,
    string? Role = null,
    int RoleRetreats = 0,
    int Armor = 0,
    HeroLook? Hero = null,
    IReadOnlyList<string?>? Companions = null,
    IReadOnlyList<double>? Xp = null);
