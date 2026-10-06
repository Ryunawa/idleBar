using System;

namespace IdleBar.Trade;

public sealed record WorkshopState(
    string TownId,
    int Level,
    double Speed,
    int StorageCapacity,
    int MaxQueue,
    int? NextLevelPrice,
    string? RecipeId,
    int Queued,
    int? BatchSeconds,
    DateTimeOffset? StartedAt,
    DateTimeOffset? PausedUntil)
{
    public bool IsProducing => Queued > 0 && BatchSeconds is > 0 && StartedAt is not null;

    public bool IsPaused(DateTimeOffset now) => IsProducing && PausedUntil > now;

    public DateTimeOffset? NextBatchAt => IsProducing ? StartedAt!.Value.AddSeconds(BatchSeconds!.Value) : null;

    public DateTimeOffset? FinishesAt => IsProducing ? StartedAt!.Value.AddSeconds((double)BatchSeconds!.Value * Queued) : null;

    public int RemainingBatches(DateTimeOffset now) => IsProducing ? Queued - BatchesDone(now) : 0;

    public double BatchProgress(DateTimeOffset now)
    {
        if (!IsProducing)
        {
            return 0;
        }

        DateTimeOffset moment = IsPaused(now) ? PausedUntil!.Value : now;
        double elapsed = Math.Max(0, (moment - StartedAt!.Value).TotalSeconds);
        return BatchesDone(moment) >= Queued ? 1 : elapsed % BatchSeconds!.Value / BatchSeconds.Value;
    }

    private int BatchesDone(DateTimeOffset now)
    {
        double elapsed = Math.Max(0, (now - StartedAt!.Value).TotalSeconds);
        return Math.Min(Queued, (int)Math.Floor(elapsed / BatchSeconds!.Value));
    }
}
