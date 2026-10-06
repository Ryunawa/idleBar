using System.Collections.Generic;

namespace IdleBar.Trade;

public sealed record DirectiveInfo(string Id, string Name, string Description, IReadOnlyList<DirectiveReaction> Choices);
