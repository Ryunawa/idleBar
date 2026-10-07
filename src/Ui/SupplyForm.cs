using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class SupplyForm : VBoxContainer
{
    private const int MaxAmount = 99999;
    private const float PickerWidth = 170;
    private const float CaptionWidth = 90;
    private const double RewardShare = 0.1;

    private readonly ChoicePicker _good = new(PickerWidth);
    private Label _heading = null!;
    private SpinBox _quantity = null!;
    private SpinBox _price = null!;
    private SpinBox _reward = null!;
    private Button _publish = null!;
    private Label _preview = null!;
    private TownContext? _context;
    private string? _pricedGood;

    public event Action<OfferDraft>? Submitted;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        _heading = ActionRow.Heading(string.Empty);
        AddChild(_heading);

        _quantity = AmountBox.Create(10, MaxAmount);
        _price = AmountBox.Create(10, MaxAmount);
        _reward = AmountBox.Create(10, MaxAmount);
        _publish = new Button { Text = "Commander", FocusMode = FocusModeEnum.None };
        _publish.Pressed += Submit;
        AddChild(CreateLine(Caption("Marchandise", CaptionWidth), _good.Button, Caption("Quantité", 0), _quantity));
        AddChild(CreateLine(Caption("Prix unitaire", CaptionWidth), _price, Caption("Récompense", 0), _reward, Caption("écus", 0), _publish));

        _preview = ActionRow.Note(string.Empty);
        AddChild(_preview);
        _quantity.ValueChanged += _ => UpdatePreview();
        _price.ValueChanged += _ => UpdatePreview();
        _reward.ValueChanged += _ => UpdatePreview();
        _good.Button.ItemSelected += _ => SuggestPrice();
    }

    public void Refresh(TownContext context)
    {
        _context = context;
        WorldData world = context.World;
        _heading.Text = $"Nouvelle commande livrée à {context.TownName}";
        _good.Fill(world.Goods.Where(good => good.MarketSells).Select(good => new PickerChoice(good.Id, good.Name)).ToList());
        if (_good.SelectedId != _pricedGood)
        {
            SuggestPrice();
            return;
        }

        UpdatePreview();
    }

    private void SuggestPrice()
    {
        _pricedGood = _good.SelectedId;
        if (_context is { } context && _good.SelectedId is string goodId)
        {
            double market = context.Snapshot.MarketAt(context.TownId).FirstOrDefault(quote => quote.GoodId == goodId)?.BuyPrice
                ?? context.World.FindGood(goodId)?.BasePrice ?? 1;
            _price.SetValueNoSignal(Math.Max(1, Math.Ceiling(market)));
            _reward.SetValueNoSignal(Math.Max(1, Math.Ceiling(_price.Value * _quantity.Value * RewardShare)));
        }

        UpdatePreview();
    }

    private static Label Caption(string text, float width)
    {
        Label caption = BarLabels.Create(13, BarPalette.Muted);
        caption.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        caption.CustomMinimumSize = new Vector2(width, 0);
        caption.Text = text;
        return caption;
    }

    private static HBoxContainer CreateLine(params Control[] controls)
    {
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 8);
        foreach (Control control in controls)
        {
            line.AddChild(control);
        }

        return line;
    }

    private int Total => (int)(_quantity.Value * _price.Value + _reward.Value);

    private void UpdatePreview()
    {
        if (_context is not { } context || _good.SelectedId is not string goodId)
        {
            _publish.Disabled = true;
            _preview.Text = string.Empty;
            return;
        }

        WorldData world = context.World;
        int quantity = (int)_quantity.Value;
        string lot = ExchangeText.Lot(world, goodId, quantity);
        bool affordable = context.Player.Coins >= Total;
        _publish.Disabled = !affordable;
        _preview.Text = affordable
            ? $"Tu bloques {NumberFormat.Coins(Total)} maintenant : {NumberFormat.Coins(_price.Value)} par unité et {NumberFormat.Coins(_reward.Value)} de récompense. "
                + $"Le premier caravanier qui apporte {lot} à {context.TownName} est payé, et la marchandise arrive dans ton entrepôt. "
                + $"Sans livraison sous {NumberFormat.Count(world.Rules.OfferHours, "heure", "heures")}, tes écus te reviennent."
            : $"Cette commande bloque {NumberFormat.Coins(Total)}, tu en as {NumberFormat.Amount(context.Player.Coins)}.";
    }

    private void Submit()
    {
        if (_good.SelectedId is string goodId)
        {
            Submitted?.Invoke(new OfferDraft(null, Total, goodId, (int)_quantity.Value, false));
        }
    }
}