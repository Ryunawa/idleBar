using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class JournalPanel : VBoxContainer, ITownPanel
{
    private VBoxContainer _list = null!;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_list);
        AddChild(scroll);
    }

    public void Refresh(TownContext context)
    {
        ActionRow.Clear(_list);
        DateTimeOffset now = context.Clock.Now;
        if (context.Snapshot.TripEvent is TripEventInfo tripEvent)
        {
            AddTripEvent(context, tripEvent, now);
        }

        if (context.Snapshot.SpecialOrders.Count > 0)
        {
            _list.AddChild(ActionRow.Heading("Commandes spéciales"));
            foreach (SpecialOrderInfo order in context.Snapshot.SpecialOrders)
            {
                _list.AddChild(CreateOrderRow(context, order, now));
            }
        }

        _list.AddChild(ActionRow.Heading("Journal"));
        if (context.Snapshot.Journal.Count == 0)
        {
            _list.AddChild(ActionRow.Note("Rien à signaler pour l'instant."));
        }

        foreach (JournalEntry entry in context.Snapshot.Journal)
        {
            _list.AddChild(CreateEntry(entry, now));
        }

        _list.AddChild(ActionRow.Heading("Achats et ventes"));
        if (context.Snapshot.TradeLog.Count == 0)
        {
            _list.AddChild(ActionRow.Note("Tes achats et tes ventes s'afficheront ici, avec leurs prix."));
        }

        foreach (TradeLogEntry trade in context.Snapshot.TradeLog)
        {
            _list.AddChild(CreateTrade(context.World, trade, now));
        }
    }

    private static VBoxContainer CreateTrade(WorldData world, TradeLogEntry trade, DateTimeOffset now)
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 0);
        Label title = BarLabels.Create(13, trade.Bought ? BarPalette.Text : BarPalette.Success);
        string verb = trade.Bought ? "Acheté" : "Vendu";
        string sign = trade.Bought ? "−" : "+";
        title.Text = $"{DurationFormat.Moment(trade.UpdatedAt, now)} · {verb} {ExchangeText.Lot(world, trade.GoodId, trade.Quantity)} · {sign}{NumberFormat.Coins(trade.Total)}";
        box.AddChild(title);
        string place = trade.Source switch
        {
            "comptoir" => "au comptoir",
            "commande" => "commande spéciale",
            "chef_oeuvre" => "chef-d'œuvre",
            _ => "au marché",
        };
        box.AddChild(ActionRow.Note($"{world.TownName(trade.TownId)} · {place} · {NumberFormat.Rate(Math.Round(trade.UnitPrice, 1))} écus pièce"));
        return box;
    }

    private void AddTripEvent(TownContext context, TripEventInfo tripEvent, DateTimeOffset now)
    {
        WorldData world = context.World;
        if (world.FindEventKind(tripEvent.KindId) is not EventKindInfo kind)
        {
            return;
        }

        string directive = world.FindDirective(context.Snapshot.Caravan?.DirectiveId ?? string.Empty)?.Name ?? "ta directive";
        string fallback = kind.Choices.FirstOrDefault(choice => choice.Id == tripEvent.DirectiveChoiceId)?.Name ?? string.Empty;
        Label title = BarLabels.Create(15, BarPalette.Danger);
        title.Text = $"{kind.Name} · réponds avant {DurationFormat.Moment(tripEvent.DecideBy, now)}";
        _list.AddChild(title);
        _list.AddChild(ActionRow.Note($"{kind.Description} Ta caravane est arrêtée. Sans réponse, ta directive « {directive} » choisira : {fallback.ToLowerInvariant()}."));
        foreach (EventChoiceInfo choice in kind.Choices)
        {
            string choiceId = choice.Id;
            string name = choice.Id == tripEvent.DirectiveChoiceId ? $"{choice.Name} · choix de ta directive" : choice.Name;
            _list.AddChild(ActionRow.Create(name, choice.Description, "Choisir", false,
                () => Requested?.Invoke(actions => actions.AnswerEventAsync(choiceId))));
        }
    }

    private static VBoxContainer CreateEntry(JournalEntry entry, DateTimeOffset now)
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 0);
        Color color = entry.Tone switch
        {
            EventTone.Good => BarPalette.Success,
            EventTone.Bad => BarPalette.Warning,
            _ => BarPalette.Text,
        };
        Label title = BarLabels.Create(13, color);
        title.Text = $"{DurationFormat.Moment(entry.HappenedAt, now)} · {entry.Title}";
        box.AddChild(title);
        box.AddChild(ActionRow.Note(entry.Detail));
        return box;
    }

    private HBoxContainer CreateOrderRow(TownContext context, SpecialOrderInfo order, DateTimeOffset now)
    {
        WorldData world = context.World;
        int held = context.Snapshot.StorageAt(order.TownId).FirstOrDefault(line => line.GoodId == order.GoodId)?.Quantity ?? 0;
        string title = $"{ExchangeText.Lot(world, order.GoodId, order.Quantity)} · {NumberFormat.Coins(order.UnitPrice)} pièce";
        string detail = order.Status switch
        {
            SpecialOrderStatus.Open when order.Crafted => $"Client de {world.TownName(order.TownId)} · avant {DurationFormat.Moment(order.Deadline, now)} · fabriqué {NumberFormat.Amount(order.Produced)}/{NumberFormat.Amount(order.Quantity)} dans ton atelier",
            SpecialOrderStatus.Open => $"Client de {world.TownName(order.TownId)} · avant {DurationFormat.Moment(order.Deadline, now)} · en stock {NumberFormat.Amount(held)}/{NumberFormat.Amount(order.Quantity)}",
            SpecialOrderStatus.Delivered => $"Livrée · payée {NumberFormat.Coins(order.Quantity * order.UnitPrice)}",
            SpecialOrderStatus.Expired => "Expirée",
            _ => "Refusée",
        };

        long orderId = order.Id;
        bool open = order.Status == SpecialOrderStatus.Open;
        HBoxContainer row = ActionRow.Create(title, detail, open ? "Livrer" : string.Empty, !order.IsReady(held),
            () => Requested?.Invoke(actions => actions.FulfillSpecialOrderAsync(orderId)));
        if (open)
        {
            Button decline = new() { Text = "Refuser", FocusMode = FocusModeEnum.None, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            decline.Pressed += () => Requested?.Invoke(actions => actions.DeclineSpecialOrderAsync(orderId));
            row.AddChild(decline);
        }

        return row;
    }
}
