using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class GearSummary
{
    public static double? Cap(string stat) => stat switch
    {
        Items.Recharge => Balance.RechargeCap,
        Items.Xp => Balance.GearXpCap,
        Items.Gold => Balance.PlunderCap,
        _ => null,
    };

    public static double Of(Gear gear, string stat) => stat switch
    {
        Items.Damage => gear.Damage,
        Items.Health => gear.Health,
        Items.Critical => gear.Critical,
        Items.Healing => gear.Healing,
        Items.Speed => gear.Speed,
        Items.Gold => gear.Gold,
        Items.Xp => gear.Xp,
        Items.Recharge => gear.Recharge,
        Items.Boss => gear.Boss,
        _ => 0,
    };

    public static string Describe(Gear gear, double teamGold)
    {
        List<string> parts = [];
        foreach (string stat in Items.Stats)
        {
            double value = stat == Items.Gold ? teamGold : Of(gear, stat);
            if (value <= 0)
            {
                continue;
            }

            string part = Items.Stat(stat, value * 100) + (stat == Items.Gold ? " pour l'équipe" : string.Empty);
            parts.Add(Cap(stat) is double cap && value >= cap ? $"{part} (plafond : {Items.Percent(cap * 100)})" : part);
        }

        return parts.Count == 0 ? "Aucun bonus porté pour l'instant." : $"Bonus portés : {string.Join(" · ", parts)}";
    }
}
