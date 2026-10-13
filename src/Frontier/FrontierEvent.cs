namespace IdleBar.Frontier;

public abstract record FrontierEvent;

public sealed record Hit(HitSource Source, double Amount, bool Critical = false, long Ally = 0) : FrontierEvent;

public sealed record Killed(double Gold, bool Boss) : FrontierEvent;

public sealed record BossFailed : FrontierEvent;

public sealed record BossCalled : FrontierEvent;

public sealed record SkillUnlocked(SkillKind Kind) : FrontierEvent;

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

public sealed record Struck(FighterKind Target, double Damage, bool Boss, long Ally = 0) : FrontierEvent;

public sealed record Fell(FighterKind Kind, double Seconds, long Ally = 0) : FrontierEvent;

public sealed record Revived(FighterKind Kind, long Ally = 0) : FrontierEvent;

public sealed record Routed(int Stage, bool Boss) : FrontierEvent;

public sealed record Healed : FrontierEvent;

public sealed record Armored(int Level) : FrontierEvent;

public sealed record AllyArrived(long Id, string Name, HeroRole Role) : FrontierEvent;

public sealed record AllyLeft(long Id, string Name) : FrontierEvent;

public sealed record Boosted(string Source, int Gold, int Damage) : FrontierEvent;
