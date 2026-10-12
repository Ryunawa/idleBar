using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class FogPainter
{
    private const int Edge = 6;
    private const int FrontRamp = 26;

    private static readonly int[] Bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, SceneMotion motion, SkyTones sky, float time)
    {
        if (layout.FogWidth > 0)
        {
            Region(r, layout, layout.FogX, layout.SkillsX, sky, time, x => Math.Clamp((x - layout.FogX) / (float)FrontRamp, 0, 1));
        }

        if (layout.Narrow)
        {
            return;
        }

        for (int index = 0; index < FrontierLayout.Slots; index++)
        {
            float strength = Strength(state, motion, index);
            if (strength <= 0)
            {
                continue;
            }

            int left = layout.SlotX(index) - 3;
            int right = layout.SlotX(index) + layout.SlotWidth + 3;
            Region(r, layout, left, right, sky, time, x => strength * Math.Clamp(Math.Min(x - left, right - x) / (float)Edge, 0, 1));
        }
    }

    public static float Strength(FrontierState state, SceneMotion motion, int index)
    {
        if (!Economy.Unlocked(state, (BuildingKind)index))
        {
            return 1;
        }

        return motion.Reveal[index] is float reveal ? 1 - reveal / SceneMotion.RevealSeconds : 0;
    }

    private static void Region(ArtRaster r, FrontierLayout layout, int from, int to, SkyTones sky, float time, Func<int, float> alphaAt)
    {
        for (int x = Math.Max(0, from); x < Math.Min(layout.SkillsX, to); x++)
        {
            float alpha = alphaAt(x);
            if (alpha <= 0)
            {
                continue;
            }

            for (int y = -layout.Top; y < FrontierLayout.Rows; y++)
            {
                float noise = 0.5f + 0.25f * MathF.Sin(x * 0.31f + time * 0.9f + y * 0.5f) + 0.25f * MathF.Sin(y * 0.7f - time * 0.6f + x * 0.13f);
                float value = alpha * (0.72f + 0.5f * noise) - (y < 3 ? 0.12f : 0);
                if (value * 16 <= Bayer[(y & 3) * 4 + (x & 3)])
                {
                    continue;
                }

                r.Set(x, y, noise > 0.62f ? sky.FogLight : sky.FogDark);
            }
        }
    }
}
