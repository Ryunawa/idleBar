using Godot;

namespace IdleBar.Ui;

public sealed class TownSlot
{
    private readonly Label _title;
    private readonly Label _detail;
    private SlotContent? _shown;

    public TownSlot()
    {
        Button = new Button
        {
            CustomMinimumSize = new Vector2(128, 0),
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };

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
        Button.AddChild(content);
        content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

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
