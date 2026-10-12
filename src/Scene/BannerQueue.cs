using System.Collections.Generic;

namespace IdleBar.Scene;

public sealed class BannerQueue
{
    private const int Capacity = 6;

    private readonly Queue<Banner> _waiting = new();

    public Banner? Current { get; private set; }

    public void Add(IReadOnlyList<string> lines, float duration = 3.4f)
    {
        Banner banner = new(lines, duration);
        if (Current is null)
        {
            Current = banner;
            return;
        }

        if (_waiting.Count < Capacity)
        {
            _waiting.Enqueue(banner);
        }
    }

    public void Update(float dt)
    {
        if (Current is null)
        {
            return;
        }

        Current.Time += dt;
        if (Current.Time >= Current.Duration)
        {
            Current = _waiting.Count > 0 ? _waiting.Dequeue() : null;
        }
    }
}
