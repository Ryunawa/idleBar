using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record ShopData(double RefreshIn, IReadOnlyList<OfferData> Offers);

public sealed record OfferData(
    long Id,
    string Slot,
    string Rarity,
    int Level,
    string StatA,
    double ValueA,
    string? StatB,
    double? ValueB,
    int Look,
    string? Role,
    double Price,
    bool Sold)
{
    public ItemData ToItem() => new(Id, Slot, Rarity, Level, StatA, ValueA, StatB, ValueB, Look, Items.ShopSource, false, Role, null, true);
}
