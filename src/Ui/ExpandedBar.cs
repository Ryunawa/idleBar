using System;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Ui;

public partial class ExpandedBar : MarginContainer
{
    private const double CoinCatchUp = 5;
    private const float GlowSeconds = 1.2f;
    private const float JarBeat = 4f;
    private const int StatsWidth = 190;
    private const int SituationLines = 2;
    private const int SituationLineSpacing = -3;

    private static readonly Color Glow = new(1.6f, 1.5f, 1.2f);

    private Label _coins = null!;
    private Label _situation = null!;
    private double _shownCoins = double.NaN;
    private double _targetCoins;
    private float _glow;
    private float _time;
    private TipJarButton _jar = null!;
    private VisitSlot _visit = null!;
    private BarSlot _update = null!;
    private Button _chat = null!;
    private Color _situationColor = BarPalette.Muted;

    public event Action? ToggleRequested;

    public event Action? QuitRequested;

    public event Action? SettingsRequested;

    public event Action? UpdateRequested;

    public event Action? MenuRequested;

    public event Action? TipJarRequested;

    public event Action<string>? EmoteRequested;

    public event Action<Vector2I>? ChatRequested;

    public event Action? LeaveRequested;

    public TavernLane Lane { get; private set; } = null!;

    public override void _Ready()
    {
        AddThemeConstantOverride("margin_left", 10);
        AddThemeConstantOverride("margin_right", 10);
        AddThemeConstantOverride("margin_top", 0);
        AddThemeConstantOverride("margin_bottom", 0);

        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        AddChild(row);

        VBoxContainer stats = new()
        {
            CustomMinimumSize = new Vector2(StatsWidth, 0),
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            TooltipText = "Ouvrir la taverne : améliorations, objectifs du jour",
        };
        stats.AddThemeConstantOverride("separation", 0);
        ClickBinding.OnLeftPress(stats, () => MenuRequested?.Invoke());
        HBoxContainer coinsRow = new();
        coinsRow.AddThemeConstantOverride("separation", 8);
        _coins = BarLabels.Create(19, BarPalette.Gold);
        _coins.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        coinsRow.AddChild(_coins);
        _jar = new TipJarButton();
        _jar.Pressed += () => TipJarRequested?.Invoke();
        coinsRow.AddChild(_jar);
        _situation = BarLabels.Create(11, BarPalette.Muted);
        _situation.CustomMinimumSize = new Vector2(StatsWidth, 0);
        _situation.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _situation.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _situation.MaxLinesVisible = SituationLines;
        _situation.AddThemeConstantOverride("line_spacing", SituationLineSpacing);
        stats.AddChild(coinsRow);
        stats.AddChild(_situation);
        row.AddChild(stats);

        Lane = new TavernLane { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(Lane);

        _chat = new Button
        {
            Text = "Parler",
            Visible = false,
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = "Écrire un message à ceux qui sont dans la même taverne que toi",
        };
        _chat.AddThemeFontSizeOverride("font_size", PixelFont.Size(12));
        ClickBinding.OnLeftPress(_chat, () => ChatRequested?.Invoke(ScreenPoint(_chat)));
        row.AddChild(_chat);

        _visit = new VisitSlot();
        _visit.EmoteRequested += emote => EmoteRequested?.Invoke(emote);
        _visit.LeaveRequested += () => LeaveRequested?.Invoke();
        row.AddChild(_visit);

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

    public void Attach(Tavern tavern, bool payments = true) => Lane.Attach(tavern, payments);

    private Vector2I ScreenPoint(Control control) =>
        GetWindow().Position + (Vector2I)((control.GetGlobalPosition() + new Vector2(control.Size.X / 2, 0)) * GetWindow().ContentScaleFactor);

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _jar.Pulse(_time * JarBeat);
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
            $"La version {version} d'IdleBar est disponible. Clique pour ouvrir la page de téléchargement, puis remplace ton jeu actuel : ta taverne est gardée sur le serveur."));
        _update.Button.Visible = true;
    }

    public void Refresh(BarStatus status)
    {
        string? hint = Lane.Hint;
        _situation.Text = hint ?? status.Situation;
        Color situationColor = hint is null ? BarPalette.Muted : BarPalette.Text;
        if (situationColor != _situationColor)
        {
            _situationColor = situationColor;
            _situation.AddThemeColorOverride("font_color", situationColor);
        }
        _jar.Display(status.TipJar);
        _visit.Display(status.Visit);
        _chat.Visible = status.CanChat;
        if (status.Coins is not double coins)
        {
            _shownCoins = double.NaN;
            _coins.Text = status.Headline;
            _coins.Modulate = Colors.White;
            return;
        }

        if (double.IsNaN(_shownCoins))
        {
            _shownCoins = coins;
            _coins.Text = NumberFormat.Coins(coins);
        }
        else if (coins > _targetCoins)
        {
            _glow = 1;
        }

        _targetCoins = coins;
    }
}
