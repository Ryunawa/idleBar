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
    IReadOnlyList<WarehouseLine> Warehouses,
    IReadOnlyList<string> Branches,
    IReadOnlyList<MarketQuote> Market,
    IReadOnlyList<OfferInfo> Offers,
    IReadOnlyList<OfferInfo> MyOffers,
    IReadOnlyList<ContractInfo> Contracts,
    IReadOnlyList<ContractInfo> MyContracts,
    ExchangeNews News,
    IReadOnlyList<StandingOrder> Orders,
    IReadOnlyList<JournalEntry> Journal,
    IReadOnlyList<SpecialOrderInfo> SpecialOrders,
    int JournalUnseen,
    MasteryState? Mastery,
    IReadOnlyList<MasterpieceInfo> Masterpieces,
    IReadOnlyList<string> Fittings,
    TripEventInfo? TripEvent,
    ReputationState? Reputation,
    OddJobsState? OddJobs = null,
    IReadOnlyList<OfferInfo>? SupplyRequests = null,
    IReadOnlyList<TradeLogEntry>? Trades = null)
{
    public IReadOnlyList<TradeLogEntry> TradeLog => Trades ?? [];

    public IReadOnlyList<OfferInfo> Supply => SupplyRequests ?? [];

    public ReputationState Standing => Reputation ?? ReputationState.None;

    public string? DefaultTownId => Caravan?.TownId ?? Player?.HomeTownId;

    public IReadOnlyList<string> PresentTownsAt(DateTimeOffset now) => Caravan switch
    {
        { } caravan when caravan.IsTravelling(now) => [],
        { } caravan => [caravan.TownId],
        null when Player is null => [],
        null => [Player.HomeTownId, .. Branches],
    };

    public IReadOnlyList<StockLine> StorageAt(string townId) =>
        Warehouses.Where(line => line.TownId == townId).Select(line => new StockLine(line.GoodId, line.Quantity)).ToList();

    public IReadOnlyList<StockLine> HoldingsAt(string townId) => Caravan is null ? StorageAt(townId) : Cargo;

    public int HoldingsLoadAt(string townId) => Caravan?.Load ?? StorageAt(townId).Sum(line => line.Quantity);

    public int HoldingsCapacityAt(string townId) => Caravan?.Capacity ?? StorageCapacityAt(townId);

    public int StorageCapacityAt(string townId) => (Workshop?.StorageCapacity ?? 0) + Standing.StorageBonusAt(townId);

    public int OwnedQuantity(string goodId, string townId) =>
        HoldingsAt(townId).FirstOrDefault(line => line.GoodId == goodId)?.Quantity ?? 0;

    public IEnumerable<MarketQuote> MarketAt(string townId) => Market.Where(quote => quote.TownId == townId);

    public IEnumerable<OfferInfo> OffersAt(string townId) => Offers.Where(offer => offer.TownId == townId);

    public string? ChoiceFor(string kindId) => Orders.FirstOrDefault(order => order.KindId == kindId)?.ChoiceId;

    public double TripProgress(DateTimeOffset now)
    {
        if (Caravan is not { DepartedAt: DateTimeOffset departure, ArrivesAt: DateTimeOffset arrival } caravan)
        {
            return 1;
        }

        if (TripEvent is not TripEventInfo halt)
        {
            return caravan.Progress(now);
        }

        double planned = (arrival - (halt.DecideBy - halt.OccurredAt) - departure).TotalSeconds;
        return planned <= 0 ? 1 : Math.Clamp((halt.OccurredAt - departure).TotalSeconds / planned, 0, 1);
    }

    public bool HadOnThisTrip(string kindId) =>
        Caravan?.DepartedAt is DateTimeOffset departure
        && Journal.Any(entry => entry.KindId == kindId && entry.HappenedAt >= departure);

    public DateTimeOffset? NextChangeAt =>
        new[] { Caravan?.ArrivesAt, Workshop?.NextBatchAt }
            .Concat(MyContracts.Select(contract => contract.NextChangeAt))
            .Concat(MyOffers.Where(offer => offer.Status == OfferStatus.Open).Select(offer => (DateTimeOffset?)offer.ExpiresAt))
            .Concat(SpecialOrders.Where(order => order.Status == SpecialOrderStatus.Open).Select(order => (DateTimeOffset?)order.Deadline))
            .Append(TripEvent?.DecideBy)
            .Where(moment => moment > ServerTime)
            .Min();

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
