using System;
using Godot;

namespace IdleBar.Ui;

public partial class ExpandedBar : MarginContainer
{
    private Label _coins = null!;
    private Label _situation = null!;
    private TownSlot _news = null!;
    private TownSlot _slot = null!;

    public event Action? ToggleRequested;

    public event Action? QuitRequested;

    public event Action? ActionRequested;

    public event Action? SlotRequested;

    public event Action? NewsRequested;

    public event Action? SettingsRequested;

    public RoadLane Lane { get; private set; } = null!;

    public override void _Ready()
    {
        AddThemeConstantOverride("margin_left", 10);
        AddThemeConstantOverride("margin_right", 10);
        AddThemeConstantOverride("margin_top", 5);
        AddThemeConstantOverride("margin_bottom", 5);

        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        AddChild(row);

        VBoxContainer stats = new() { CustomMinimumSize = new Vector2(170, 0), Alignment = BoxContainer.AlignmentMode.Center };
        stats.AddThemeConstantOverride("separation", 0);
        _coins = BarLabels.Create(19, BarPalette.Gold);
        _coins.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _situation = BarLabels.Create(11, BarPalette.Muted);
        _situation.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        stats.AddChild(_coins);
        stats.AddChild(_situation);
        row.AddChild(stats);

        Lane = new RoadLane { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        Lane.Pressed += () => ActionRequested?.Invoke();
        row.AddChild(Lane);

        _news = new TownSlot();
        _news.Button.Visible = false;
        ClickBinding.OnLeftPress(_news.Button, () => NewsRequested?.Invoke());
        row.AddChild(_news.Button);

        _slot = new TownSlot();
        ClickBinding.OnLeftPress(_slot.Button, () => SlotRequested?.Invoke());
        row.AddChild(_slot.Button);

        row.AddChild(WindowButtons.CreateSettings(() => SettingsRequested?.Invoke()));

        VBoxContainer windowButtons = new() { Alignment = BoxContainer.AlignmentMode.Center };
        WindowButtons.Add(windowButtons, "–", () => ToggleRequested?.Invoke(), () => QuitRequested?.Invoke());
        row.AddChild(windowButtons);
    }

    public void Refresh(BarStatus status)
    {
        _coins.Text = status.Coins;
        _situation.Text = status.Situation;
        _slot.Refresh(status.Slot);
        _news.Button.Visible = status.News is not null;
        if (status.News is SlotContent news)
        {
            _news.Refresh(news);
        }

        Lane.SetScene(status.Scene);
    }
}
