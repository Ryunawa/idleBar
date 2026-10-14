namespace IdleBar.Frontier;

public abstract record Purchase;

public sealed record Recruitment(FighterKind Kind, HeroRole Class) : Purchase;

public sealed record Construction(int Plot, BuildingKind Kind) : Purchase;

public sealed record PlotPurchase(int Plot) : Purchase;
