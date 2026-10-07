using System;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class ExpandedBar : MarginContainer
{
    private const double CoinCatchUp = 5;
    private const float GlowSeconds = 1.2f;
    private const float PurseBeat = 4f;
    private const int PurseIconSize = 14;

    private static readonly Color Glow = new(1.6f, 1.5f, 1.2f);

    private Label _coins = null!;
    private double _shownCoins = double.NaN;
    private double _targetCoins;
    private float _glow;
    private HBoxContainer _purse = null!;
    private Label _purseAmount = null!;
    private bool _purseFull;
    private bool _purseReady;
    private bool _purseHovered;
    private float _time;
    private Label _situation = null!;
    private TownSlot _news = null!;
    private TownSlot _update = null!;
    private TownSlot _slot = null!;

    public event Action? ToggleRequested;

    public event Action? QuitRequested;

    public event Action? ActionRequested;

    public event Action? SlotRequested;

    public event Action? NewsRequested;

    public event Action? SettingsRequested;

    public event Action? PurseRequested;

    public event Action? UpdateRequested;

    public event Action<TownTab>? BuildingRequested;

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
        HBoxContainer coinsRow = new();
        coinsRow.AddThemeConstantOverride("separation", 6);
        coinsRow.AddChild(_coins);
        _purse = CreatePurse();
        coinsRow.AddChild(_purse);
        stats.AddChild(coinsRow);
        stats.AddChild(_situation);
        row.AddChild(stats);

        Lane = new RoadLane { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        Lane.Pressed += () => ActionRequested?.Invoke();
        Lane.BuildingPressed += building => BuildingRequested?.Invoke(StreetNames.TabOf(building, Lane.Scene));
        row.AddChild(Lane);

        _news = new TownSlot();
        _news.Button.Visible = false;
        ClickBinding.OnLeftPress(_news.Button, () => NewsRequested?.Invoke());
        row.AddChild(_news.Button);

        _update = new TownSlot();
        _update.Button.Visible = false;
        ClickBinding.OnLeftPress(_update.Button, () => UpdateRequested?.Invoke());
        row.AddChild(_update.Button);

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
        _time += (float)delta;
        _purse.Modulate = _purseFull ? Colors.White.Lerp(Glow, 0.5f + 0.5f * MathF.Sin(_time * PurseBeat)) : Colors.White;
        _purseAmount.AddThemeColorOverride("font_color", !_purseReady ? BarPalette.Muted : _purseHovered ? BarPalette.Text : BarPalette.Gold);
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
            $"La version {version} d'IdleBar est disponible. Clique pour ouvrir la page de téléchargement, puis remplace ton jeu actuel : ta progression est gardée sur le serveur."));
        _update.Button.Visible = true;
    }

    public void Refresh(BarStatus status)
    {
        RefreshCoins(status);
        RefreshPurse(status.Purse);
        _situation.Text = status.Situation;
        _slot.Refresh(status.Slot);
        _news.Button.Visible = status.News is not null;
        if (status.News is SlotContent news)
        {
            _news.Refresh(news);
        }

        Lane.SetScene(status.Scene);
    }

    private void RefreshPurse(PurseView? purse)
    {
        _purse.Visible = purse is not null;
        if (purse is null)
        {
            _purseFull = false;
            return;
        }

        _purseAmount.Text = NumberFormat.Amount(purse.Amount);
        _purse.TooltipText = purse.Tooltip;
        _purseReady = purse.Amount >= 1;
        _purseFull = purse.Full;
    }

    private HBoxContainer CreatePurse()
    {
        HBoxContainer purse = new()
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        purse.AddThemeConstantOverride("separation", 3);
        purse.AddChild(new TextureRect
        {
            Texture = IconSprites.Pouch.Texture,
            CustomMinimumSize = new Vector2(PurseIconSize, PurseIconSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });
        _purseAmount = BarLabels.Create(12, BarPalette.Gold);
        _purseAmount.MouseFilter = MouseFilterEnum.Ignore;
        _purseAmount.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        purse.AddChild(_purseAmount);
        purse.MouseEntered += () => _purseHovered = true;
        purse.MouseExited += () => _purseHovered = false;
        ClickBinding.OnLeftPress(purse, () =>
        {
            if (_purseReady)
            {
                PurseRequested?.Invoke();
            }
        });
        return purse;
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
