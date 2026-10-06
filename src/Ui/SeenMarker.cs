using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed class SeenMarker
{
    private readonly GameSession _session;
    private readonly GameActions _actions;
    private readonly HashSet<string> _marking = [];

    public SeenMarker(GameSession session, GameActions actions)
    {
        _session = session;
        _actions = actions;
    }

    public void MarkExchanges() =>
        Mark("échanges", _session.Snapshot?.News.Total > 0, _actions.MarkExchangesSeenAsync);

    public void MarkJournal() =>
        Mark("journal", _session.Snapshot?.JournalUnseen > 0, _actions.MarkJournalSeenAsync);

    public void MarkTab(TownTab tab)
    {
        if (tab is TownTab.Counter or TownTab.Contracts)
        {
            MarkExchanges();
        }
        else if (tab == TownTab.Journal)
        {
            MarkJournal();
        }
    }

    private async void Mark(string what, bool pending, Func<Task> action)
    {
        if (!pending || !_marking.Add(what))
        {
            return;
        }

        string? error = await ActionFeedback.CaptureAsync(action);
        _marking.Remove(what);
        if (error is not null)
        {
            GD.PushWarning($"Nouvelles ({what}) non marquées comme lues : {error}");
        }
    }
}
