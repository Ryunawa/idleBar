using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Trade;

public sealed record GameSnapshot(
    DateTimeOffset ServerTime,
    PlayerState? Player,
    CaravanState? Caravan,
    IReadOnlyList<StockLine> Cargo,
    WorkshopState? Workshop,
    IReadOnlyList<StockLine> Storage,
    IReadOnlyList<MarketQuote> Market)
{
    public IReadOnlyList<StockLine> Holdings => Caravan is null ? Storage : Cargo;

    public int HoldingsLoad => Holdings.Sum(line => line.Quantity);

    public int HoldingsCapacity => Caravan?.Capacity ?? Workshop?.StorageCapacity ?? 0;

    public DateTimeOffset? NextChangeAt =>
        Caravan?.ArrivesAt is DateTimeOffset arrival && arrival > ServerTime ? arrival : Workshop?.NextBatchAt;

    public int OwnedQuantity(string goodId) => Holdings.FirstOrDefault(line => line.GoodId == goodId)?.Quantity ?? 0;

    public bool EndsTripOf(GameSnapshot? previous) =>
        previous?.Caravan is { ArrivesAt: DateTimeOffset arrival } before
        && Caravan is { } after
        && before.ArrivesAt == after.ArrivesAt
        && previous.ServerTime < arrival
        && ServerTime >= arrival;

    public int BatchesDeliveredSince(GameSnapshot? previous) =>
        previous?.Workshop is { Queued: > 0 } before
        && Workshop is { } after
        && after.Queued < before.Queued
        && (after.RecipeId is null || after.RecipeId == before.RecipeId)
            ? before.Queued - after.Queued
            : 0;
}
