using System;

namespace IdleBar.Online;

public sealed record ChatLine(long Id, Guid Author, string Name, string Text, DateTimeOffset At, Guid? Room = null);
