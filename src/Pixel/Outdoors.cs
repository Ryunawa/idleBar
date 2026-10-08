using System;

namespace IdleBar.Pixel;

public readonly record struct Outdoors(SkyLight Sky, Precipitation Weather, Season Season, Festival Festival)
{
    public bool Raining => Weather != Precipitation.None;

    public static Outdoors At(DateTime local) =>
        new(SkyLight.At(local), Pixel.Weather.At(local), Calendar.SeasonOf(local), Calendar.FestivalOf(local));
}
