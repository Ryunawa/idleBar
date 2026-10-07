using System.Linq;
using IdleBar.Pixel;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class StreetStates
{
    public static StreetState ForWorkshop(GameSnapshot snapshot, WorkshopState workshop) =>
        new(
            snapshot.Player!.CraftId,
            workshop.Level,
            snapshot.HoldingsLoadAt(workshop.TownId),
            snapshot.StorageCapacityAt(workshop.TownId),
            snapshot.Branches.Count,
            OpenOffersAt(snapshot, workshop.TownId),
            snapshot.MyContracts.Count(contract => contract is { Role: ContractRole.Shipper, IsUnderway: true })
                + snapshot.MyOffers.Count(offer => SupplyView.IsOrder(offer) && offer.Status == OfferStatus.Open),
            News(snapshot));

    public static StreetState ForCaravan(GameSnapshot snapshot, WorldData? world, string townId) =>
        new(
            string.Empty,
            0,
            snapshot.StorageAt(townId).Sum(line => line.Quantity),
            (world?.Rules.DepotCapacity ?? 0) + snapshot.Standing.StorageBonusAt(townId),
            0,
            OpenOffersAt(snapshot, townId),
            snapshot.Contracts.Count(contract => contract.OriginTownId == townId) + snapshot.Supply.Count(order => order.TownId == townId),
            News(snapshot));

    private static int OpenOffersAt(GameSnapshot snapshot, string townId) =>
        snapshot.MyOffers.Count(offer => offer.TownId == townId && offer.Status == OfferStatus.Open);

    private static int News(GameSnapshot snapshot) => snapshot.JournalUnseen + snapshot.News.Total;
}