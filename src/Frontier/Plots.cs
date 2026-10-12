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
        new(7, 40_000, 400_000_000, 61),
        new(8, 200_000, 6_000_000_000, 71),
        new(9, 1_000_000, 100_000_000_000, 81),
        new(10, 5_000_000, 1_500_000_000_000, 91),
        new(11, 25_000_000, 25_000_000_000_000, 101),
    ];

    public static PlotInfo Of(int plot) => All[plot];
}
