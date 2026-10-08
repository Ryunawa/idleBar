using System;
using System.Collections.Generic;

namespace IdleBar.Online;

public sealed record RoomData(
    Guid HostId,
    string Host,
    bool Mine,
    long? Visit,
    bool HostHome,
    AvatarData HostAvatar,
    int Stools,
    IReadOnlyList<string> Menu,
    int Helper,
    IReadOnlyList<string> Upgrades,
    IReadOnlyList<string> Souvenirs,
    ShownSpecialty Specialty,
    IReadOnlyList<RoomGuest> Guests,
    IReadOnlyList<ChatLine> Chat);
