using Godot;

namespace IdleBar.Ui;

public sealed class BarSlot
{
    private const int Width = 128;
    private const int PaddingX = 4;
    private const int PaddingY = 2;

    private readonly Label _title;
    private readonly Label _detail;
    private SlotContent? _shown;

    public BarSlot()
    {
        Root = new MarginContainer
        {
            CustomMinimumSize = new Vector2(Width, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        Button = new Button
        {
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        Root.AddChild(Button);

        MarginContainer padding = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        padding.AddThemeConstantOverride("margin_left", PaddingX);
        padding.AddThemeConstantOverride("margin_right", PaddingX);
        padding.AddThemeConstantOverride("margin_top", PaddingY);
        padding.AddThemeConstantOverride("margin_bottom", PaddingY);
        VBoxContainer content = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddThemeConstantOverride("separation", 0);
        _title = BarLabels.Create(12, BarPalette.Text, HorizontalAlignment.Center);
        _detail = BarLabels.Create(11, BarPalette.Muted, HorizontalAlignment.Center);
        content.AddChild(_title);
        content.AddChild(_detail);
        padding.AddChild(content);
        Root.AddChild(padding);
    }

    public Control Root { get; }

    public Button Button { get; }

    public void Refresh(SlotContent content)
    {
        if (content == _shown)
        {
            return;
        }

        _shown = content;
        _title.Text = content.Title;
        _detail.Text = content.Detail;
        _detail.AddThemeColorOverride("font_color", content.Color);
        Button.TooltipText = content.Tooltip;
    }
}
