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
    Guid? SavedBy = null,
    int Armor = 0,
    string? HeroName = null,
    string? HeroRole = null,
    int RoleRetreats = 0,
    int HeroSkin = 0,
    int HeroHair = 0,
    int HeroHairColor = 0,
    int HeroOutfit = 0,
    IReadOnlyList<string?>? Companions = null,
    IReadOnlyList<double>? Xp = null,
    IReadOnlyList<string>? Skills = null,
    IReadOnlyList<int>? SkillLevels = null,
    IReadOnlyList<string?>? Slots = null,
    IReadOnlyList<bool>? Placed = null,
    IReadOnlyList<double>? SkillCooldowns = null)
{
    public HeroLook? Hero => HeroName is null ? null : new HeroLook(HeroName, HeroSkin, HeroHair, HeroHairColor, HeroOutfit);

    public FrontierSave ToSave() => WithHero(new FrontierSave(
        Skills is null ? SaveCodec.LevelCurveVersion : SaveCodec.CurrentVersion, Gold, LifetimeGold, Souls, Retreats, Stage, MaxStage, BestStage, Kills, BossHeld,
        Fighters, Plots, Levels, Cooldowns, Achievements,
        MonstersKilled, BossesKilled, Chickens, Clicks, SkillsUsed, Armor: Armor, Companions: Companions, Xp: Xp,
        Skills: Skills, SkillLevels: SkillLevels, Slots: Slots, Placed: Placed, SkillCooldowns: SkillCooldowns));

    public FrontierSave WithHero(FrontierSave save) => save with { Role = HeroRole, RoleRetreats = RoleRetreats, Hero = Hero };
}
