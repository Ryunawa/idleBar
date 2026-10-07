using Godot;

namespace IdleBar.Ui;

public partial class WindowTitleBar : HBoxContainer
{
    public const int Height = 26;

    private static readonly Vector2 CloseSize = new(30, 24);

    private Label _title = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, Height);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.Move;

        _title = BarLabels.Create(13, BarPalette.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        AddChild(_title);

        Button close = new()
        {
            Text = "×",
            TooltipText = "Fermer",
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = CloseSize,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        close.Pressed += () => GetWindow().EmitSignal(Window.SignalName.CloseRequested);
        AddChild(close);
    }

    public override void _Process(double delta)
    {
        string title = GetWindow().Title;
        if (_title.Text != title)
        {
            _title.Text = title;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AcceptEvent();
            GetWindow().StartDrag();
        }
    }
}