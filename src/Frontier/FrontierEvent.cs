namespace IdleBar.Frontier;

public abstract record FrontierEvent;

public sealed record Hit(HitSource Source, double Amount) : FrontierEvent;

public sealed record Killed(double Gold, bool Boss) : FrontierEvent;

public sealed record BossFailed : FrontierEvent;

public sealed record BossCalled : FrontierEvent;

public sealed record StageReached(int Stage, bool Boss) : FrontierEvent;

public sealed record PlotConquered(int Plot) : FrontierEvent;

public sealed record Built(int Plot, BuildingKind Kind, int Level) : FrontierEvent;

public sealed record TierReached(int Plot, BuildingKind Kind, int Tier) : FrontierEvent;

public sealed record Trained(FighterKind Kind, int Level) : FrontierEvent;

public sealed record Supplied(int Stacks) : FrontierEvent;

public sealed record ChickenAppeared : FrontierEvent;

public sealed record FrenzyStarted : FrontierEvent;

public sealed record GoldRained(double Gold) : FrontierEvent;

public sealed record Smote : FrontierEvent;

public sealed record FuryStarted : FrontierEvent;

public sealed record Retreated(int Souls, double Multiplier) : FrontierEvent;

public sealed record AchievementUnlocked(Achievement Achievement) : FrontierEvent;

public sealed record Returned(double Seconds, double Counted, double Gold) : FrontierEvent;
