namespace IdleBar.Frontier;

public abstract record Purchase;

public sealed record FighterPurchase(FighterKind Kind) : Purchase;

public sealed record Construction(int Plot, BuildingKind Kind) : Purchase;

public sealed record PlotPurchase(int Plot) : Purchase;

public sealed record ArmorPurchase : Purchase;
