using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed record ResearchView(IReadOnlyList<int> Levels)
{
    public int Level(ResearchKind kind) => (int)kind >= 0 && (int)kind < Levels.Count ? Levels[(int)kind] : 0;
}
