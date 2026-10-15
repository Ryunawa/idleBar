using System.Collections.Concurrent;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class ItemSprites
{
    public static readonly (Rgba Main, Rgba Shade)[] Rarities =
    [
        (Palette.LightGrey, Palette.DarkGrey), (Palette.PicoBlue, Palette.DarkBlue), (Palette.PicoPink, Palette.DarkPurple), (Palette.PicoYellow, Palette.PicoOrange),
    ];

    private static readonly ConcurrentDictionary<(string Slot, int Look, int Rarity, HeroRole Role), ArtSprite> Icons = new();
    private static readonly ConcurrentDictionary<(int Look, int Rarity), ArtSprite> Helms = new();

    public static ArtSprite Icon(string slot, int look, int rarity, HeroRole? role = null) =>
        Icons.GetOrAdd((slot, Items.Look(look), Rank(rarity), slot == Items.Weapon ? role ?? HeroRole.Warrior : HeroRole.Warrior),
            key => ArtSprite.Parse(Grids(key.Slot, key.Role)[key.Look], Colors(key.Rarity)));

    public static ArtSprite Helm(int look, int rarity) =>
        Helms.GetOrAdd((Items.Look(look), Rank(rarity)), key => ArtSprite.Parse(ItemGrids.Worn[key.Look], Colors(key.Rarity)));

    public static Dictionary<char, Rgba> Colors(int rarity) => new()
    {
        ['J'] = Rarities[Rank(rarity)].Main,
        ['Z'] = Rarities[Rank(rarity)].Shade,
    };

    private static int Rank(int rarity) => System.Math.Clamp(rarity, 0, Rarities.Length - 1);

    private static IReadOnlyList<IReadOnlyList<string>> Grids(string slot, HeroRole role) => slot switch
    {
        Items.Weapon => WeaponGrids.Of(role),
        Items.Armor => ItemGrids.Armors,
        Items.Arms => GearGrids.Arms,
        Items.Legs => GearGrids.Legs,
        Items.Feet => GearGrids.Feet,
        Items.Ring => GearGrids.Rings,
        _ => ItemGrids.Helms,
    };
}
