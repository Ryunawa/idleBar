using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Game;

public sealed class GameState
{
    private readonly Dictionary<string, int> _owned = new();

    public double Gold { get; private set; }

    public double TotalEarned { get; private set; }

    public IReadOnlyList<UpgradeDefinition> Upgrades => UpgradeCatalog.All;

    public double IncomePerSecond => Upgrades.Sum(upgrade => upgrade.IncomePerSecond * OwnedCount(upgrade));

    public double ClickValue => 1 + Upgrades.Sum(upgrade => upgrade.ClickBonus * OwnedCount(upgrade));

    public ProgressData Capture() => new(Gold, TotalEarned, new Dictionary<string, int>(_owned));

    public void Restore(ProgressData progress)
    {
        Gold = progress.Gold;
        TotalEarned = progress.TotalEarned;
        _owned.Clear();
        foreach (KeyValuePair<string, int> entry in progress.Owned)
        {
            _owned[entry.Key] = entry.Value;
        }
    }

    public int OwnedCount(UpgradeDefinition upgrade) => OwnedCount(upgrade.Id);

    public int OwnedCount(string upgradeId) => _owned.GetValueOrDefault(upgradeId);

    public double CostOf(UpgradeDefinition upgrade) =>
        Math.Ceiling(upgrade.BaseCost * Math.Pow(upgrade.CostGrowth, OwnedCount(upgrade)));

    public bool CanAfford(UpgradeDefinition upgrade) => Gold >= CostOf(upgrade);

    public bool TryBuy(UpgradeDefinition upgrade)
    {
        double cost = CostOf(upgrade);
        if (Gold < cost)
        {
            return false;
        }

        Gold -= cost;
        _owned[upgrade.Id] = OwnedCount(upgrade) + 1;
        return true;
    }

    public double Mine()
    {
        double amount = ClickValue;
        Earn(amount);
        return amount;
    }

    public double Advance(double seconds)
    {
        double amount = IncomePerSecond * seconds;
        Earn(amount);
        return amount;
    }

    private void Earn(double amount)
    {
        Gold += amount;
        TotalEarned += amount;
    }
}
