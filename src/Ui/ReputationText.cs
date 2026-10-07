using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class ReputationText
{
    private const string Unknown = "Inconnu";

    public static string Standing(ReputationState standing, string townId)
    {
        string tier = standing.TierAt(townId)?.Name ?? Unknown;
        return standing.NextTierAt(townId) is ReputationTier next
            ? $"{tier} · {NumberFormat.Amount(standing.PointsAt(townId))}/{NumberFormat.Amount(next.Points)}"
            : tier;
    }

    public static void AddRows(VBoxContainer list, TownContext context)
    {
        ReputationState standing = context.Snapshot.Standing;
        if (standing.Tiers.Count == 0)
        {
            return;
        }

        list.AddChild(ActionRow.Heading("Réputation dans les villes"));
        list.AddChild(ActionRow.Note("Elle grandit quand tu achètes, vends, échanges ou livres dans une ville, et ne baisse jamais."));
        foreach (TownInfo town in context.World.Towns.OrderByDescending(town => standing.PointsAt(town.Id)).ThenBy(town => town.Name))
        {
            ReputationTier? tier = standing.TierAt(town.Id);
            string progress = standing.NextTierAt(town.Id) is ReputationTier next
                ? $"{NumberFormat.Amount(standing.PointsAt(town.Id))}/{NumberFormat.Amount(next.Points)} pour devenir {next.Name.ToLowerInvariant()}"
                : "palier le plus haut";
            list.AddChild(ActionRow.Create($"{town.Name} · {tier?.Name ?? Unknown}", $"{progress} · {tier?.Description}", string.Empty, true, () => { }));
        }
    }
}
