using System;
using Godot;

namespace IdleBar.Ui;

public partial class VisitSlot : HBoxContainer
{
    private static readonly string[] Emotes = ["cheers", "thanks", "laugh"];

    private Label _title = null!;
    private Label _detail = null!;
    private VisitView? _shown;

    public event Action<string>? EmoteRequested;

    public event Action? LeaveRequested;

    public override void _Ready()
    {
        Visible = false;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddThemeConstantOverride("separation", 4);
        VBoxContainer text = new() { Alignment = BoxContainer.AlignmentMode.Center, CustomMinimumSize = new Vector2(110, 0) };
        text.AddThemeConstantOverride("separation", 0);
        _title = BarLabels.Create(12, BarPalette.Text);
        _detail = BarLabels.Create(11, BarPalette.Gold);
        text.AddChild(_title);
        text.AddChild(_detail);
        AddChild(text);
        foreach (string emote in Emotes)
        {
            Button button = new()
            {
                Text = VisitDesk.Text(emote),
                FocusMode = FocusModeEnum.None,
                MouseDefaultCursorShape = CursorShape.PointingHand,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                TooltipText = "Envoyer à ton hôte",
            };
            button.AddThemeFontSizeOverride("font_size", PixelFont.Size(12));
            ClickBinding.OnLeftPress(button, () => EmoteRequested?.Invoke(emote));
            AddChild(button);
        }

        Button leave = new()
        {
            Text = "Rentrer",
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = "Quitter cette taverne et rentrer dans la tienne",
        };
        leave.AddThemeFontSizeOverride("font_size", PixelFont.Size(12));
        ClickBinding.OnLeftPress(leave, () => LeaveRequested?.Invoke());
        AddChild(leave);
    }

    public void Display(VisitView? visit)
    {
        Visible = visit is not null;
        if (visit is null || visit == _shown)
        {
            return;
        }

        _shown = visit;
        _title.Text = visit.Title;
        _detail.Text = visit.Detail;
    }
}
