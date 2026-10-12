using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed class EventLog
{
    private const int Capacity = 400;

    private readonly List<FrontierEvent> _events = [];

    public int Count => _events.Count;

    public void Add(FrontierEvent frontierEvent)
    {
        if (_events.Count >= Capacity)
        {
            _events.RemoveAt(0);
        }

        _events.Add(frontierEvent);
    }

    public IReadOnlyList<FrontierEvent> Drain()
    {
        if (_events.Count == 0)
        {
            return [];
        }

        List<FrontierEvent> drained = [.. _events];
        _events.Clear();
        return drained;
    }
}
