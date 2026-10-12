namespace IdleBar.Scene;

public static class GroundPainter
{
    private const int Blend = 10;

    public static BiomeLook LookAt(int x, int border, BiomeLook home, BiomeLook away)
    {
        if (x < border - Blend)
        {
            return home;
        }

        if (x > border + Blend)
        {
            return away;
        }

        float share = (x - border + Blend) / (2f * Blend);
        return Noise.Unit(x, 21) < share ? away : home;
    }

    public static void Paint(ArtRaster raster, FrontierLayout layout, int biome)
    {
        BiomeLook home = BiomeLook.Of(0);
        BiomeLook away = BiomeLook.Of(biome);
        int border = layout.FrontX - 12;
        int ground = FrontierLayout.Ground;
        for (int x = 0; x < layout.Width; x++)
        {
            BiomeLook look = LookAt(x, border, home, away);
            bool trampled = x >= layout.FrontX - 6 && x < layout.FogX + 18;
            raster.Fill(x, ground, 1, 1, trampled && Noise.Unit(x, 31) < 0.45f ? look.Dirt : look.Grass);
            if (!trampled && Noise.Unit(x, 32) < 0.16f)
            {
                raster.Set(x, ground - 1, look.Tuft);
            }

            if (!trampled && !look.Desert && Noise.Unit(x, 33) < 0.025f)
            {
                raster.Set(x, ground - 1, Noise.Unit(x, 34) < 0.5f ? Palette.Yellow : Palette.Rose);
            }

            raster.Fill(x, ground + 1, 1, 3, look.Dirt);
            if (Noise.Unit(x, 35) < 0.12f)
            {
                raster.Set(x, ground + 1 + (int)(Noise.Unit(x, 36) * 3), look.Pebble);
            }

            if (Noise.Unit(x, 37) < 0.05f)
            {
                raster.Set(x, ground + 2 + (int)(Noise.Unit(x, 38) * 2), Palette.Peach);
            }
        }
    }
}
