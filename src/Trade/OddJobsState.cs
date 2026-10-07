using System;

namespace IdleBar.Trade;

public sealed record OddJobsState(DateTimeOffset Since, int Hourly, double CapHours, int Earned)
{
    public DateTimeOffset FullAt => Since + TimeSpan.FromHours(CapHours);

    public int Capacity => (int)Math.Floor(CapHours * Hourly);

    public int EarnedAt(DateTimeOffset now) => (int)Math.Floor(Math.Clamp((now - Since).TotalHours, 0, CapHours) * Hourly);

    public bool IsFull(DateTimeOffset now) => now >= FullAt;
}