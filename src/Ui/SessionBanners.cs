using System.Collections.Generic;
using System.Linq;
using IdleBar.Online;

namespace IdleBar.Ui;

public static class SessionBanners
{
    public static IEnumerable<string> Describe(WorldData? world, TavernData? previous, TavernData current)
    {
        if (current.TipJar.Amount > (previous?.TipJar.Amount ?? 0))
        {
            yield return $"Pendant ton absence, ton aide a rempli le pot : {current.TipJar.Amount} écus";
        }

        if (previous is null)
        {
            yield break;
        }

        foreach (GoalData goal in current.Goals.Where(goal => goal.Done && previous.Goals.Any(before => before.Id == goal.Id && !before.Done)))
        {
            yield return $"Objectif rempli : {goal.Label} (+{goal.Reward} écus)";
        }

        if (current.Tier > previous.Tier && world?.Tiers.FirstOrDefault(tier => tier.Tier == current.Tier) is TierInfo tier)
        {
            yield return $"Ta taverne devient « {tier.Name} » : de nouvelles améliorations t'attendent";
        }

        foreach (string id in current.Upgrades.Except(previous.Upgrades))
        {
            if (world?.Upgrades.FirstOrDefault(upgrade => upgrade.Id == id) is UpgradeInfo upgrade)
            {
                yield return $"{upgrade.Name} : c'est installé !";
            }
        }
    }
}
