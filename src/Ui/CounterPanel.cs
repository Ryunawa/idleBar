using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class CounterPanel : VBoxContainer, ITownPanel
{
    private VBoxContainer _list = null!;
    private OfferForm _form = null!;
    private string _townId = string.Empty;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_list);
        AddChild(scroll);

        AddChild(new HSeparator());
        _form = new OfferForm();
        _form.Submitted += draft => Requested?.Invoke(actions => actions.PostOfferAsync(draft, _townId));
        AddChild(_form);
        AddChild(ActionRow.Note("Ce que tu reçois arrive à l'entrepôt de la ville. Une offre reste 48 h au comptoir, puis te revient."));
    }

    public void Refresh(TownContext context)
    {
        _townId = context.TownId;
        _form.Refresh(context);
        ActionRow.Clear(_list);
        WorldData world = context.World;
        DateTimeOffset now = context.Clock.Now;

        _list.AddChild(ActionRow.Heading($"Offres des autres marchands à {context.TownName}"));
        OfferInfo[] offers = context.Snapshot.OffersAt(context.TownId).ToArray();
        if (offers.Length == 0)
        {
            _list.AddChild(ActionRow.Note("Personne ne propose rien ici pour l'instant."));
        }

        foreach (OfferInfo offer in offers)
        {
            string detail = $"{offer.Seller} · jusqu'à {DurationFormat.Moment(offer.ExpiresAt, now)} · tu paies {ExchangeText.Lot(world, offer.WantGoodId, offer.WantQuantity)}";
            long offerId = offer.Id;
            _list.AddChild(ActionRow.Create(ExchangeText.Offer(world, offer), detail, "Accepter", !CanPay(context, offer),
                () => Requested?.Invoke(actions => actions.AcceptOfferAsync(offerId))));
        }

        if (context.Snapshot.MyOffers.Count == 0)
        {
            return;
        }

        _list.AddChild(ActionRow.Heading("Mes offres"));
        foreach (OfferInfo offer in context.Snapshot.MyOffers)
        {
            long offerId = offer.Id;
            bool open = offer.Status == OfferStatus.Open;
            _list.AddChild(ActionRow.Create(ExchangeText.Offer(world, offer), ExchangeText.OfferState(world, offer, now), open ? "Retirer" : string.Empty, false,
                () => Requested?.Invoke(actions => actions.CancelOfferAsync(offerId))));
        }
    }

    private static bool CanPay(TownContext context, OfferInfo offer) =>
        offer.WantGoodId is string goodId ? context.Owned(goodId) >= offer.WantQuantity : context.Player.Coins >= offer.WantQuantity;
}
