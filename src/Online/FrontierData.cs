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
    Guid? SavedBy = null,
    int Armor = 0,
    string? HeroName = null,
    string? HeroRole = null,
    int RoleRetreats = 0,
    int HeroSkin = 0,
    int HeroHair = 0,
    int HeroHairColor = 0,
    int HeroOutfit = 0)
{
    public HeroLook? Hero => HeroName is null ? null : new HeroLook(HeroName, HeroSkin, HeroHair, HeroHairColor, HeroOutfit);

    public FrontierSave ToSave() => WithHero(new FrontierSave(
        SaveCodec.CurrentVersion, Gold, LifetimeGold, Souls, Retreats, Stage, MaxStage, BestStage, Kills, BossHeld,
        Fighters, Plots, Levels, Cooldowns, IntendantUnlocked, IntendantOn, Achievements,
        MonstersKilled, BossesKilled, Chickens, Clicks, SkillsUsed, Armor: Armor));

    public FrontierSave WithHero(FrontierSave save) => save with { Role = HeroRole, RoleRetreats = RoleRetreats, Hero = Hero };
}
