using System;

namespace IdleBar.Pixel;

public static class Weather
{
    private const int BlockHours = 3;

    public static Precipitation At(DateTime local)
    {
        Season season = Calendar.SeasonOf(local);
        int chance = season switch
        {
            Season.Autumn => 35,
            Season.Winter => 30,
            Season.Spring => 25,
            _ => 10,
        };
        if (Hash(local.Year * 10000 + local.DayOfYear * 10 + local.Hour / BlockHours) % 100 >= chance)
        {
            return Precipitation.None;
        }

        return season == Season.Winter ? Precipitation.Snow : Precipitation.Rain;
    }

    public static bool Wet(DateTime local) => At(local) != Precipitation.None;

    private static int Hash(int value)
    {
        uint mixed = unchecked((uint)value);
        mixed ^= mixed >> 16;
        mixed = unchecked(mixed * 0x7feb352d);
        mixed ^= mixed >> 15;
        mixed = unchecked(mixed * 0x846ca68b);
        mixed ^= mixed >> 16;
        return (int)(mixed & 0x7fffffff);
    }
}
