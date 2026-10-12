using System;
using Godot;

namespace IdleBar.Ui;

public partial class ExpandedBar : MarginContainer
{
    private const double GoldCatchUp = 5;
    private const float GlowSeconds = 1.2f;
    private const int StatsWidth = 190;
    private const int SituationLines = 2;
    private const int SituationLineSpacing = -3;

    private static readonly Color GlowColor = new(1.6f, 1.5f, 1.2f);

    private Label _gold = null!;
    private Label _situation = null!;
    private double _shownGold = double.NaN;
    private double _targetGold;
    private float _glow;
    private BarSlot _update = null!;
    private Color _situationColor = BarPalette.Muted;

    public event Action? ToggleRequested;

    public event Action? QuitRequested;

    public event Action? SettingsRequested;

    public event Action? UpdateRequested;

    public event Action? MenuRequested;

    public FrontierLane Lane { get; private set; } = null!;

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
            TooltipText = "Ouvrir la gestion : combattants, village, repli",
        };
        stats.AddThemeConstantOverride("separation", 0);
        ClickBinding.OnLeftPress(stats, () => MenuRequested?.Invoke());
        _gold = BarLabels.Create(19, BarPalette.Gold);
        _gold.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _situation = BarLabels.Create(11, BarPalette.Muted);
        _situation.CustomMinimumSize = new Vector2(StatsWidth, 0);
        _situation.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _situation.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _situation.MaxLinesVisible = SituationLines;
        _situation.AddThemeConstantOverride("line_spacing", SituationLineSpacing);
        stats.AddChild(_gold);
        stats.AddChild(_situation);
        row.AddChild(stats);

        Lane = new FrontierLane { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(Lane);

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

    public override void _Process(double delta)
    {
        if (double.IsNaN(_shownGold))
        {
            return;
        }

        double gap = _targetGold - _shownGold;
        _shownGold = Math.Abs(gap) < 1 ? _targetGold : _shownGold + gap * Math.Min(1, delta * GoldCatchUp);
        _gold.Text = NumberFormat.Gold(_shownGold);
        _glow = Math.Max(0, _glow - (float)delta / GlowSeconds);
        _gold.Modulate = Colors.White.Lerp(GlowColor, _glow);
    }

    public void Glow() => _glow = 1;

    public void ShowUpdate(string version)
    {
        _update.Refresh(new SlotContent(
            "Mise à jour",
            $"version {version}",
            BarPalette.Gold,
            $"La version {version} d'IdleBar est disponible. Clique pour ouvrir la page de téléchargement, puis remplace ton jeu actuel : ta partie est gardée sur le serveur."));
        _update.Button.Visible = true;
    }

    public void Refresh(BarStatus status)
    {
        string? hint = Lane.Hint;
        _situation.Text = hint ?? status.Situation;
        Color situationColor = hint is not null ? BarPalette.Text : status.Alert ? BarPalette.Danger : BarPalette.Muted;
        if (situationColor != _situationColor)
        {
            _situationColor = situationColor;
            _situation.AddThemeColorOverride("font_color", situationColor);
        }

        if (status.Gold is not double gold)
        {
            _shownGold = double.NaN;
            _gold.Text = status.Headline;
            _gold.Modulate = Colors.White;
            return;
        }

        if (double.IsNaN(_shownGold))
        {
            _shownGold = gold;
            _gold.Text = NumberFormat.Gold(gold);
        }

        _targetGold = gold;
    }
}
