namespace IdleBar.Trade;

public sealed record ReputationTier(
    int Level,
    string Id,
    string Name,
    int Points,
    double PriceRate,
    int StorageBonus,
    double FreightDiscount,
    double OrderBonus,
    string Description);
