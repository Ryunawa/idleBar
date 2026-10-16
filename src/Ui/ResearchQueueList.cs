using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class ResearchQueueList : VBoxContainer
{
    private readonly List<ResearchQueueRow> _rows = [];
    private Label _title = null!;
    private Label _rule = null!;

    public event Action<ResearchKind, int, int>? Moved;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 2);
        _title = WindowRows.Muted(string.Empty);
        AddChild(_title);
        _rule = WindowRows.Muted("Chef de la guilde, tu peux changer l'ordre de la file avec ▲ et ▼ : la recherche en cours garde sa place, et deux niveaux d'une même recherche gardent leur ordre.");
        AddChild(_rule);
    }

    public void Refresh(ResearchSnapshot snapshot)
    {
        IReadOnlyList<QueuedResearch> waiting = snapshot.Member ? snapshot.Research?.Waiting ?? [] : [];
        while (_rows.Count < waiting.Count)
        {
            ResearchQueueRow row = new();
            row.Moved += (kind, level, offset) => Moved?.Invoke(kind, level, offset);
            AddChild(row);
            _rows.Add(row);
            MoveChild(_rule, GetChildCount() - 1);
        }

        for (int index = 0; index < _rows.Count; index++)
        {
            _rows[index].Visible = index < waiting.Count;
            if (index < waiting.Count)
            {
                _rows[index].Refresh(snapshot, waiting, index);
            }
        }

        _title.Text = waiting.Count == 0 ? "File d'attente vide." : "File d'attente :";
        _rule.Visible = snapshot.Leader && waiting.Count > 1;
    }
}
