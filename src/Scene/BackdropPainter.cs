using System;

namespace IdleBar.Scene;

public static class BackdropPainter
{
    private const int StarCount = 46;
    private const int CloudCount = 5;

    public static void Paint(ArtRaster raster, FrontierLayout layout, SkyTones sky, SceneLight light, int biome, float time)
    {
        for (int y = -layout.Top; y < FrontierLayout.Ground; y++)
        {
            int band = Math.Clamp((y + 2) / 6, 0, sky.Bands.Count - 1);
            raster.Fill(0, y, layout.Width, 1, sky.Bands[band]);
            if ((y + 2) % 6 == 5 && band < sky.Bands.Count - 1)
            {
                for (int x = y % 2 == 0 ? 0 : 1; x < layout.Width; x += 2)
                {
                    raster.Set(x, y, sky.Bands[band + 1]);
                }
            }
        }

        PaintStars(raster, layout, sky, time);
        PaintCelestial(raster, layout, light, sky);
        PaintHills(raster, layout, sky, light, biome);
        PaintClouds(raster, layout, light, time);
    }

    private static void PaintStars(ArtRaster raster, FrontierLayout layout, SkyTones sky, float time)
    {
        if (sky.Stars <= 0)
        {
            return;
        }

        for (int star = 0; star < StarCount * layout.Width / 500; star++)
        {
            int x = (int)(Noise.Unit(star, 1) * layout.Width);
            int y = (int)(Noise.Unit(star, 2) * (13 + layout.Top)) - layout.Top;
            float twinkle = MathF.Sin(time * (0.6f + Noise.Unit(star, 3) * 2) + Noise.Unit(star, 4) * 6);
            if (twinkle > -0.2f)
            {
                raster.Set(x, y, (Noise.Unit(star, 5) < 0.3f ? Palette.White : Palette.Silver).WithAlpha(Math.Min(1, sky.Stars)));
            }
        }
    }

    private static void PaintCelestial(ArtRaster raster, FrontierLayout layout, SceneLight light, SkyTones sky)
    {
        if (light.Rain || light.Snow)
        {
            return;
        }

        if (light.Sun >= 0)
        {
            Disc(raster, Arc(layout, light.Sun), Height(light.Sun), Palette.Yellow, Palette.Gold);
        }

        if (light.Moon >= 0)
        {
            int x = Arc(layout, light.Moon);
            int y = Height(light.Moon);
            Disc(raster, x, y, Palette.Sand, Palette.Sand);
            Disc(raster, x + 2, y - 1, sky.Bands[0], sky.Bands[0]);
        }
    }

    private static int Arc(FrontierLayout layout, float course) => (int)(layout.Width * (0.08f + 0.84f * course));

    private static int Height(float course) => (int)(17 - MathF.Sin(MathF.PI * course) * 15);

    private static void Disc(ArtRaster raster, int x, int y, Rgba core, Rgba rim)
    {
        raster.Fill(x - 1, y - 2, 3, 5, rim);
        raster.Fill(x - 2, y - 1, 5, 3, rim);
        raster.Fill(x - 1, y - 1, 3, 3, core);
    }

    private static void PaintHills(ArtRaster raster, FrontierLayout layout, SkyTones sky, SceneLight light, int biome)
    {
        BiomeLook home = BiomeLook.Of(0);
        BiomeLook away = BiomeLook.Of(biome);
        int border = layout.FrontX - 12;
        for (int x = 0; x < layout.Width; x++)
        {
            int far = (int)Math.Round(13 + 2.5 * Math.Sin(x * 0.045) + 1.5 * Math.Sin(x * 0.11 + 1.3));
            raster.Fill(x, far, 1, FrontierLayout.Ground - far, sky.Hill(GroundPainter.LookAt(x, border, home, away).FarHill, light));
        }

        for (int x = 0; x < layout.Width; x++)
        {
            if (Noise.Unit(x, 9) < 0.035f)
            {
                BiomeLook look = GroundPainter.LookAt(x, border, home, away);
                PaintTree(raster, x, Near(x), sky.Hill(look.Tree, light), look.Desert);
            }
        }

        for (int x = 0; x < layout.Width; x++)
        {
            int near = Near(x);
            raster.Fill(x, near, 1, FrontierLayout.Ground - near, sky.Hill(GroundPainter.LookAt(x, border, home, away).NearHill, light));
        }
    }

    private static int Near(int x) => (int)Math.Round(18 + 1.5 * Math.Sin(x * 0.08 + 2.1) + Math.Sin(x * 0.19));

    private static void PaintTree(ArtRaster raster, int x, int root, Rgba color, bool cactus)
    {
        if (cactus)
        {
            raster.Fill(x, root - 5, 1, 5, color);
            raster.Fill(x - 2, root - 3, 1, 2, color);
            raster.Fill(x - 2, root - 2, 2, 1, color);
            return;
        }

        for (int row = 0; row < 7; row++)
        {
            int half = row / 2;
            raster.Fill(x - half, root - 6 + row, half * 2 + 1, 1, color);
        }
    }

    private static void PaintClouds(ArtRaster raster, FrontierLayout layout, SceneLight light, float time)
    {
        bool wet = light.Rain || light.Snow;
        Rgba top = PicoFilter.Nearest(wet ? Palette.Gray : Palette.White.Mix(Palette.Rose, light.Warmth * 0.6f).Mix(Palette.Slate, 1 - light.Daylight));
        Rgba bottom = wet ? Palette.Slate : top == Palette.PicoWhite ? Palette.LightGrey : PicoFilter.Nearest(top.Mix(Palette.Steel, 0.35f));
        int count = CloudCount * layout.Width / 500 + (wet ? 4 : 0);
        for (int cloud = 0; cloud < count; cloud++)
        {
            int width = 8 + (int)(Noise.Unit(cloud, 11) * 8);
            int span = layout.Width + 40;
            int x = (int)((Noise.Unit(cloud, 12) * span + time * (0.8f + Noise.Unit(cloud, 13))) % span) - 20;
            int y = 1 + (int)(Noise.Unit(cloud, 14) * 6) - layout.Top / 2;
            raster.Fill(x + 2, y, width - 4, 1, top);
            raster.Fill(x, y + 1, width, 2, top);
            raster.Fill(x + 1, y + 3, width - 2, 1, bottom);
        }
    }
}
