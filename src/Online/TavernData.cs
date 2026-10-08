using System;
using System.Collections.Generic;

namespace IdleBar.Online;

public sealed record TavernData(
    string Name,
    string FriendCode,
    long Coins,
    long Renown,
    int Tier,
    long Served,
    long Perfect,
    int Stools,
    IReadOnlyList<string> Menu,
    int Helper,
    IReadOnlyList<string> Upgrades,
    TipJarData TipJar,
    IReadOnlyList<GoalData> Goals,
    IReadOnlyList<RegularProgress> Regulars,
    AvatarData Avatar,
    SpecialtyData Specialty,
    IReadOnlyList<FriendData> Friends,
    IReadOnlyList<RequestData> Requests,
    IReadOnlyList<TastedData> Tasted,
    IReadOnlyList<GuestData> Guests,
    OutingData? Outing,
    IReadOnlyList<GuestbookEntry> Guestbook,
    IReadOnlyList<PasserbyData> Passersby,
    IReadOnlyList<InvitationData> Invitations,
    IReadOnlyList<RoundData> Rounds,
    int RoundPrice,
    DateTimeOffset? RoundReadyAt);
