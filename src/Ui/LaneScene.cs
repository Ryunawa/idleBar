using System.Collections.Generic;
using IdleBar.Pixel;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed record LaneScene(
    LaneMode Mode,
    Biome Biome,
    double Progress,
    string Caption,
    string CraftId,
    bool Busy,
    IReadOnlyList<string> Products,
    bool Raining = false,
    CaravanLook? Look = null,
    string? Hazard = null,
    bool Errands = false,
    StreetState? Street = null)
{
    public static LaneScene Idle(string caption) => new(LaneMode.Idle, Biome.Plain, 0, caption, string.Empty, false, []);

    public static LaneScene Travelling(Biome biome, double progress, bool raining, CaravanLook look) =>
        new(LaneMode.Travelling, biome, progress, string.Empty, string.Empty, true, [], raining, look);

    public static LaneScene Halted(Biome biome, double progress, bool raining, CaravanLook look, string hazard, string caption) =>
        new(LaneMode.Halted, biome, progress, caption, string.Empty, false, [], raining, look, hazard);

    public static LaneScene InTown(Biome biome, string townName, CaravanLook look, StreetState street) =>
        new(LaneMode.InTown, biome, 1, townName, string.Empty, false, [], false, look, Errands: true, Street: street);

    public static LaneScene Workshop(Biome biome, string craftId, bool busy, double progress, IReadOnlyList<string> products, string caption, StreetState street, bool errands = false) =>
        new(LaneMode.Workshop, biome, progress, caption, craftId, busy, products, Errands: errands, Street: street);
}
