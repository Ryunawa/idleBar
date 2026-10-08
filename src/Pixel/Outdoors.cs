using System;

namespace IdleBar.Pixel;

public readonly record struct Outdoors(SkyLight Sky, bool Raining)
{
    public static Outdoors At(DateTime local) => new(SkyLight.At(local), Weather.Raining(local));
}
