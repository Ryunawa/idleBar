using System;

namespace IdleBar.Online;

public sealed record GuestData(long Visit, string Name, AvatarData Avatar, string Stamp, string Drink, DateTimeOffset StartedAt);
