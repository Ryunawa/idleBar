using System;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record SocialData(
    string Status,
    DateTimeOffset ServerTime,
    GuildData? Guild,
    double RejoinIn,
    HelpData? Help,
    TitanData? Titan,
    ItemsData? Items = null,
    int? Revision = null,
    double? Spent = null,
    ShopData? Shop = null)
{
    public const string Ok = "ok";
    public const string Missing = "missing";
    public const string NoHero = "no_hero";
    public const string AlreadyMember = "already_member";
    public const string Cooldown = "cooldown";
    public const string InvalidName = "invalid_name";
    public const string BannedName = "banned_name";
    public const string NameTaken = "name_taken";
    public const string UnknownCode = "unknown_code";
    public const string Full = "full";
    public const string NotMember = "not_member";
    public const string NotLeader = "not_leader";
    public const string UnknownMember = "unknown_member";
    public const string Self = "self";
    public const string FrontFull = "front_full";
    public const string ConvoyFull = "convoy_full";
    public const string Nothing = "nothing";
    public const string UnknownItem = "unknown_item";
    public const string WrongClass = "wrong_class";
    public const string BagFull = "bag_full";
    public const string TooManyTries = "too_many_tries";
    public const string Paused = "paused";
    public const string TooPoor = "too_poor";
    public const string UnknownFighter = "unknown_fighter";
    public const string Conflict = "conflict";
    public const string Elsewhere = "elsewhere";
    public const string Maxed = "maxed";
    public const string Refused = "refused";
    public const string UnknownOffer = "unknown_offer";
    public const string Sold = "sold";
    public const string LevelTooLow = "level_too_low";
}

public sealed record GuildData(string Name, string Code, bool Leader, IReadOnlyList<MemberData> Members);

public sealed record MemberData(
    long Id,
    string Name,
    string? Role,
    int Skin,
    int Hair,
    int HairColor,
    int Outfit,
    int Level,
    int BestStage,
    bool Leader,
    bool Me,
    double AwaySeconds,
    int Heroes,
    double ConvoyIn)
{
    public HeroLook Look => new(Name, Skin, Hair, HairColor, Outfit);

    public HeroRole Kind => Roles.Parse(Role) ?? HeroRole.Warrior;
}

public sealed record HelpData(double HeroReadyIn, double ConvoyReadyIn, IReadOnlyList<AllyData> Allies, IReadOnlyList<BoostData> Boosts);

public sealed record AllyData(long Id, string Name, string Role, int Skin, int Hair, int HairColor, int Outfit, double Ratio, double EndsIn)
{
    public Ally ToAlly() => new(Id, new HeroLook(Name, Skin, Hair, HairColor, Outfit), Roles.Parse(Role) ?? HeroRole.Warrior, Ratio, EndsIn);
}

public sealed record BoostData(string Source, int Gold, int Damage, double EndsIn)
{
    public Boost ToBoost() => new(Source, Gold, Damage, EndsIn);
}

public sealed record TitanData(
    int Kind,
    long Hp,
    long Damage,
    bool Fallen,
    double EndsIn,
    long Mine,
    TitanReward? Claimable,
    IReadOnlyList<RankData> Ranking,
    string Week = "",
    int Fighters = 0,
    IReadOnlyList<TitanTier>? Tiers = null,
    LastTitan? Last = null)
{
    public TitanView ToView() => new(Week, Kind, Hp, Damage, Fallen, Mine, Claimable is not null);
}

public sealed record TitanTier(long Points, int Percent, int Hours);

public sealed record LastTitan(int Kind, bool Fallen, long Mine);

public sealed record TitanReward(int Percent, int Hours);

public sealed record RankData(int Rank, string Name, long Monsters, bool Mine);
