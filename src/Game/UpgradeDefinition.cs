namespace IdleBar.Game;

public sealed record UpgradeDefinition(
    string Id,
    string Name,
    string Description,
    double BaseCost,
    double CostGrowth,
    double IncomePerSecond,
    double ClickBonus);
