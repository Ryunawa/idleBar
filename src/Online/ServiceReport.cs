using System.Collections.Generic;

namespace IdleBar.Online;

public sealed record ServiceReport(IReadOnlyDictionary<string, int> Drinks, int Perfect, int Parting, long Coins);
