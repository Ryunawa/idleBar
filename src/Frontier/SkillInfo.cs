namespace IdleBar.Frontier;

public sealed record SkillInfo(SkillKind Kind, string Id, string Name, int Cost, int MaxLevel, double Cooldown, double Duration, string Effect)
{
    public bool Active => Cooldown > 0;

    public SkillBranch Branch => Skills.BranchOf(Kind);
}
