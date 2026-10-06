using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class MarketPanel : VBoxContainer, ITownPanel
{
    private const int MaxQuantity = 1000;

    private static readonly int[] Quantities = [1, 10, MaxQuantity];
    private static readonly string[] Headers = ["Marchandise", "En stock", "Achat", string.Empty, "Vente", string.Empty];

    private GridContainer _grid = null!;
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

        Label legend = BarLabels.Create(11, BarPalette.Muted);
        legend.Text = "Prix vert : spécialité de la ville, bon marché. Prix doré : la ville en manque et paie cher.";
        AddChild(legend);
    }

    public void Refresh(TownContext context)
    {
        ActionRow.Clear(_grid);
        foreach (string header in Headers)
        {
            _grid.AddChild(CreateCell(header, BarPalette.Muted));
        }

        string townId = context.TownId;
        TownInfo? town = context.World.FindTown(townId);
        foreach (MarketQuote quote in context.Snapshot.MarketAt(townId))
        {
            int owned = context.Owned(quote.GoodId);
            bool local = town?.Produces.Contains(quote.GoodId) == true;
            bool wanted = town?.Demands.Contains(quote.GoodId) == true;
            _grid.AddChild(CreateCell(context.World.GoodName(quote.GoodId), BarPalette.Text));
            _grid.AddChild(CreateCell(owned > 0 ? NumberFormat.Amount(owned) : "–", BarPalette.Muted));
            _grid.AddChild(CreateCell(NumberFormat.Rate(quote.BuyPrice), local ? BarPalette.Success : BarPalette.Text));
            _grid.AddChild(CreateAction("Acheter", () => Requested?.Invoke(actions => actions.BuyAsync(quote.GoodId, _quantity, townId)), context.World.FindGood(quote.GoodId)?.MarketSells == false));
            _grid.AddChild(CreateCell(NumberFormat.Rate(quote.SellPrice), wanted ? BarPalette.Gold : BarPalette.Text));
            _grid.AddChild(CreateAction("Vendre", () => Requested?.Invoke(actions => actions.SellAsync(quote.GoodId, _quantity, townId)), owned == 0));
        }
    }

    private static Label CreateCell(string text, Color color)
    {
        Label cell = BarLabels.Create(13, color);
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
