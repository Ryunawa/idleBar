using System;
using Godot;

namespace IdleBar.Ui;

public partial class VisitSlot : VBoxContainer
{
    private Label _title = null!;
    private Label _detail = null!;
    private VisitView? _shown;

    public event Action? MenuRequested;

    public override void _Ready()
    {
        Visible = false;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        Alignment = AlignmentMode.Center;
        CustomMinimumSize = new Vector2(110, 0);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        TooltipText = "Trinquer, commander ou rentrer chez toi";
        AddThemeConstantOverride("separation", 0);
        _title = BarLabels.Create(12, BarPalette.Text);
        _detail = BarLabels.Create(11, BarPalette.Gold);
        _title.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _detail.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        AddChild(_title);
        AddChild(_detail);
        ClickBinding.OnLeftPress(this, () => MenuRequested?.Invoke());
    }

    public void Display(VisitView? visit)
    {
        Visible = visit is not null;
        if (visit is null || visit == _shown)
        {
            return;
        }

        _shown = visit;
        _title.Text = $"{visit.Title} ▾";
        _detail.Text = visit.Detail;
    }
}
