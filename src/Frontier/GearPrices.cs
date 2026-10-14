using System;

namespace IdleBar.Frontier;

public static class GearPrices
{
    public static double Offer(FrontierState state, int level, int rarity) => Base(state, level, rarity);

    public static double Upgrade(FrontierState state, int rarity, int upgrades, int count)
    {
        double growth = Balance.UpgradeGrowth;
        return Base(state, state.BestStage, rarity) * Balance.UpgradeShare * Math.Pow(growth, upgrades) * (Math.Pow(growth, count) - 1) / (growth - 1);
    }

    public static double Refund(FrontierState state, int rarity, int level) => Base(state, level, rarity) * Balance.RefundShare;

    public static int MaxUpgrades(FrontierState state, int rarity, int upgrades)
    {
        int count = 0;
        while (upgrades + count < Balance.MaxUpgrades && Upgrade(state, rarity, upgrades, count + 1) <= state.Gold)
        {
            count++;
        }

        return count;
    }

    private static double Base(FrontierState state, int level, int rarity) =>
        Balance.ShopKills * (Economy.MonsterHp(level) * Balance.KillGoldRatio + Balance.KillGoldFlat)
        * Balance.RarityPrices[Math.Clamp(rarity, 0, Balance.RarityPrices.Length - 1)] * Economy.Discount(state);
}
