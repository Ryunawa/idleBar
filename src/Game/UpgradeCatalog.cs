using System.Collections.Generic;

namespace IdleBar.Game;

public static class UpgradeCatalog
{
    public const string MinerId = "miner";
    public const string PickaxeId = "pickaxe";
    public const string DrillId = "drill";

    public static IReadOnlyList<UpgradeDefinition> All { get; } =
    [
        new(MinerId, "Mineur", "+0,5 or/s", 15, 1.15, 0.5, 0),
        new(PickaxeId, "Pioche", "+1 or par clic", 30, 1.6, 0, 1),
        new(DrillId, "Foreuse", "+6 or/s", 300, 1.18, 6, 0),
    ];
}
