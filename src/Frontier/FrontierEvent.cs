namespace IdleBar.Frontier;

public abstract record FrontierEvent;

public sealed record Hit(HitSource Source, double Amount, bool Critical = false, long Ally = 0, bool Big = false) : FrontierEvent;

public sealed record Killed(double Gold, bool Boss) : FrontierEvent;

public sealed record BossFailed : FrontierEvent;

public sealed record BossCalled : FrontierEvent;

public sealed record PointEarned(int Points) : FrontierEvent;

public sealed record SkillsChanged : FrontierEvent;

public sealed record SkillCast(SkillKind Kind, double Amount = 0) : FrontierEvent;

public sealed record StageReached(int Stage, bool Boss) : FrontierEvent;

public sealed record PlotConquered(int Plot) : FrontierEvent;

public sealed record Built(int Plot, BuildingKind Kind, int Level) : FrontierEvent;

public sealed record TierReached(int Plot, BuildingKind Kind, int Tier) : FrontierEvent;

public sealed record Trained(FighterKind Kind, int Level) : FrontierEvent;

public sealed record LeveledUp(FighterKind Kind, int Level) : FrontierEvent;

public sealed record CompanionReady(FighterKind Kind) : FrontierEvent;

public sealed record Supplied(int Stacks) : FrontierEvent;

public sealed record ChickenAppeared : FrontierEvent;

public sealed record FrenzyStarted : FrontierEvent;

public sealed record Retreated(int Souls, double Multiplier) : FrontierEvent;

public sealed record AchievementUnlocked(Achievement Achievement) : FrontierEvent;

public sealed record Returned(double Seconds, double Counted, double Gold) : FrontierEvent;

public sealed record Struck(FighterKind Target, double Damage, bool Boss, long Ally = 0) : FrontierEvent;

public sealed record Fell(FighterKind Kind, double Seconds, long Ally = 0, double Lost = 0) : FrontierEvent;

public sealed record Revived(FighterKind Kind, long Ally = 0) : FrontierEvent;

public sealed record Routed(int Stage, bool Boss) : FrontierEvent;

public sealed record ActiveUsed(FighterKind Kind, HeroRole Role, int Raised = 0) : FrontierEvent;

public sealed record Mended(FighterKind Kind, long Ally = 0) : FrontierEvent;

public sealed record AllyArrived(long Id, string Name, HeroRole Role) : FrontierEvent;

public sealed record AllyLeft(long Id, string Name) : FrontierEvent;

public sealed record Boosted(string Source, int Gold, int Damage) : FrontierEvent;

public sealed record TitanAppeared(int Kind, double Left, bool Fallen) : FrontierEvent;

public sealed record TitanWounded(int Kind, double Left) : FrontierEvent;

public sealed record TitanFell(int Kind, bool Rewarded) : FrontierEvent;

public sealed record TitanStruck(long Monsters) : FrontierEvent;

public sealed record LootFound(int Rarity, string Name) : FrontierEvent;

public sealed record Recycled(double Gold) : FrontierEvent;
