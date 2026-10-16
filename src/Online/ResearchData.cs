using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record ResearchData(
    int Seals,
    int Members = 0,
    IReadOnlyList<ResearchNodeData>? Nodes = null,
    IReadOnlyList<QueuedResearch>? Queue = null)
{
    public ResearchView ToView() => new([.. Researches.All.Select(research => Node(research.Kind)?.Level ?? 0)]);

    public ResearchNodeData? Node(ResearchKind kind) => Nodes?.FirstOrDefault(node => Researches.Parse(node.Id) == kind);

    public QueuedResearch? Running => Queue?.FirstOrDefault(entry => entry.StartsIn <= 0);

    public IReadOnlyList<QueuedResearch> Waiting => [.. (Queue ?? []).Where(entry => entry.StartsIn > 0)];

    public int Position(ResearchKind kind)
    {
        IReadOnlyList<QueuedResearch> waiting = Waiting;
        for (int index = 0; index < waiting.Count; index++)
        {
            if (waiting[index].Kind == kind)
            {
                return index + 1;
            }
        }

        return 0;
    }
}

public sealed record ResearchNodeData(string Id, int Level, int Queued, int Funded, int? Cost, double? Hours, bool Frozen = false)
{
    public int Missing => Cost is int cost ? System.Math.Max(0, cost - Funded) : 0;
}

public sealed record QueuedResearch(string Node, int Level, double StartsIn, double EndsIn, double Hours)
{
    public ResearchKind? Kind => Researches.Parse(Node);
}
