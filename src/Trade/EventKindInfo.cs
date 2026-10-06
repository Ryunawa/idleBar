using System.Collections.Generic;

namespace IdleBar.Trade;

public sealed record EventKindInfo(string Id, EventScope Scope, string Name, string Description, IReadOnlyList<EventChoiceInfo> Choices);
