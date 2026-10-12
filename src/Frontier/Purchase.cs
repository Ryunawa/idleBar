namespace IdleBar.Frontier;

public abstract record Purchase;

public sealed record FighterPurchase(FighterKind Kind) : Purchase;

public sealed record BuildingPurchase(BuildingKind Kind) : Purchase;
