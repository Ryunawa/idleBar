namespace IdleBar.Frontier;

public sealed record FighterInfo(FighterKind Kind, string Id, string Name, double Dps, double Hp, double Cost, double Revive, HeroRole Class, int Unlock, string Effect);
