using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class OfferRow : PanelContainer
{
    private const int Many = 10;

    private readonly List<OfferChoice> _options = [];
    private Label _title = null!;
    private Label _detail = null!;
    private OptionButton _choice = null!;
    private Button _buy = null!;
    private Button _many = null!;
    private Button _max = null!;
    private int _affordable;
    private string _plotDetail = string.Empty;

    public event Action<int>? Bought;

    public event Action<int>? Chosen;

    public override void _Ready()
    {
        ThemeTypeVariation = WindowSkin.Card;
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 2);
        _title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _detail = WindowRows.Muted(string.Empty);
        text.AddChild(_title);
        text.AddChild(_detail);
        row.AddChild(text);
        _choice = new OptionButton { CustomMinimumSize = new Vector2(150, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter, Visible = false };
        _choice.ItemSelected += _ => _detail.Text = ChoiceDetail();
        _buy = new Button { CustomMinimumSize = new Vector2(150, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _buy.Pressed += Press;
        _many = new Button { Text = $"×{Many}", SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _many.Pressed += () => Bought?.Invoke(Many);
        _max = new Button { CustomMinimumSize = new Vector2(84, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _max.Pressed += () => Bought?.Invoke(_affordable);
        row.AddChild(_choice);
        row.AddChild(_buy);
        row.AddChild(_many);
        row.AddChild(_max);
        AddChild(row);
    }

    public void Show(string title, string detail, string action, FrontierState state, Purchase purchase)
    {
        Fill(title, detail, action, true, false);
        _affordable = Shop.MaxCount(state, purchase);
        _buy.Disabled = _affordable < 1;
        _many.Disabled = _affordable < Many;
        _many.TooltipText = $"Acheter {Many} niveaux pour {NumberFormat.Gold(Shop.CostOf(state, purchase, Many))}";
        _max.Text = $"Max ×{NumberFormat.Amount(_affordable)}";
        _max.Disabled = _affordable < 1;
        _max.TooltipText = _affordable > 0
            ? $"Acheter {NumberFormat.Count(_affordable, "niveau", "niveaux")} pour {NumberFormat.Gold(Shop.CostOf(state, purchase, _affordable))}"
            : "Pas assez d'or pour un niveau";
    }

    public void ShowChoice(string title, string plotDetail, string action, IReadOnlyList<OfferChoice> options, bool affordable, int? preferred = null)
    {
        _plotDetail = plotDetail;
        if (!_options.SequenceEqual(options))
        {
            int? chosen = _choice.Selected >= 0 ? _choice.GetSelectedId() : null;
            _options.Clear();
            _options.AddRange(options);
            _choice.Clear();
            foreach (OfferChoice option in options)
            {
                _choice.AddItem(option.Name, option.Id);
            }

            if (_options.Count > 0)
            {
                _choice.Select(Math.Max(0, _options.FindIndex(option => option.Id == (chosen ?? preferred))));
            }
        }

        Fill(title, ChoiceDetail(), action, false, true);
        _buy.Disabled = !affordable || _options.Count == 0;
    }

    public void ShowInfo(string title, string detail)
    {
        Fill(title, detail, string.Empty, false, false);
        _buy.Visible = false;
    }

    public void ShowLocked(string title, string detail, string action)
    {
        Fill(title, detail, action, false, false);
        _buy.Disabled = true;
    }

    private void Fill(string title, string detail, string action, bool open, bool choosing)
    {
        _buy.Visible = true;
        _title.Text = title;
        _detail.Text = detail;
        _buy.Text = action;
        _choice.Visible = choosing;
        _many.Visible = open;
        _max.Visible = open;
    }

    private string ChoiceDetail() =>
        _choice.Selected >= 0 ? $"{_plotDetail} {_options[_choice.Selected].Effect}" : _plotDetail;

    private void Press()
    {
        if (_choice.Visible && _choice.Selected >= 0)
        {
            Chosen?.Invoke(_choice.GetSelectedId());
            return;
        }

        Bought?.Invoke(1);
    }
}
