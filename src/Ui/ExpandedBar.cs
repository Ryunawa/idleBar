using System;
using Godot;

namespace IdleBar.Ui;

public partial class ExpandedBar : MarginContainer
{
    private const double CoinCatchUp = 5;
    private const float GlowSeconds = 1.2f;

    private static readonly Color Glow = new(1.6f, 1.5f, 1.2f);

    private Label _coins = null!;
    private double _shownCoins = double.NaN;
    private double _targetCoins;
    private float _glow;
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

    public override void _Process(double delta)
    {
        if (double.IsNaN(_shownCoins))
        {
            return;
        }

        double gap = _targetCoins - _shownCoins;
        _shownCoins = Math.Abs(gap) < 1 ? _targetCoins : _shownCoins + gap * Math.Min(1, delta * CoinCatchUp);
        _coins.Text = NumberFormat.Coins(_shownCoins);
        _glow = Math.Max(0, _glow - (float)delta / GlowSeconds);
        _coins.Modulate = Colors.White.Lerp(Glow, _glow);
    }

    public void Refresh(BarStatus status)
    {
        RefreshCoins(status);
        _situation.Text = status.Situation;
        _slot.Refresh(status.Slot);
        _news.Button.Visible = status.News is not null;
        if (status.News is SlotContent news)
        {
            _news.Refresh(news);
        }

        Lane.SetScene(status.Scene);
    }

    private void RefreshCoins(BarStatus status)
    {
        if (status.CoinValue is not double coins)
        {
            _shownCoins = double.NaN;
            _coins.Text = status.Coins;
            _coins.Modulate = Colors.White;
            return;
        }

        if (double.IsNaN(_shownCoins))
        {
            _shownCoins = coins;
            _coins.Text = status.Coins;
        }
        else if (coins > _targetCoins)
        {
            _glow = 1;
        }

        _targetCoins = coins;
    }
}
