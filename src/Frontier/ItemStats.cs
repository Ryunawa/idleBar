using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class ItemStats
{
    private static readonly Dictionary<string, (double Base, string[] Slots, HeroRole[] Roles)> Table = new()
    {
        [Items.Damage] = (4, [Items.Weapon, Items.Helm, Items.Arms, Items.Ring], [HeroRole.Warrior, HeroRole.Mage]),
        [Items.Health] = (6, [Items.Armor, Items.Helm, Items.Legs, Items.Feet, Items.Ring], [HeroRole.Tank, HeroRole.Support]),
        [Items.Critical] = (2, [Items.Weapon, Items.Helm, Items.Arms, Items.Ring], [HeroRole.Archer, HeroRole.Mage]),
        [Items.Healing] = (5, [Items.Armor, Items.Helm, Items.Legs, Items.Ring], [HeroRole.Support]),
        [Items.Speed] = (3, [Items.Weapon, Items.Helm, Items.Arms, Items.Feet, Items.Ring], [HeroRole.Warrior, HeroRole.Archer]),
        [Items.Gold] = (6, [Items.Armor, Items.Helm, Items.Ring], [HeroRole.Warrior]),
        [Items.Xp] = (10, [Items.Armor, Items.Helm, Items.Legs, Items.Ring], [HeroRole.Tank, HeroRole.Support]),
        [Items.Recharge] = (4, [Items.Weapon, Items.Helm, Items.Feet, Items.Ring], [HeroRole.Tank, HeroRole.Archer, HeroRole.Mage]),
        [Items.Boss] = (6, [Items.Weapon, Items.Helm, Items.Ring], [HeroRole.Warrior, HeroRole.Mage]),
    };

    public static double Base(string stat) => Table.TryGetValue(stat, out (double Base, string[] Slots, HeroRole[] Roles) entry) ? entry.Base : 0;

    public static IReadOnlyList<string> SlotsOf(string stat) => Table.TryGetValue(stat, out (double Base, string[] Slots, HeroRole[] Roles) entry) ? entry.Slots : [];

    public static IReadOnlyList<HeroRole> RolesOf(string stat) => Table.TryGetValue(stat, out (double Base, string[] Slots, HeroRole[] Roles) entry) ? entry.Roles : [];

    public static string Name(string stat) => stat switch
    {
        Items.Damage => "dégâts",
        Items.Health => "PV",
        Items.Critical => "coups critiques",
        Items.Healing => "soins",
        Items.Speed => "vitesse",
        Items.Gold => "or des monstres",
        Items.Xp => "xp",
        Items.Recharge => "recharge de l'actif",
        Items.Boss => "dégâts aux boss",
        _ => "effet inconnu",
    };

    public static string Line(string slot) => Items.Either([.. Pool(slot, null).Select(Name)]);

    public static IReadOnlyList<string> Pool(string slot, HeroRole? role) =>
        [.. Items.Stats.Where(stat => slot == Items.Weapon
            ? stat == Items.Damage || role is HeroRole owner && Table[stat].Roles.Contains(owner)
            : Table[stat].Slots.Contains(slot))];
}
