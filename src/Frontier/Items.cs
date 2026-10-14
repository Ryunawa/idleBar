using System;
using System.Collections.Generic;
using System.Globalization;

namespace IdleBar.Frontier;

public static class Items
{
    public const string Weapon = "weapon";
    public const string Armor = "armor";
    public const string Helm = "helm";
    public const string Damage = "damage";
    public const string Health = "health";
    public const string Critical = "critical";
    public const string Healing = "healing";
    public const string Speed = "speed";

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly string[] RarityIds = ["common", "rare", "epic", "legendary"];
    private static readonly string[] RarityLabels = ["commune", "rare", "épique", "légendaire"];
    private static readonly double[] SalvageFactors = [2, 5, 12, 30];
    private static readonly string[] WeaponNouns = ["Lame", "Hache", "Masse", "Lance"];
    private static readonly string[] ArmorNouns = ["Cuirasse", "Cotte", "Brigandine", "Armure"];
    private static readonly string[] HelmNouns = ["Barbute", "Couronne", "Capuche", "Toque"];

    public static IReadOnlyList<string> Slots { get; } = [Weapon, Armor, Helm];

    public static int Rank(string? rarity) => Math.Max(0, Array.IndexOf(RarityIds, rarity));

    public static string Name(string slot, int look, int rarity) =>
        $"{Nouns(slot)[look & 3]} {RarityLabels[Math.Clamp(rarity, 0, RarityLabels.Length - 1)]}";

    public static string SlotName(string slot) => slot switch
    {
        Weapon => "Arme",
        Armor => "Armure",
        _ => "Coiffe",
    };

    public static string Stat(string stat, double value)
    {
        string amount = value.ToString("0.#", French);
        return stat switch
        {
            Damage => $"+{amount} % de dégâts",
            Health => $"+{amount} % de PV",
            Critical => $"+{amount} % de coups critiques",
            Healing => $"+{amount} % de soins",
            _ => $"+{amount} % de vitesse",
        };
    }

    public static double SalvageGold(FrontierState state, int rarity) =>
        Economy.KillGold(state, true) * SalvageFactors[Math.Clamp(rarity, 0, SalvageFactors.Length - 1)];

    private static string[] Nouns(string slot) => slot switch
    {
        Weapon => WeaponNouns,
        Armor => ArmorNouns,
        _ => HelmNouns,
    };
}
