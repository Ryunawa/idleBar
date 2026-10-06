using System;

namespace IdleBar.Trade;

public sealed record JournalEntry(long Id, string KindId, DateTimeOffset HappenedAt, string Title, string Detail, EventTone Tone, bool Seen);
