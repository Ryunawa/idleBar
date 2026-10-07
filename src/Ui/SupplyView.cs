using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class SupplyView : VBoxContainer, ITownPanel
{
    private const string DeliverHelp = "Des artisans et des marchands paient pour qu'on leur apporte des marchandises. Achète-les là où elles sont bon marché, puis livre-les dans leur ville : tu es payé tout de suite. Livrer prend d'abord dans l'entrepôt de la ville, puis dans ta cale.";
    private const string OrderHelp = "Commande une marchandise livrée dans ta ville : tes écus sont bloqués, le premier caravanier qui l'apporte est payé et la marchandise arrive dans ton entrepôt.";

    private Label _help = null!;
    private SupplyForm _form = null!;
    private Label _listHeading = null!;
    private VBoxContainer _list = null!;
    private string _townId = string.Empty;

    public event Action<TownCommand>? Requested;

    public static bool IsOrder(OfferInfo offer) => offer.GiveGoodId is null && offer.WantGoodId is not null;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        _help = ActionRow.Note(string.Empty);
        AddChild(_help);
        _form = new SupplyForm();
        _form.Submitted += draft => Requested?.Invoke(actions => actions.PostOfferAsync(draft, _townId));
        AddChild(_form);
        _listHeading = ActionRow.Heading("Mes commandes");
        AddChild(_listHeading);
        _list = ContractRow.AddScrollList(this);
    }

    public void Refresh(TownContext context)
    {
        _townId = context.TownId;
        _help.Text = context.Itinerant ? DeliverHelp : OrderHelp;
        _form.Visible = !context.Itinerant;
        _listHeading.Visible = !context.Itinerant;
        if (!context.Itinerant)
        {
            _form.Refresh(context);
        }

        ActionRow.Clear(_list);
        if (context.Itinerant)
        {
            ListDeliveries(context);
        }
        else
        {
            ListOrders(context);
        }
    }

    private void ListDeliveries(TownContext context)
    {
        OfferInfo[] orders = context.Snapshot.Supply
            .OrderByDescending(order => order.TownId == context.TownId)
            .ThenBy(order => order.ExpiresAt)
            .ToArray();
        if (orders.Length == 0)
        {
            _list.AddChild(ActionRow.Note("Personne ne commande de marchandise pour l'instant."));
            return;
        }

        foreach (OfferInfo order in orders)
        {
            string? blocker = FindBlocker(context, order);
            ContractRowContent content = new(
                order.WantGoodId!,
                Title(context.World, order),
                ListFacts(context, order),
                blocker ?? $"Commandé par {order.Seller ?? "un marchand"}.",
                blocker is null ? BarPalette.Muted : BarPalette.Warning,
                null);
            long offerId = order.Id;
            _list.AddChild(ContractRow.Create(content, "Livrer", blocker is null,
                () => Requested?.Invoke(actions => actions.AcceptOfferAsync(offerId))));
        }
    }

    private void ListOrders(TownContext context)
    {
        DateTimeOffset now = context.Clock.Now;
        OfferInfo[] orders = context.Snapshot.MyOffers
            .Where(IsOrder)
            .OrderByDescending(order => order.Status == OfferStatus.Open)
            .ToArray();
        if (orders.Length == 0)
        {
            _list.AddChild(ActionRow.Note("Aucune commande pour l'instant."));
            return;
        }

        foreach (OfferInfo order in orders)
        {
            ContractRowContent content = new(
                order.WantGoodId!,
                Title(context.World, order),
                [$"Paie {NumberFormat.Coins(order.GiveQuantity)}", UnitText(order)],
                State(order, now),
                order.Status switch
                {
                    OfferStatus.Concluded => BarPalette.Success,
                    OfferStatus.Open => BarPalette.Muted,
                    _ => BarPalette.Gold,
                },
                null);
            long offerId = order.Id;
            _list.AddChild(order.Status == OfferStatus.Open
                ? ContractRow.Create(content, "Retirer", true, () => Requested?.Invoke(actions => actions.CancelOfferAsync(offerId)))
                : ContractRow.Create(content));
        }
    }

    private static string Title(WorldData world, OfferInfo order) =>
        $"{ExchangeText.Lot(world, order.WantGoodId, order.WantQuantity)} · à livrer à {world.TownName(order.TownId)}";

    private static string UnitText(OfferInfo order) => $"{NumberFormat.Rate(Math.Floor(order.GiveQuantity * 10.0 / order.WantQuantity) / 10)} écus l'unité";

    private static IReadOnlyList<string> ListFacts(TownContext context, OfferInfo order)
    {
        List<string> facts =
        [
            $"Paie {NumberFormat.Coins(order.GiveQuantity)}",
            UnitText(order),
            $"Jusqu'à {DurationFormat.Moment(order.ExpiresAt, context.Clock.Now)}",
        ];
        string[] producers = context.World.Towns
            .Where(town => town.Produces.Contains(order.WantGoodId!) && town.Id != order.TownId)
            .Select(town => town.Name)
            .ToArray();
        if (producers.Length > 0)
        {
            facts.Add($"Bon marché à {string.Join(", ", producers)}");
        }

        return facts;
    }

    private static string? FindBlocker(TownContext context, OfferInfo order)
    {
        if (order.TownId != context.TownId)
        {
            return $"À livrer à {context.World.TownName(order.TownId)} : apporte la marchandise là-bas.";
        }

        GameSnapshot snapshot = context.Snapshot;
        int held = snapshot.OwnedQuantity(order.WantGoodId!, context.TownId)
            + (snapshot.StorageAt(context.TownId).FirstOrDefault(line => line.GoodId == order.WantGoodId)?.Quantity ?? 0);
        return held >= order.WantQuantity
            ? null
            : $"Il t'en manque {NumberFormat.Amount(order.WantQuantity - held)} : tu en as {NumberFormat.Amount(held)} dans ta cale et l'entrepôt de la ville.";
    }

    private static string State(OfferInfo order, DateTimeOffset now) => order.Status switch
    {
        OfferStatus.Open => $"En attente d'une livraison jusqu'à {DurationFormat.Moment(order.ExpiresAt, now)}.",
        OfferStatus.Concluded => $"Livrée par {order.Buyer ?? "un ancien joueur"} : la marchandise est dans ton entrepôt.",
        OfferStatus.Expired => "Personne n'a livré à temps : tes écus te sont revenus.",
        _ => "Retirée : tes écus te sont revenus.",
    };
}