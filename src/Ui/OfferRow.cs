using System;
using Godot;

namespace IdleBar.Ui;

public partial class OfferRow : PanelContainer
{
    private const int Many = 10;

    private Label _title = null!;
    private Label _detail = null!;
    private Button _buy = null!;
    private Button _many = null!;

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
        _many = new Button { Text = $"×{Many}", SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = $"Acheter jusqu'à {Many} niveaux d'un coup" };
        _many.Pressed += () => Bought?.Invoke(Many);
        row.AddChild(_buy);
        row.AddChild(_many);
        AddChild(row);
    }

    public void Show(string title, string detail, string action, bool affordable, bool locked)
    {
        _title.Text = title;
        _detail.Text = detail;
        _buy.Text = action;
        _buy.Disabled = locked || !affordable;
        _many.Visible = !locked;
        _many.Disabled = !affordable;
    }
}
