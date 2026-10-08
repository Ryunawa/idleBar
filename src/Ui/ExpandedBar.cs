using System;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Ui;

public partial class ExpandedBar : MarginContainer
{
    private const double CoinCatchUp = 5;
    private const float GlowSeconds = 1.2f;
    private const int StatsWidth = 190;

    private static readonly Color Glow = new(1.6f, 1.5f, 1.2f);

    private Label _coins = null!;
    private Label _situation = null!;
    private double _shownCoins = double.NaN;
    private double _targetCoins;
    private float _glow;
    private BarSlot _update = null!;
    private TavernLane _lane = null!;

    public event Action? ToggleRequested;

    public event Action? QuitRequested;

    public event Action? SettingsRequested;

    public event Action? UpdateRequested;

    public override void _Ready()
    {
        AddThemeConstantOverride("margin_left", 10);
        AddThemeConstantOverride("margin_right", 10);
        AddThemeConstantOverride("margin_top", 0);
        AddThemeConstantOverride("margin_bottom", 0);

        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        AddChild(row);

        VBoxContainer stats = new() { CustomMinimumSize = new Vector2(StatsWidth, 0), Alignment = BoxContainer.AlignmentMode.Center };
        stats.AddThemeConstantOverride("separation", 0);
        _coins = BarLabels.Create(19, BarPalette.Gold);
        _coins.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _situation = BarLabels.Create(11, BarPalette.Muted);
        _situation.CustomMinimumSize = new Vector2(StatsWidth, 0);
        stats.AddChild(_coins);
        stats.AddChild(_situation);
        row.AddChild(stats);

        _lane = new TavernLane { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(_lane);

        _update = new BarSlot();
        _update.Button.Visible = false;
        _update.Button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        ClickBinding.OnLeftPress(_update.Button, () => UpdateRequested?.Invoke());
        row.AddChild(_update.Button);

        row.AddChild(WindowButtons.CreateSettings(() => SettingsRequested?.Invoke()));

        VBoxContainer windowButtons = new() { Alignment = BoxContainer.AlignmentMode.Center };
        WindowButtons.Add(windowButtons, "–", () => ToggleRequested?.Invoke(), () => QuitRequested?.Invoke());
        row.AddChild(windowButtons);
    }

    public void Attach(Tavern tavern) => _lane.Attach(tavern);

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

    public void ShowUpdate(string version)
    {
        _update.Refresh(new SlotContent(
            "Mise à jour",
            $"version {version}",
            BarPalette.Gold,
            $"La version {version} d'IdleBar est disponible. Clique pour ouvrir la page de téléchargement, puis remplace ton jeu actuel."));
        _update.Button.Visible = true;
    }

    public void Refresh(BarStatus status)
    {
        string? hint = _lane.Hint;
        _situation.Text = hint ?? status.Situation;
        _situation.AddThemeColorOverride("font_color", hint is null ? BarPalette.Muted : BarPalette.Text);
        if (double.IsNaN(_shownCoins))
        {
            _shownCoins = status.Coins;
            _coins.Text = NumberFormat.Coins(status.Coins);
        }
        else if (status.Coins > _targetCoins)
        {
            _glow = 1;
        }

        _targetCoins = status.Coins;
    }
}
