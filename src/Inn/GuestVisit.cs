using System;

namespace IdleBar.Inn;

public sealed record GuestVisit(long Visit, string Name, Drink Order, bool Served = false, bool Dozing = false, DateTimeOffset? OrderedAt = null);
