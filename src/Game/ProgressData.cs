using System.Collections.Generic;

namespace IdleBar.Game;

public sealed record ProgressData(double Gold, double TotalEarned, IReadOnlyDictionary<string, int> Owned);
