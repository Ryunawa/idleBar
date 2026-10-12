using System;

namespace IdleBar.Scene;

public static class WeatherPainter
{
    private const int DropsPer100 = 9;

    public static void Paint(ArtRaster r, FrontierLayout layout, SceneLight light, float time)
    {
        if (!light.Rain && !light.Snow)
        {
            return;
        }

        int height = FrontierLayout.Rows + layout.Top;
        int count = DropsPer100 * layout.Width / 100;
        for (int drop = 0; drop < count; drop++)
        {
            float speed = light.Snow ? 4 + Noise.Unit(drop, 41) * 3 : 26 + Noise.Unit(drop, 41) * 10;
            float fall = (Noise.Unit(drop, 42) * height + time * speed) % height;
            float drift = light.Snow ? MathF.Sin(time * 1.3f + drop) * 2 : -fall * 0.3f;
            int x = (int)(Noise.Unit(drop, 43) * layout.Width + drift);
            int y = (int)fall - layout.Top;
            if (light.Snow)
            {
                r.Set(x, y, Palette.White.WithAlpha(0.9f));
                continue;
            }

            Rgba streak = Palette.Silver.WithAlpha(0.55f);
            r.Set(x, y, streak);
            r.Set(x - 1, y + 1, streak);
        }
    }
}
