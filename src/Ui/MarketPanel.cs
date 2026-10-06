using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class MarketPanel : VBoxContainer
{
    private const int MaxQuantity = 1000;

    private static readonly int[] Quantities = [1, 10, MaxQuantity];
    private static readonly string[] Headers = ["Marchandise", "En stock", "Achat", string.Empty, "Vente", string.Empty];

    private GridContainer _grid = null!;
    private int _quantity = 1;

    public event Action<string, int>? BuyRequested;

    public event Action<string, int>? SellRequested;

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

    public void Refresh(WorldData world, GameSnapshot snapshot)
    {
        foreach (Node child in _grid.GetChildren())
        {
            _grid.RemoveChild(child);
            child.QueueFree();
        }

        foreach (string header in Headers)
        {
            _grid.AddChild(CreateCell(header, BarPalette.Muted));
        }

        TownInfo? town = world.FindTown(snapshot.Caravan!.TownId);
        foreach (MarketQuote quote in snapshot.Market)
        {
            int owned = snapshot.OwnedQuantity(quote.GoodId);
            bool local = town?.Produces.Contains(quote.GoodId) == true;
            bool wanted = town?.Demands.Contains(quote.GoodId) == true;
            _grid.AddChild(CreateCell(world.GoodName(quote.GoodId), BarPalette.Text));
            _grid.AddChild(CreateCell(owned > 0 ? NumberFormat.Amount(owned) : "–", BarPalette.Muted));
            _grid.AddChild(CreateCell(NumberFormat.Rate(quote.BuyPrice), local ? BarPalette.Success : BarPalette.Text));
            _grid.AddChild(CreateAction("Acheter", () => BuyRequested?.Invoke(quote.GoodId, _quantity), false));
            _grid.AddChild(CreateCell(NumberFormat.Rate(quote.SellPrice), wanted ? BarPalette.Gold : BarPalette.Text));
            _grid.AddChild(CreateAction("Vendre", () => SellRequested?.Invoke(quote.GoodId, _quantity), owned == 0));
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
