using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed record ResearchSnapshot(ResearchData? Research, bool Member, double Elapsed, bool Leader = false)
{
    public int Seals => Research?.Seals ?? 0;

    public int Members => Research?.Members ?? 0;

    public double Left(double seconds) => System.Math.Max(0, seconds - Elapsed);

    public ResearchNodeData? Node(ResearchKind kind) => Member ? Research?.Node(kind) : null;

    public int Level(ResearchKind kind) => Node(kind)?.Level ?? 0;

    public QueuedResearch? Running => Member ? Research?.Running : null;

    public static string Seal(int count) => count > 1 ? $"{count} sceaux" : $"{count} sceau";

    public static string Title(QueuedResearch entry) => entry.Kind is ResearchKind kind ? Researches.Of(kind).Name : entry.Node;
}
