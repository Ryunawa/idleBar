namespace IdleBar.Trade;

public sealed record TradeRules(
    int OfferHours,
    int TakeoverSeconds,
    int ContractSlackSeconds,
    double GameCarrierSlowness,
    double FreightBaseRate,
    double FreightHourlyRate,
    int DepotCapacity,
    int JourneymanXp,
    int MasterXp);
