using System.Collections.Generic;

namespace IdleBar.Game;

public sealed record SaveData(
    double Gold,
    double TotalEarned,
    IReadOnlyDictionary<string, int> Owned,
    double SavedAtUnixSeconds,
    bool Collapsed,
    long? CloudRevision)
{
    public static SaveData From(ProgressData progress, double savedAtUnixSeconds, bool collapsed, long? cloudRevision) =>
        new(progress.Gold, progress.TotalEarned, progress.Owned, savedAtUnixSeconds, collapsed, cloudRevision);

    public ProgressData ToProgress() => new(Gold, TotalEarned, Owned);
}
