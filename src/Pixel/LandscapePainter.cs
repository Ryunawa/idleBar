using System;
using Godot;

namespace IdleBar.Pixel;

public static class LandscapePainter
{
    public const int GroundRows = 4;

    private const float PropParallax = 0.75f;
    private const int RoadMarkSpacing = 7;
    private const int WidestProp = 16;
    private const int TuftSpacing = 6;
    private const float WindSpeed = 1.8f;
    private const float WindWavelength = 0.12f;
    private const float GustSeconds = 9f;
    private const float DesignSky = 18f;

    public static int GroundTop(PixelCanvas canvas) => canvas.Height - GroundRows;

    public static int Scatter(int seed) => (int)Math.Abs((seed * 73856093L + 19349663L) % 83492791L);

    public static void Paint(PixelCanvas canvas, BiomeStyle style, float distance, bool withProps, Ambience ambience)
    {
        int ground = GroundTop(canvas);
        SkyLight light = ambience.Light;
        SkyPainter.Paint(canvas, style, ground, distance, ambience);
        PaintSilhouette(canvas, style.Far, ground, distance, light);
        PaintSilhouette(canvas, style.Near, ground, distance, light);
        PaintGround(canvas, style, ground, distance, light);
        if (style.Tuft is Color tuft)
        {
            PaintTufts(canvas, light.Shade(tuft), ground, distance, ambience.Time);
        }

        if (withProps)
        {
            PaintProps(canvas, style, ground, distance);
        }
    }

    private static void PaintSilhouette(PixelCanvas canvas, SilhouetteLayer layer, int ground, float distance, SkyLight light)
    {
        Color fill = light.Shade(layer.Fill);
        Color rim = light.Shade(layer.Rim);
        float vertical = Math.Min(1f, ground / DesignSky);
        int shift = (int)(distance * layer.Parallax);
        for (int x = 0; x < canvas.Width; x++)
        {
            float u = x + shift;
            int height = (int)MathF.Round((layer.Height
                + layer.Amplitude * MathF.Sin(u / layer.Period) + layer.Detail * MathF.Sin(u / layer.DetailPeriod)) * vertical);
            if (height <= 0)
            {
                continue;
            }

            canvas.Fill(x, ground - height, 1, height, fill);
            canvas.Fill(x, ground - height, 1, 1, rim);
        }
    }

    private static void PaintGround(PixelCanvas canvas, BiomeStyle style, int ground, float distance, SkyLight light)
    {
        canvas.Fill(0, ground, canvas.Width, GroundRows + 1, light.Shade(style.Ground));
        canvas.Fill(0, ground + 1, canvas.Width, 2, light.Shade(style.Road));
        Color mark = light.Shade(style.RoadMark);
        int offset = (int)distance % RoadMarkSpacing;
        for (int x = -offset; x < canvas.Width; x += RoadMarkSpacing)
        {
            canvas.Fill(x, ground + 2, 2, 1, mark);
        }
    }

    private static void PaintTufts(PixelCanvas canvas, Color tuft, int ground, float distance, float time)
    {
        int shift = (int)distance;
        float gust = 0.5f + 0.5f * MathF.Sin(time * MathF.Tau / GustSeconds);
        for (int index = shift / TuftSpacing - 1; index <= (shift + canvas.Width) / TuftSpacing + 1; index++)
        {
            int seed = Scatter(index + 1200);
            int x = index * TuftSpacing + seed % 4 - shift;
            float wave = MathF.Sin(time * WindSpeed - (x + shift) * WindWavelength);
            int lean = wave * gust > 0.3f ? 1 : 0;
            canvas.Fill(x, ground - 1, 1, 1, tuft);
            if (seed % 3 != 0)
            {
                canvas.Fill(x + lean, ground - 2, 1, 1, tuft);
            }
        }
    }

    private static void PaintProps(PixelCanvas canvas, BiomeStyle style, int ground, float distance)
    {
        int shift = (int)(distance * PropParallax);
        int spacing = style.PropSpacing;
        int first = (int)Math.Floor((shift - WidestProp) / (double)spacing);
        int last = (shift + canvas.Width) / spacing + 1;
        for (int index = first; index <= last; index++)
        {
            int seed = Scatter(index);
            PixelSprite prop = style.Props[seed % style.Props.Count];
            int x = index * spacing + seed % (spacing / 2 + 1) - shift;
            canvas.Draw(prop, x, ground - prop.Height + 1);
        }
    }
}
