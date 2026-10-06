using System.Collections.Generic;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed record LaneScene(
    LaneMode Mode,
    Biome Biome,
    double Progress,
    string Caption,
    string CraftId,
    bool Busy,
    IReadOnlyList<string> Products)
{
    public static LaneScene Idle(string caption) => new(LaneMode.Idle, Biome.Plain, 0, caption, string.Empty, false, []);

    public static LaneScene Travelling(Biome biome, double progress) =>
        new(LaneMode.Travelling, biome, progress, string.Empty, string.Empty, true, []);

    public static LaneScene InTown(Biome biome, string townName) =>
        new(LaneMode.InTown, biome, 1, townName, string.Empty, false, []);

    public static LaneScene Workshop(Biome biome, string craftId, bool busy, double progress, IReadOnlyList<string> products, string caption) =>
        new(LaneMode.Workshop, biome, progress, caption, craftId, busy, products);
}
