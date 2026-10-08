using System;
using System.Collections.Generic;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed class Street
{
    private const float WalkSeconds = 14f;
    private const float MinPause = 8f;
    private const float MaxPause = 25f;

    private readonly Random _random = new();
    private IReadOnlyList<PasserbyData> _passersby = [];
    private int _next;
    private float _pause = MinPause / 2;

    public StreetWalk? Current { get; private set; }

    public void Sync(IReadOnlyList<PasserbyData> passersby) => _passersby = passersby;

    public void Update(float delta, IReadOnlyList<int> windows)
    {
        if (Current is StreetWalk walk)
        {
            float progress = walk.Progress + delta / WalkSeconds;
            Current = progress >= 1 ? null : walk with { Progress = progress };
            return;
        }

        _pause -= delta;
        if (_pause > 0 || _passersby.Count == 0 || windows.Count == 0)
        {
            return;
        }

        _pause = MinPause + (float)_random.NextDouble() * (MaxPause - MinPause);
        PasserbyData passerby = _passersby[_next++ % _passersby.Count];
        Current = new StreetWalk(passerby, windows[_random.Next(windows.Count)], 0);
    }
}
