using System;

namespace IdleBar.Frontier;

public sealed record Achievement(string Id, string Name, string Description, Func<FrontierState, bool> Reached);
