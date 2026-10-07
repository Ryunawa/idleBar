namespace IdleBar.Pixel;

public sealed record StreetState(
    string CraftId,
    int WorkshopLevel,
    int StorageLoad,
    int StorageCapacity,
    int Branches,
    int OpenOffers,
    int Contracts,
    int News);