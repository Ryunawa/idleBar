namespace IdleBar.Frontier;

public sealed record RoleInfo(
    HeroRole Role, string Id, string Name, double Dps, double Hp, double Interval, bool Ranged, double Cooldown, string Effect, string Training);
