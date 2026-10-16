namespace IdleBar.Frontier;

public sealed record ResearchInfo(ResearchKind Kind, string Id, string Name, int MaxLevel, int Cost, double Hours, double Effect)
{
    public ResearchBranch Branch => Researches.BranchOf(Kind);
}
