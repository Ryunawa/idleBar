using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IdleBar.Scene;

public static class PicoFilter
{
    private const int CacheLimit = 1 << 16;
    private const int Levels = 16;
    private const int MinMix = 4;

    private static readonly int[] Bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];
    private static readonly ConcurrentDictionary<int, int> Cache = new();

    public static void Apply(ArtRaster raster)
    {
        byte[] data = raster.Data;
        int lastKey = -1;
        int lastPick = 0;
        for (int y = 0; y < raster.Height; y++)
        {
            for (int x = 0; x < raster.Width; x++)
            {
                int index = (y * raster.Width + x) * 4;
                if (data[index + 3] < 128)
                {
                    data[index + 3] = 0;
                    continue;
                }

                int key = data[index] << 16 | data[index + 1] << 8 | data[index + 2];
                int pick = key == lastKey ? lastPick : Lookup(key);
                lastKey = key;
                lastPick = pick;
                bool second = Bayer[(y & 3) * 4 + (x & 3)] < (pick >> 8);
                Rgba color = Palette.Pico[second ? (pick >> 4) & 15 : pick & 15];
                data[index] = color.R;
                data[index + 1] = color.G;
                data[index + 2] = color.B;
                data[index + 3] = 255;
            }
        }
    }

    public static void Shade(ArtRaster raster, float strength, IReadOnlyList<int> ramp)
    {
        int threshold = (int)MathF.Round(Math.Clamp(strength, 0, 1) * Levels);
        if (threshold <= 0)
        {
            return;
        }

        byte[] data = raster.Data;
        for (int y = 0; y < raster.Height; y++)
        {
            for (int x = 0; x < raster.Width; x++)
            {
                int index = (y * raster.Width + x) * 4;
                if (data[index + 3] == 0 || Bayer[(y & 3) * 4 + (x & 3)] >= threshold)
                {
                    continue;
                }

                Rgba color = Palette.Pico[ramp[Lookup(data[index] << 16 | data[index + 1] << 8 | data[index + 2]) & 15]];
                data[index] = color.R;
                data[index + 1] = color.G;
                data[index + 2] = color.B;
            }
        }
    }

    public static Rgba Nearest(Rgba color) => Palette.Pico[Lookup(color.R << 16 | color.G << 8 | color.B) & 15];

    private static int Lookup(int key)
    {
        if (Cache.TryGetValue(key, out int pick))
        {
            return pick;
        }

        if (Cache.Count >= CacheLimit)
        {
            Cache.Clear();
        }

        Rgba color = new((byte)(key >> 16), (byte)(key >> 8), (byte)key);
        int first = 0;
        int second = 0;
        double best = double.MaxValue;
        double next = double.MaxValue;
        for (int index = 0; index < Palette.Pico.Count; index++)
        {
            double distance = Distance(color, Palette.Pico[index]);
            if (distance < best)
            {
                next = best;
                second = first;
                best = distance;
                first = index;
            }
            else if (distance < next)
            {
                next = distance;
                second = index;
            }
        }

        int mix = best <= 0 ? 0 : (int)(Levels * best / (best + next) + 0.5);
        mix = mix < MinMix ? 0 : mix;
        return Cache[key] = mix << 8 | second << 4 | first;
    }

    private static double Distance(Rgba a, Rgba b)
    {
        double mean = (a.R + b.R) / 2.0;
        int red = a.R - b.R;
        int green = a.G - b.G;
        int blue = a.B - b.B;
        return (2 + mean / 256) * red * red + 4 * green * green + (2 + (255 - mean) / 256) * blue * blue;
    }
}
