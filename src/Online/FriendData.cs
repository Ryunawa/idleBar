using System;

namespace IdleBar.Online;

public sealed record FriendData(Guid Id, string Name, AvatarData Avatar, bool Online, string Specialty);
