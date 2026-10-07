using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class OfferForm : VBoxContainer
{
    private const int MaxAmount = 99999;
    private const float CaptionWidth = 70;
    private const float PickerWidth = 200;

    private readonly ChoicePicker _give = new(PickerWidth);
    private readonly ChoicePicker _want = new(PickerWidth);
    private readonly List<OfferStock> _stocks = [];
    private SpinBox _giveQuantity = null!;
    private SpinBox _wantQuantity = null!;

    public event Action<OfferDraft>? Submitted;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        _giveQuantity = AmountBox.Create(10, MaxAmount);
        AddChild(CreateLine("Je donne", _giveQuantity, _give.Button, null));
        _give.Button.ItemSelected += _ => LimitQuantity();

        _wantQuantity = AmountBox.Create(100, MaxAmount);
        Button publish = new() { Text = "Publier", FocusMode = FocusModeEnum.None };
        publish.Pressed += Submit;
        AddChild(CreateLine("Contre", _wantQuantity, _want.Button, publish));
    }

    public void Refresh(TownContext context)
    {
        WorldData world = context.World;
        string? holdingsPlace = context.Itinerant ? "cale" : null;
        _stocks.Clear();
        _stocks.Add(new OfferStock(null, false, "écus", context.Player.Coins));
        _stocks.AddRange(context.Holdings.Select(line => new OfferStock(line.GoodId, false, Describe(world, line, holdingsPlace), line.Quantity)));
        if (context.Itinerant)
        {
            _stocks.AddRange(context.Storage.Select(line => new OfferStock(line.GoodId, true, Describe(world, line, "entrepôt"), line.Quantity)));
        }

        _give.Fill(_stocks.Select(stock => new PickerChoice(stock.PickerId, stock.Label)).ToList());
        LimitQuantity();

        PickerChoice coins = new(null, "écus");
        List<PickerChoice> goods = [coins];
        goods.AddRange(context.World.Goods.Select(good => new PickerChoice(good.Id, good.Name)));
        _want.Fill(goods);
    }

    private void LimitQuantity()
    {
        long available = SelectedStock()?.Available ?? 1;
        _giveQuantity.MaxValue = Math.Clamp(available, 1, MaxAmount);
    }

    private OfferStock? SelectedStock() =>
        _give.HasSelection ? _stocks.FirstOrDefault(stock => stock.PickerId == _give.SelectedId) : null;

    private static string Describe(WorldData world, StockLine line, string? place) =>
        place is null
            ? $"{world.GoodName(line.GoodId)} ({NumberFormat.Amount(line.Quantity)})"
            : $"{world.GoodName(line.GoodId)} · {place} ({NumberFormat.Amount(line.Quantity)})";

    private static HBoxContainer CreateLine(string caption, SpinBox quantity, OptionButton picker, Button? action)
    {
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 8);
        Label label = BarLabels.Create(13, BarPalette.Muted);
        label.Text = caption;
        label.CustomMinimumSize = new Vector2(CaptionWidth, 0);
        line.AddChild(label);
        line.AddChild(quantity);
        line.AddChild(picker);
        if (action is not null)
        {
            line.AddChild(action);
        }

        return line;
    }

    private void Submit()
    {
        if (!_want.HasSelection || SelectedStock() is not OfferStock stock)
        {
            return;
        }

        Submitted?.Invoke(new OfferDraft(stock.GoodId, (int)_giveQuantity.Value, _want.SelectedId, (int)_wantQuantity.Value, stock.FromWarehouse));
    }
}
