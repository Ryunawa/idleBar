using System.Collections.Generic;
using System.Linq;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class CounterStatus
{
    private const int ShownGoods = 12;

    public static BarStatus Describe(GameSession session, WorkshopState counter, string coins)
    {
        WorldData? world = session.World;
        GameSnapshot snapshot = session.Snapshot!;
        string town = world?.TownName(counter.TownId) ?? counter.TownId;
        string network = snapshot.Branches.Count == 0 ? "sans succursale" : NumberFormat.Count(snapshot.Branches.Count, "succursale", "succursales");
        string situation = $"{MasteryText.Title(snapshot.Mastery?.Rank, world?.FindCraft(snapshot.Player!.CraftId)?.Name ?? "Négociant")} à {town} · {network}";
        int offers = snapshot.MyOffers.Count(offer => offer.Status == OfferStatus.Open);
        int shipments = snapshot.MyContracts.Count(contract => contract is { Role: ContractRole.Shipper, IsUnderway: true });
        bool busy = offers + shipments > 0;
        IReadOnlyList<string> goods = snapshot.StorageAt(counter.TownId)
            .SelectMany(line => Enumerable.Repeat(line.GoodId, line.Quantity))
            .Take(ShownGoods)
            .ToList();
        Biome biome = world?.FindTown(counter.TownId)?.Biome ?? Biome.Plain;
        string activity = $"{NumberFormat.Count(offers, "offre", "offres")} · {NumberFormat.Count(shipments, "envoi", "envois")}";
        SlotContent slot = busy
            ? new SlotContent("Comptoir", activity, BarPalette.Success, $"{activity} en cours. Clique pour ouvrir ton comptoir.")
            : new SlotContent("Comptoir", "Rien en cours", BarPalette.Warning, "Aucune offre ni expédition en cours. Clique pour ouvrir ton comptoir.");
        return new BarStatus(
            coins,
            situation,
            situation,
            slot,
            LaneScene.Workshop(biome, snapshot.Player!.CraftId, busy, 0, goods, busy ? town : $"{town} · comptoir calme", !busy));
    }
}
