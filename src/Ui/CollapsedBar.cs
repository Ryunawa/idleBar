using System;
using Godot;

namespace IdleBar.Ui;

public partial class CollapsedBar : MarginContainer
{
    private Label _summary = null!;

    public event Action? ToggleRequested;

    public event Action? QuitRequested;

    public override void _Ready()
    {
        AddThemeConstantOverride("margin_left", 10);
        AddThemeConstantOverride("margin_right", 10);
        AddThemeConstantOverride("margin_top", 1);
        AddThemeConstantOverride("margin_bottom", 1);

        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 6);
        AddChild(row);

        _summary = BarLabels.Create(12, BarPalette.Gold);
        _summary.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(_summary);
        WindowButtons.Add(row, "+", () => ToggleRequested?.Invoke(), () => QuitRequested?.Invoke());
    }

    public void Refresh(BarStatus status) => _summary.Text = status.Compact;
}
