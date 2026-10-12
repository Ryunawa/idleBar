using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Plots
{
    public static IReadOnlyList<PlotInfo> All { get; } =
    [
        new(0, 0.8, 30, 1),
        new(1, 5, 400, 1),
        new(2, 25, 5_000, 11),
        new(3, 120, 60_000, 21),
        new(4, 600, 700_000, 31),
        new(5, 1_500, 1_500_000, 41),
        new(6, 8_000, 25_000_000, 51),
    ];

    public static PlotInfo Of(int plot) => All[plot];
}
