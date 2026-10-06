using Godot;
using IdleBar.Game;

namespace IdleBar.Ui;

public sealed class UpgradeSlot
{
    private static readonly Color UnaffordableTint = new(1, 1, 1, 0.45f);

    private readonly Label _title;
    private readonly Label _cost;

    public UpgradeSlot(UpgradeDefinition upgrade)
    {
        Upgrade = upgrade;
        Button = new Button
        {
            CustomMinimumSize = new Vector2(112, 0),
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = upgrade.Description,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };

        VBoxContainer content = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddThemeConstantOverride("separation", 0);
        _title = CreateLabel(12, BarPalette.Text);
        _cost = CreateLabel(11, BarPalette.Gold);
        content.AddChild(_title);
        content.AddChild(_cost);
        Button.AddChild(content);
        content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public UpgradeDefinition Upgrade { get; }

    public Button Button { get; }

    public void Refresh(GameState game)
    {
        bool affordable = game.CanAfford(Upgrade);
        _title.Text = $"{Upgrade.Name} ×{game.OwnedCount(Upgrade)}";
        _cost.Text = $"{NumberFormat.Amount(game.CostOf(Upgrade))} or";
        _cost.Modulate = affordable ? Colors.White : UnaffordableTint;
        Button.Disabled = !affordable;
    }

    private static Label CreateLabel(int fontSize, Color color)
    {
        Label label = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
