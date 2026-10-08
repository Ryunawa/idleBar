using System.Linq;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed record UpgradeOffer(string Label, bool Enabled)
{
    public static UpgradeOffer For(UpgradeInfo upgrade, WorldData world, TavernData tavern, long coins)
    {
        if (tavern.Upgrades.Contains(upgrade.Id))
        {
            return new UpgradeOffer("Acquis", false);
        }

        if (upgrade.Requires is string required && !tavern.Upgrades.Contains(required))
        {
            return new UpgradeOffer($"Après « {world.Upgrades.First(each => each.Id == required).Name} »", false);
        }

        if (tavern.Tier < upgrade.Tier)
        {
            return new UpgradeOffer($"{world.Tiers.First(tier => tier.Tier == upgrade.Tier).Name} requis", false);
        }

        return new UpgradeOffer(NumberFormat.Coins(upgrade.Price), coins >= upgrade.Price);
    }
}
