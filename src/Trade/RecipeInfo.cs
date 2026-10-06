using System.Collections.Generic;

namespace IdleBar.Trade;

public sealed record RecipeInfo(
    string Id,
    string CraftId,
    string OutputGoodId,
    int OutputQuantity,
    int Seconds,
    IReadOnlyList<StockLine> Inputs);
