using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record ShopData(double RefreshIn, IReadOnlyList<OfferData> Offers, IReadOnlyList<OddsData>? Odds = null)
{
    public string? OddsLine => Odds is { Count: > 0 } odds ? ItemRolls.OddsLine(odds.Select(entry => (entry.Rarity, entry.Percent))) : null;
}

public sealed record OddsData(string Rarity, double Percent);

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
