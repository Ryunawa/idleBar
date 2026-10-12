using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Fighters
{
    public static IReadOnlyList<FighterInfo> All { get; } =
    [
        new(FighterKind.Hero, "Héros", 11, 200, 0.6, "Corps à corps. Tes clics frappent avec lui."),
        new(FighterKind.Archer, "Archère", 20, 750, 0.9, "Tire depuis l'arrière-garde."),
        new(FighterKind.Mage, "Mage", 74, 6_000, 1.6, "Sorts lents, mais qui piquent."),
    ];

    public static FighterInfo Of(FighterKind kind) => All[(int)kind];
}
