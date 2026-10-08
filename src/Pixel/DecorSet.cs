using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Pixel;

public sealed class DecorSet
{
    private static readonly DecorKind[] Base =
    [
        DecorKind.Window, DecorKind.Shelf, DecorKind.Lantern, DecorKind.Barrels, DecorKind.Candle, DecorKind.Bowl, DecorKind.Mug,
    ];

    private readonly HashSet<DecorKind> _kinds;

    private DecorSet(IEnumerable<DecorKind> kinds)
    {
        _kinds = [.. Base, .. kinds];
        Key = string.Join(',', _kinds.OrderBy(kind => kind));
    }

    public static DecorSet Bare { get; } = new([]);

    public string Key { get; }

    public static DecorSet From(IEnumerable<string> unlocks) => new(unlocks.SelectMany(Kinds));

    public bool Has(DecorKind kind) => _kinds.Contains(kind);

    public static IReadOnlyList<DecorKind> Kinds(string unlock) => unlock switch
    {
        "plants" => [DecorKind.Plant],
        "hangings" => [DecorKind.Garlic, DecorKind.Herbs, DecorKind.Pot],
        "boards" => [DecorKind.Board],
        "paintings" => [DecorKind.Painting, DecorKind.Portrait],
        "darts" => [DecorKind.Dartboard],
        "banners" => [DecorKind.Banner, DecorKind.BlueBanner],
        "cat" => [DecorKind.SleepingCat],
        "clocks" => [DecorKind.Clock],
        "trophies" => [DecorKind.Trophy, DecorKind.Shield],
        "fireplaces" => [DecorKind.Fireplace],
        _ => [],
    };
}
