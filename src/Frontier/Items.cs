using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IdleBar.Frontier;

public static class Items
{
    public const string Weapon = "weapon";
    public const string Armor = "armor";
    public const string Helm = "helm";
    public const string Arms = "arms";
    public const string Legs = "legs";
    public const string Feet = "feet";
    public const string Ring = "ring";
    public const string Damage = "damage";
    public const string Health = "health";
    public const string Critical = "critical";
    public const string Healing = "healing";
    public const string Speed = "speed";
    public const string Gold = "gold";
    public const string Xp = "xp";
    public const string Recharge = "recharge";
    public const string Boss = "boss";
    public const string ShopSource = "shop";
    public const int Looks = 8;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly string[] RarityIds = ["common", "rare", "epic", "legendary"];
    private static readonly string[][] RarityLabels =
    [
        ["commune", "rare", "épique", "légendaire"],
        ["commun", "rare", "épique", "légendaire"],
        ["communes", "rares", "épiques", "légendaires"],
        ["communs", "rares", "épiques", "légendaires"],
    ];
    private static readonly double[] SalvageFactors = [2, 5, 12, 30];
    private static readonly (string Noun, bool Masculine)[][] ClassWeapons =
    [
        [("Épée", false), ("Hache", false), ("Masse", false), ("Lance", false), ("Marteau", true), ("Fléau", true), ("Cimeterre", true), ("Espadon", true)],
        [("Bouclier", true), ("Targe", false), ("Pavois", true), ("Rondache", false), ("Écu", true), ("Égide", false), ("Bastion", true), ("Pelte", false)],
        [("Arc", true), ("Arbalète", false), ("Arc long", true), ("Fronde", false), ("Sarbacane", false), ("Arc recourbé", true), ("Carquois", true), ("Arquebuse", false)],
        [("Bâton", true), ("Baguette", false), ("Grimoire", true), ("Tome", true), ("Orbe", true), ("Parchemin", true), ("Cristal", true), ("Sablier", true)],
        [("Sceptre", true), ("Crosse", false), ("Encensoir", true), ("Relique", false), ("Calice", true), ("Lanterne", false), ("Clochette", false), ("Ostensoir", true)],
    ];
    private static readonly (string Noun, bool Masculine)[] ArmorNouns =
        [("Cuirasse", false), ("Cotte", false), ("Brigandine", false), ("Armure", false), ("Gambison", true), ("Robe", false), ("Manteau", true), ("Harnois", true)];
    private static readonly (string Noun, bool Masculine)[] HelmNouns =
        [("Barbute", false), ("Couronne", false), ("Capuche", false), ("Toque", false), ("Heaume", true), ("Casque", true), ("Chapeau", true), ("Morion", true)];
    private static readonly (string Noun, bool Masculine)[] ArmsNouns =
        [("Gantelets", true), ("Brassards", true), ("Gants", true), ("Mitaines", false), ("Épaulières", false), ("Griffes", false), ("Moufles", false), ("Manchettes", false)];
    private static readonly (string Noun, bool Masculine)[] LegsNouns =
        [("Jambières", false), ("Grèves", false), ("Cuissardes", false), ("Chausses", false), ("Braies", false), ("Tassettes", false), ("Houseaux", true), ("Hauts-de-chausses", true)];
    private static readonly (string Noun, bool Masculine)[] FeetNouns =
        [("Bottes", false), ("Solerets", true), ("Sandales", false), ("Bottines", false), ("Sabots", true), ("Poulaines", false), ("Brodequins", true), ("Chaussons", true)];
    private static readonly (string Noun, bool Masculine)[] RingNouns =
        [("Anneau", true), ("Bague", false), ("Chevalière", false), ("Alliance", false), ("Jonc", true), ("Solitaire", true), ("Torsade", false), ("Cachet", true)];

    public static IReadOnlyList<string> Slots { get; } = [Weapon, Helm, Armor, Arms, Legs, Feet, Ring];

    public static IReadOnlyList<string> Stats { get; } = [Damage, Health, Critical, Healing, Speed, Gold, Xp, Recharge, Boss];

    public static int Rank(string? rarity) => Math.Max(0, Array.IndexOf(RarityIds, rarity));

    public static string RarityId(int rank) => RarityIds[Math.Clamp(rank, 0, RarityIds.Length - 1)];

    public static string RarityName(int rank) => RarityLabels[0][Math.Clamp(rank, 0, RarityIds.Length - 1)];

    public static string Percent(double value) => $"{Number(value)} %";

    public static string Number(double value) => value.ToString("0.#", French);

    public static string Required(int level) => $"Niveau {level} requis";

    public static int Look(int look) => (look % Looks + Looks) % Looks;

    public static string Name(string slot, int look, int rarity, HeroRole? role = null)
    {
        (string noun, bool masculine) = Nouns(slot, role)[Look(look)];
        string[] labels = RarityLabels[(masculine ? 1 : 0) + (Paired(slot) ? 2 : 0)];
        return $"{noun} {labels[Math.Clamp(rarity, 0, labels.Length - 1)]}";
    }

    public static string ClassWeapon(HeroRole role) => ClassWeapons[(int)role][0].Noun;

    public static string SlotName(string slot) => slot switch
    {
        Weapon => "Arme",
        Armor => "Armure",
        Arms => "Bras",
        Legs => "Jambes",
        Feet => "Pieds",
        Ring => "Anneau",
        Helm => "Coiffe",
        _ => "Objet",
    };

    public static string LooksLine(string slot) => Either([.. Nouns(slot, null).Select(entry => entry.Noun.ToLower(French))]);

    public static string Either(IReadOnlyList<string> words) =>
        words.Count < 2 ? string.Concat(words) : $"{string.Join(", ", words.Take(words.Count - 1))} ou {words[^1]}";

    public static string Stat(string stat, double value)
    {
        string amount = Number(value);
        return stat switch
        {
            Damage => $"+{amount} % de dégâts",
            Health => $"+{amount} % de PV",
            Critical => $"+{amount} % de coups critiques",
            Healing => $"+{amount} % de soins",
            Speed => $"+{amount} % de vitesse",
            Gold => $"+{amount} % d'or des monstres",
            Xp => $"+{amount} % d'xp",
            Recharge => $"+{amount} % de recharge de l'actif",
            Boss => $"+{amount} % de dégâts aux boss",
            _ => $"+{amount} % d'un effet inconnu",
        };
    }

    public static double Upgraded(double value, int upgrades) => value * (1 + Balance.UpgradeBonus * upgrades);

    public static double SalvageGold(FrontierState state, int rarity, bool bought, int level) =>
        bought ? GearPrices.Refund(state, rarity, level) : Economy.BaseKillGold(state, true) * SalvageFactors[Math.Clamp(rarity, 0, SalvageFactors.Length - 1)];

    private static bool Paired(string slot) => slot is Arms or Legs or Feet;

    private static (string Noun, bool Masculine)[] Nouns(string slot, HeroRole? role) => slot switch
    {
        Weapon => ClassWeapons[Math.Clamp((int)(role ?? HeroRole.Warrior), 0, ClassWeapons.Length - 1)],
        Armor => ArmorNouns,
        Arms => ArmsNouns,
        Legs => LegsNouns,
        Feet => FeetNouns,
        Ring => RingNouns,
        _ => HelmNouns,
    };
}
