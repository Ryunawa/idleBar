using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Titans
{
    private static readonly double[] Thresholds = [0.75, 0.5, 0.25];

    public static IReadOnlyList<string> Names { get; } = ["Colosse de pierre", "Hydre des marais", "Wyrm de givre", "Roi des sables"];

    public static string Name(int kind) => Names[((kind % Names.Count) + Names.Count) % Names.Count];

    public static void Sight(FrontierState state, TitanView view, EventLog events)
    {
        TitanView? known = state.Titan;
        state.Titan = view;
        if (known is null || known.Week != view.Week)
        {
            events.Add(new TitanAppeared(view.Kind, view.Left, view.Fallen));
            return;
        }

        if (view.Mine > known.Mine)
        {
            events.Add(new TitanStruck(view.Mine - known.Mine));
        }

        if (view.Fallen && !known.Fallen)
        {
            events.Add(new TitanFell(view.Kind));
            return;
        }

        foreach (double threshold in Thresholds)
        {
            if (known.Left > threshold && view.Left <= threshold)
            {
                events.Add(new TitanWounded(view.Kind, view.Left));
                break;
            }
        }
    }
}
