using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class ResearchQueueRow : HBoxContainer
{
    private Label _text = null!;
    private Button _up = null!;
    private Button _down = null!;
    private QueuedResearch? _entry;

    public event Action<ResearchKind, int, int>? Moved;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        _text = WindowRows.Muted(string.Empty);
        _text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_text);
        _up = Arrow("▲", "Avance cette recherche d'une place dans la file.", -1);
        _down = Arrow("▼", "Recule cette recherche d'une place dans la file.", 1);
    }

    public void Refresh(ResearchSnapshot snapshot, IReadOnlyList<QueuedResearch> waiting, int index)
    {
        QueuedResearch entry = waiting[index];
        _entry = entry;
        _text.Text = $"{index + 1}. {ResearchSnapshot.Title(entry)} niveau {entry.Level} ({ResearchText.Hours(entry.Hours)}, fin dans {GuildMessages.Wait(snapshot.Left(entry.EndsIn))})";
        _up.Visible = snapshot.Leader;
        _down.Visible = snapshot.Leader;
        _up.Disabled = entry.Kind is null || index == 0 || waiting[index - 1].Node == entry.Node;
        _down.Disabled = entry.Kind is null || index == waiting.Count - 1 || waiting[index + 1].Node == entry.Node;
    }

    private Button Arrow(string text, string tooltip, int offset)
    {
        Button button = new() { Text = text, TooltipText = tooltip, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.Pressed += () =>
        {
            if (_entry?.Kind is ResearchKind kind)
            {
                Moved?.Invoke(kind, _entry.Level, offset);
            }
        };
        AddChild(button);
        return button;
    }
}
