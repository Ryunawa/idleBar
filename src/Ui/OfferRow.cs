using System;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class OfferRow : PanelContainer
{
    private const int Many = 10;

    private Label _title = null!;
    private Label _detail = null!;
    private Button _buy = null!;
    private Button _many = null!;
    private Button _max = null!;
    private int _affordable;

    public event Action<int>? Bought;

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
        _buy = new Button { CustomMinimumSize = new Vector2(150, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _buy.Pressed += () => Bought?.Invoke(1);
        _many = new Button { Text = $"×{Many}", SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _many.Pressed += () => Bought?.Invoke(Many);
        _max = new Button { CustomMinimumSize = new Vector2(84, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _max.Pressed += () => Bought?.Invoke(_affordable);
        row.AddChild(_buy);
        row.AddChild(_many);
        row.AddChild(_max);
        AddChild(row);
    }

    public void Show(string title, string detail, string action, FrontierState state, Purchase purchase)
    {
        Fill(title, detail, action, true);
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

    public void ShowLocked(string title, string detail, string action)
    {
        Fill(title, detail, action, false);
        _buy.Disabled = true;
    }

    private void Fill(string title, string detail, string action, bool open)
    {
        _title.Text = title;
        _detail.Text = detail;
        _buy.Text = action;
        _many.Visible = open;
        _max.Visible = open;
    }
}
