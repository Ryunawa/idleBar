using System;

namespace IdleBar.Trade;

public sealed record TripEventInfo(string KindId, DateTimeOffset OccurredAt, DateTimeOffset DecideBy, string? DirectiveChoiceId);
