using System.Collections.Generic;

namespace IdleBar.Pixel;

public static class Costumes
{
    private const int CacheLimit = 256;

    private static readonly Dictionary<(int Seed, Festival Festival), PatronLook> Cache = [];

    public static PatronLook? For(int seed, Festival festival)
    {
        if (festival == Festival.None || seed % 3 != 0)
        {
            return null;
        }

        if (Cache.TryGetValue((seed, festival), out PatronLook? look))
        {
            return look;
        }

        if (Cache.Count >= CacheLimit)
        {
            Cache.Clear();
        }

        int head = festival == Festival.Christmas ? PatronSprites.Cap : seed % 2 == 0 ? PatronSprites.PumpkinHead : PatronSprites.Witch;
        look = PatronLook.From(seed) with { Head = head };
        Cache[(seed, festival)] = look;
        return look;
    }
}
