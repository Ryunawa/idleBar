using System.Collections.Generic;
using System.Linq;
using IdleBar.Online;

namespace IdleBar.Ui;

public static class SessionBanners
{
    private const float Short = 5;
    private const float Long = 9;

    public static IEnumerable<Announcement> Describe(WorldData? world, TavernData? previous, TavernData current)
    {
        if (current.TipJar.Amount > (previous?.TipJar.Amount ?? 0))
        {
            yield return new Announcement($"Pendant ton absence, ton aide a rempli le pot : {current.TipJar.Amount} écus", Short);
        }

        if (previous is null || world is null)
        {
            yield break;
        }

        foreach (GoalData goal in current.Goals.Where(goal => goal.Done && previous.Goals.Any(before => before.Id == goal.Id && !before.Done)))
        {
            yield return new Announcement($"Objectif rempli : {goal.Label} (+{goal.Reward} écus)", Short);
        }

        if (current.Tier > previous.Tier && world.Tiers.FirstOrDefault(tier => tier.Tier == current.Tier) is TierInfo tier)
        {
            yield return new Announcement($"Ta taverne devient « {tier.Name} » : de nouvelles améliorations t'attendent", Short);
        }

        foreach (string id in current.Upgrades.Except(previous.Upgrades))
        {
            if (world.Upgrades.FirstOrDefault(upgrade => upgrade.Id == id) is UpgradeInfo upgrade)
            {
                yield return new Announcement($"{upgrade.Name} : c'est installé !", Short);
            }
        }

        foreach (Announcement announcement in current.Regulars.SelectMany(progress => Regular(world, previous, progress)))
        {
            yield return announcement;
        }
    }

    private static IEnumerable<Announcement> Regular(WorldData world, TavernData previous, RegularProgress progress)
    {
        if (world.Regulars.FirstOrDefault(regular => regular.Id == progress.Id) is not RegularInfo regular)
        {
            yield break;
        }

        RegularProgress? before = previous.Regulars.FirstOrDefault(each => each.Id == progress.Id);
        if (before is null)
        {
            yield return new Announcement($"Nouvel habitué : {regular.Name}, {regular.Title}", Short);
        }

        if (progress.Chapter > (before?.Chapter ?? 0) && regular.Chapters.FirstOrDefault(chapter => chapter.Chapter == progress.Chapter) is ChapterInfo chapter)
        {
            yield return new Announcement($"{regular.Name} : « {chapter.Line} »", Long);
            if (progress.Chapter == regular.Chapters.Count)
            {
                yield return new Announcement($"{regular.Name} t'offre {regular.Souvenir} (+250 écus)", Short);
            }
        }
    }
}
