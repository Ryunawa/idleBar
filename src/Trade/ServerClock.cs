using System;
using Godot;

namespace IdleBar.Trade;

public sealed class ServerClock
{
    private double _offsetSeconds;

    public DateTimeOffset Now =>
        DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Round((Time.GetUnixTimeFromSystem() + _offsetSeconds) * 1000));

    public void Synchronize(DateTimeOffset serverTime) =>
        _offsetSeconds = serverTime.ToUnixTimeMilliseconds() / 1000.0 - Time.GetUnixTimeFromSystem();
}
