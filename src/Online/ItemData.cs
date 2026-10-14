using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record ItemsData(int Max, IReadOnlyList<ItemData> Bag)
{
    public Gear ToGear() => Gear.Of(Bag.Where(item => item.Equipped).SelectMany(item => item.Stats));

    public GearLook ToWorn()
    {
        ItemData? weapon = Equipped(Items.Weapon);
        ItemData? armor = Equipped(Items.Armor);
        ItemData? helm = Equipped(Items.Helm);
        return new GearLook(weapon?.Rank, armor?.Rank, helm?.Look, helm?.Rank ?? 0);
    }

    public ItemData? Equipped(string slot) => Bag.FirstOrDefault(item => item.Equipped && item.Slot == slot);
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
    bool Equipped)
{
    public int Rank => Items.Rank(Rarity);

    public string Name => Items.Name(Slot, Look, Rank);

    public IEnumerable<(string Stat, double Percent)> Stats =>
        StatB is string second && ValueB is double value ? [(StatA, ValueA), (second, value)] : [(StatA, ValueA)];

    public string Describe() => string.Join(" · ", Stats.Select(stat => Items.Stat(stat.Stat, stat.Percent)));
}
