using System;

namespace IdleBar.Online;

public sealed record RoomGuest(long Visit, Guid Id, string Name, AvatarData Avatar, string Drink, bool Served, DateTimeOffset StartedAt);
