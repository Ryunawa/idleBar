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
    private const float PickerWidth = 170;

    private readonly ChoicePicker _give = new(PickerWidth);
    private readonly ChoicePicker _want = new(PickerWidth);
    private SpinBox _giveQuantity = null!;
    private SpinBox _wantQuantity = null!;

    public event Action<OfferDraft>? Submitted;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        AddChild(ActionRow.Heading("Publier une offre au comptoir"));

        _giveQuantity = AmountBox.Create(10, MaxAmount);
        AddChild(CreateLine("Je donne", _giveQuantity, _give.Button, null));

        _wantQuantity = AmountBox.Create(100, MaxAmount);
        Button publish = new() { Text = "Publier", FocusMode = FocusModeEnum.None };
        publish.Pressed += Submit;
        AddChild(CreateLine("Contre", _wantQuantity, _want.Button, publish));
    }

    public void Refresh(TownContext context)
    {
        PickerChoice coins = new(null, "écus");
        List<PickerChoice> owned = [coins];
        owned.AddRange(context.Holdings.Select(line => new PickerChoice(line.GoodId, $"{context.World.GoodName(line.GoodId)} ({NumberFormat.Amount(line.Quantity)})")));
        _give.Fill(owned);

        List<PickerChoice> goods = [coins];
        goods.AddRange(context.World.Goods.Select(good => new PickerChoice(good.Id, good.Name)));
        _want.Fill(goods);
    }

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
        if (!_give.HasSelection || !_want.HasSelection)
        {
            return;
        }

        Submitted?.Invoke(new OfferDraft(_give.SelectedId, (int)_giveQuantity.Value, _want.SelectedId, (int)_wantQuantity.Value));
    }
}
