using System.Collections.Generic;

namespace IdleBar.Trade;

public sealed record FittingInfo(string Id, string Name, string Description, IReadOnlyList<StockLine> Costs);
