using System;
using System.Collections.Generic;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class ExchangeText
{
    public static string Lot(WorldData world, string? goodId, int quantity) =>
        goodId is null
            ? NumberFormat.Coins(quantity)
            : $"{NumberFormat.Amount(quantity)} {world.GoodNoun(goodId, quantity)}";

    public static string Offer(WorldData world, OfferInfo offer) =>
        $"{Lot(world, offer.GiveGoodId, offer.GiveQuantity)} contre {Lot(world, offer.WantGoodId, offer.WantQuantity)}";

    public static string OfferState(WorldData world, OfferInfo offer, DateTimeOffset now) => offer.Status switch
    {
        OfferStatus.Open => $"{world.TownName(offer.TownId)} · en attente jusqu'à {DurationFormat.Moment(offer.ExpiresAt, now)}",
        OfferStatus.Concluded => $"{world.TownName(offer.TownId)} · conclue avec {offer.Buyer ?? "un ancien joueur"}",
        OfferStatus.Expired => $"{world.TownName(offer.TownId)} · expirée, rendue à l'entrepôt",
        _ => $"{world.TownName(offer.TownId)} · retirée",
    };

    public static string Journey(WorldData world, ContractInfo contract) =>
        $"{Lot(world, contract.GoodId, contract.Quantity)} · {world.TownName(contract.OriginTownId)} → {world.TownName(contract.DestinationTownId)}";

    public static string ContractState(ContractInfo contract, DateTimeOffset now) => (contract.Status, contract.Role) switch
    {
        (ContractStatus.Open, _) => $"en attente d'un caravanier · sinon transporteur du jeu à {DurationFormat.Moment(contract.TakeoverAt, now)}",
        (ContractStatus.Carried, ContractRole.Carrier) => $"à livrer avant {DurationFormat.Moment(contract.Deadline!.Value, now)} · {NumberFormat.Coins(contract.Reward)} + caution",
        (ContractStatus.Carried, _) => $"{contract.Carrier ?? "un caravanier"} transporte · avant {DurationFormat.Moment(contract.Deadline!.Value, now)}",
        (ContractStatus.Shipped, _) => $"transporteur du jeu · arrivée vers {DurationFormat.Moment(contract.GameArrivesAt, now)}",
        (ContractStatus.Delivered, ContractRole.Carrier) => $"livré · gain de {NumberFormat.Coins(contract.Reward)}, caution rendue",
        (ContractStatus.Delivered, _) => "livré à l'entrepôt",
        (ContractStatus.Failed, ContractRole.Carrier) => "en retard · caution perdue, marchandise gardée",
        (ContractStatus.Failed, _) => $"en retard · caution de {NumberFormat.Coins(contract.Deposit)} reçue",
        _ => "annulé",
    };

    public static string News(ExchangeNews news)
    {
        List<string> parts = [];
        AddPart(parts, news.Concluded, "offre conclue", "offres conclues");
        AddPart(parts, news.Expired, "offre expirée", "offres expirées");
        AddPart(parts, news.Delivered, "contrat livré", "contrats livrés");
        AddPart(parts, news.Failed, "contrat en retard", "contrats en retard");
        return string.Join(" · ", parts);
    }

    private static void AddPart(List<string> parts, int count, string one, string many)
    {
        if (count > 0)
        {
            parts.Add(NumberFormat.Count(count, one, many));
        }
    }
}
