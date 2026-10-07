using System;
using System.Globalization;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class MarketPanel : VBoxContainer, ITownPanel
{
    private const int MaxQuantity = 1000;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private const string PriceLegend = "Prix vert : abondant ici en ce moment, bon marché. Prix doré : la ville en manque et paie cher. Les besoins des villes changent chaque jour, et tes achats et ventes marquent les prix pendant environ un jour.";

    private static readonly int[] Quantities = [1, 10, MaxQuantity];
    private static readonly string[] Headers = ["Marchandise", "En stock", "Achat", string.Empty, "Vente", string.Empty];
    private static readonly string[] CaravanHeaders = ["Marchandise", "Cale", "Entrepôt", "Achat", string.Empty, "Vente", string.Empty];

    private GridContainer _grid = null!;
    private Label _legend = null!;
    private int _quantity = 1;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        AddChild(BatchPicker.Create("Quantité par clic :", Quantities, MaxQuantity, quantity => _quantity = quantity));

        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _grid = new GridContainer { Columns = Headers.Length, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _grid.AddThemeConstantOverride("h_separation", 14);
        _grid.AddThemeConstantOverride("v_separation", 4);
        scroll.AddChild(_grid);
        AddChild(scroll);

        _legend = ActionRow.Note(PriceLegend);
        AddChild(_legend);
    }

    public void Refresh(TownContext context)
    {
        ActionRow.Clear(_grid);
        string[] headers = context.Itinerant ? CaravanHeaders : Headers;
        _grid.Columns = headers.Length;
        foreach (string header in headers)
        {
            _grid.AddChild(CreateCell(header, BarPalette.Muted));
        }

        _legend.Text = context switch
        {
            { Itinerant: true } => $"{PriceLegend} Vendre prend d'abord dans l'entrepôt de la ville, puis dans la cale.",
            _ when context.World.FindCraft(context.Player.CraftId)?.OpensBranches == true =>
                $"{PriceLegend} En négociant, tu achètes moins cher et vends plus cher que les autres marchands : c'est déjà compté dans ces prix.",
            _ => PriceLegend,
        };
        if (context.Snapshot.Standing.TierAt(context.TownId) is { PriceRate: > 0 } tier)
        {
            _legend.Text += $" Ta réputation ({tier.Name.ToLowerInvariant()}) te fait gagner {tier.PriceRate.ToString("P0", French)} à l'achat comme à la vente ici.";
        }

        string townId = context.TownId;
        TownInfo? town = context.World.FindTown(townId);
        foreach (MarketQuote quote in context.Snapshot.MarketAt(townId))
        {
            int owned = context.Owned(quote.GoodId);
            int stored = context.Itinerant ? context.Storage.FirstOrDefault(line => line.GoodId == quote.GoodId)?.Quantity ?? 0 : 0;
            bool local = quote.Trend is null ? town?.Produces.Contains(quote.GoodId) == true : quote.Cheap;
            bool wanted = quote.Trend is null ? town?.Demands.Contains(quote.GoodId) == true : quote.Dear;
            _grid.AddChild(GoodBadge.Create(quote.GoodId, context.World.GoodName(quote.GoodId), 13, BarPalette.Text));
            _grid.AddChild(CreateCell(owned > 0 ? NumberFormat.Amount(owned) : "–", BarPalette.Muted));
            if (context.Itinerant)
            {
                _grid.AddChild(CreateCell(stored > 0 ? NumberFormat.Amount(stored) : "–", BarPalette.Muted));
            }

            _grid.AddChild(CreateCell(NumberFormat.Rate(quote.BuyPrice), local ? BarPalette.Success : BarPalette.Text));
            _grid.AddChild(CreateAction("Acheter", () => Requested?.Invoke(actions => actions.BuyAsync(quote.GoodId, _quantity, townId)), context.World.FindGood(quote.GoodId)?.MarketSells == false));
            _grid.AddChild(CreateCell(NumberFormat.Rate(quote.SellPrice), wanted ? BarPalette.Gold : BarPalette.Text));
            _grid.AddChild(CreateAction("Vendre", () => Requested?.Invoke(actions => actions.SellAsync(quote.GoodId, _quantity, townId)), owned + stored == 0));
        }
    }

    private static Label CreateCell(string text, Color color)
    {
        Label cell = BarLabels.Create(13, color);
        cell.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        cell.Text = text;
        return cell;
    }

    private static Button CreateAction(string text, Action onPressed, bool disabled)
    {
        Button button = new() { Text = text, Disabled = disabled, FocusMode = FocusModeEnum.None };
        button.Pressed += onPressed;
        return button;
    }
}
