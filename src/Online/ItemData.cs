using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record ItemsData(int Max, IReadOnlyList<ItemData> Bag)
{
    public Gear ToGear(FighterKind kind) => Gear.Of(Bag.Where(item => item.Wearer == kind).SelectMany(item => item.Stats));

    public GearLook ToWorn(FighterKind kind)
    {
        ItemData? weapon = Equipped(kind, Items.Weapon);
        ItemData? armor = Equipped(kind, Items.Armor);
        ItemData? helm = Equipped(kind, Items.Helm);
        return new GearLook(weapon?.Rank, armor?.Rank, helm?.Look, helm?.Rank ?? 0);
    }

    public ItemData? Equipped(FighterKind kind, string slot) => Bag.FirstOrDefault(item => item.Wearer == kind && item.Slot == slot);
}

public sealed record ItemData(
    long Id,
    string Slot,
    string Rarity,
    int Level,
    string StatA,
    double ValueA,
    string? StatB,
    double? ValueB,
    int Look,
    string Source,
    bool Equipped,
    string? Role = null,
    string? Fighter = null,
    bool Bound = false,
    int Upgrades = 0)
{
    public int Rank => Items.Rank(Rarity);

    public string Name => Upgrades > 0 ? $"{Items.Name(Slot, Look, Rank, Class)} +{Upgrades}" : Items.Name(Slot, Look, Rank, Class);

    public HeroRole? Class => Roles.Parse(Role);

    public FighterKind? Wearer => Equipped ? Fighters.Parse(Fighter ?? Fighters.Of(FighterKind.Hero).Id) : null;

    public bool Fits(HeroRole role) => Class is not HeroRole owner || owner == role;

    public IEnumerable<(string Stat, double Percent)> Stats =>
        StatB is string second && ValueB is double value
            ? [(StatA, Items.Upgraded(ValueA, Upgrades)), (second, Items.Upgraded(value, Upgrades))]
            : [(StatA, Items.Upgraded(ValueA, Upgrades))];

    public string Describe() => string.Join(" · ", Stats.Select(stat => Items.Stat(stat.Stat, stat.Percent)));
}
