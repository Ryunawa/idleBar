using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class ResearchNodeCard : PanelContainer
{
    private const int Bundle = 10;

    private static readonly Color Faded = new(1, 1, 1, 0.55f);

    private Label _title = null!;
    private Label _effect = null!;
    private Label _funding = null!;
    private Label _state = null!;
    private HBoxContainer _buttons = null!;
    private Button _one = null!;
    private Button _ten = null!;
    private Button _all = null!;
    private int _bundle;
    private int _missing;

    public ResearchNodeCard(ResearchKind kind)
    {
        Kind = kind;
    }

    public event Action<ResearchKind, int>? Funded;

    public ResearchKind Kind { get; }

    public override void _Ready()
    {
        ThemeTypeVariation = WindowSkin.Card;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 2);
        _title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        rows.AddChild(_title);
        _effect = WindowRows.Muted(string.Empty);
        rows.AddChild(_effect);
        _funding = WindowRows.Muted(string.Empty);
        rows.AddChild(_funding);
        _state = WindowRows.Muted(string.Empty);
        rows.AddChild(_state);
        _buttons = new HBoxContainer();
        _buttons.AddThemeConstantOverride("separation", 4);
        _one = Give("+1", () => 1);
        _ten = Give($"+{Bundle}", () => _bundle);
        _all = Give(string.Empty, () => _missing);
        _all.TooltipText = "Verse ce qu'il manque à ce niveau, dans la limite de tes sceaux.";
        rows.AddChild(_buttons);
        AddChild(rows);
    }

    public void Refresh(ResearchSnapshot snapshot)
    {
        ResearchInfo info = Researches.Of(Kind);
        ResearchNodeData? node = snapshot.Node(Kind);
        int level = node?.Level ?? 0;
        int next = level + (node?.Queued ?? 0) + 1;
        bool full = next > info.MaxLevel;
        string? locked = Researches.Prerequisite(Kind) is ResearchKind before && snapshot.Level(before) < 1 ? Researches.Of(before).Name : null;
        _title.Text = $"{info.Name} · niveau {level}/{info.MaxLevel}";
        _title.AddThemeColorOverride("font_color", level > 0 ? BarPalette.Gold : BarPalette.Text);
        string now = level > 0 ? $"Actuel : {ResearchText.Effect(Kind, level)}" : "Pas encore recherché";
        _effect.Text = now + (level < info.MaxLevel ? $" · niveau {level + 1} : {ResearchText.Effect(Kind, level + 1)}." : " · maximum.");
        int cost = node?.Cost ?? Researches.Cost(Kind, next, Math.Max(1, snapshot.Members));
        double hours = node?.Hours ?? Researches.Hours(Kind, next);
        string frozen = node?.Frozen == true ? " (prix fixé)" : string.Empty;
        _funding.Text = full
            ? "Tous les niveaux sont financés."
            : $"Niveau {next} : {node?.Funded ?? 0} / {ResearchSnapshot.Seal(cost)}{frozen} · {ResearchText.Hours(hours)} de recherche";
        _funding.Visible = level < info.MaxLevel;
        _state.Text = State(snapshot, info, level, locked);
        _missing = Math.Min(node?.Missing ?? 0, snapshot.Seals);
        _bundle = Math.Min(Bundle, node?.Missing ?? 0);
        _buttons.Visible = snapshot.Member && locked is null && !full;
        _one.Disabled = snapshot.Seals < 1;
        _ten.Visible = _bundle > 1 && _bundle != _missing;
        _ten.Disabled = snapshot.Seals < _bundle;
        _ten.Text = $"+{_bundle}";
        _all.Disabled = _missing < 1;
        _all.Text = $"Tout ({_missing})";
        Modulate = locked is not null ? Faded : Colors.White;
    }

    private string State(ResearchSnapshot snapshot, ResearchInfo info, int level, string? locked)
    {
        if (locked is not null)
        {
            return $"Verrouillé : demande « {locked} » au niveau 1.";
        }

        if (snapshot.Running is QueuedResearch running && running.Kind == Kind)
        {
            return $"En recherche : niveau {running.Level}, fin dans {GuildMessages.Wait(snapshot.Left(running.EndsIn))}.";
        }

        int position = snapshot.Member ? snapshot.Research?.Position(Kind) ?? 0 : 0;
        if (position > 0)
        {
            QueuedResearch entry = snapshot.Research!.Waiting[position - 1];
            return $"En attente : niveau {entry.Level}, {(position == 1 ? "1er" : $"{position}e")} dans la file.";
        }

        return level >= info.MaxLevel ? "Terminé." : snapshot.Member ? "À financer." : "À financer par ta guilde.";
    }

    private Button Give(string text, Func<int> amount)
    {
        Button button = new() { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.Pressed += () => Funded?.Invoke(Kind, amount());
        _buttons.AddChild(button);
        return button;
    }
}
