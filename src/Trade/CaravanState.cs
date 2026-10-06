using System;

namespace IdleBar.Trade;

public sealed record CaravanState(
    int Wagons,
    int Capacity,
    int Load,
    int? NextWagonPrice,
    string TownId,
    string? FromTownId,
    DateTimeOffset? DepartedAt,
    DateTimeOffset? ArrivesAt,
    string DirectiveId)
{
    public bool IsTravelling(DateTimeOffset now) => ArrivesAt is DateTimeOffset arrival && arrival > now;

    public TimeSpan Remaining(DateTimeOffset now) =>
        ArrivesAt is DateTimeOffset arrival && arrival > now ? arrival - now : TimeSpan.Zero;

    public double Progress(DateTimeOffset now)
    {
        if (DepartedAt is not DateTimeOffset departure || ArrivesAt is not DateTimeOffset arrival || arrival <= departure)
        {
            return 1;
        }

        return Math.Clamp((now - departure).TotalSeconds / (arrival - departure).TotalSeconds, 0, 1);
    }
}
