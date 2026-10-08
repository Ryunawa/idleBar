using System;

namespace IdleBar.Pixel;

public static class Weather
{
    private const int BlockHours = 3;
    private const int RainPercent = 25;

    public static bool Raining(DateTime local) => Hash(local.Year * 10000 + local.DayOfYear * 10 + local.Hour / BlockHours) % 100 < RainPercent;

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
