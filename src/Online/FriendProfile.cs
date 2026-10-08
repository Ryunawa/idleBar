using System;
using System.Collections.Generic;

namespace IdleBar.Online;

public sealed record FriendProfile(
    Guid Id,
    string Name,
    AvatarData Avatar,
    int Tier,
    long Served,
    bool Online,
    DateTimeOffset? FriendsSince,
    bool Muted,
    ShownSpecialty Specialty,
    int Souvenirs,
    string Stamp,
    IReadOnlyList<GuestbookEntry> Guestbook,
    IReadOnlyList<ShownSpecialty> Tasted);
