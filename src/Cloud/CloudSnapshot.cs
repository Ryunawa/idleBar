using System;
using IdleBar.Game;

namespace IdleBar.Cloud;

public sealed record CloudSnapshot(
    ProgressData Progress,
    long Revision,
    string ActiveDevice,
    DateTimeOffset UpdatedAt,
    DateTimeOffset ServerTime)
{
    public double SecondsSinceUpdate => Math.Max(0, (ServerTime - UpdatedAt).TotalSeconds);
}
