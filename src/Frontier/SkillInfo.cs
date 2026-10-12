namespace IdleBar.Frontier;

public sealed record SkillInfo(SkillKind Kind, string Name, double Cooldown, double Duration, int Unlock, string Effect);
