using System.Collections.Generic;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed record TownContext(WorldData World, GameSnapshot Snapshot, ServerClock Clock, string TownId)
{
    public PlayerState Player => Snapshot.Player!;

    public bool Itinerant => Snapshot.Caravan is not null;

    public string TownName => World.TownName(TownId);

    public IReadOnlyList<StockLine> Holdings => Snapshot.HoldingsAt(TownId);

    public IReadOnlyList<StockLine> Storage => Snapshot.StorageAt(TownId);

    public int Owned(string goodId) => Snapshot.OwnedQuantity(goodId, TownId);
}
