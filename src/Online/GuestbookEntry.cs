using System;

namespace IdleBar.Online;

public sealed record GuestbookEntry(string Name, string Stamp, DateTimeOffset At, int Visits);
