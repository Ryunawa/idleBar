using System;

namespace IdleBar.Pixel;

public static class LandscapePainter
{
    public const int GroundRows = 5;

    private const float PropParallax = 0.75f;
    private const int RoadMarkSpacing = 7;
    private const int StarDensity = 9;
    private const int WidestProp = 16;

    public static int GroundTop(PixelCanvas canvas) => canvas.Height - GroundRows;

    public static int Scatter(int seed) => (int)Math.Abs((seed * 73856093L + 19349663L) % 83492791L);

    public static void Paint(PixelCanvas canvas, BiomeStyle style, float distance, bool withProps)
    {
        int ground = GroundTop(canvas);
        canvas.Fill(0, 0, canvas.Width, ground, style.Sky);
        PaintStars(canvas, style, ground);
        PaintSilhouette(canvas, style.Far, ground, distance);
        PaintSilhouette(canvas, style.Near, ground, distance);
        PaintGround(canvas, style, ground, distance);
        if (withProps)
        {
            PaintProps(canvas, style, ground, distance);
        }
    }

    private static void PaintStars(PixelCanvas canvas, BiomeStyle style, int ground)
    {
        int skyHeight = Math.Max(ground - 6, 1);
        for (int index = 0; index < canvas.Width / StarDensity; index++)
        {
            canvas.Fill(Scatter(index) % canvas.Width, Scatter(index + 500) % skyHeight, 1, 1, style.Stars);
        }
    }

    private static void PaintSilhouette(PixelCanvas canvas, SilhouetteLayer layer, int ground, float distance)
    {
        int shift = (int)(distance * layer.Parallax);
        for (int x = 0; x < canvas.Width; x++)
        {
            float u = x + shift;
            int height = layer.Height
                + (int)MathF.Round(layer.Amplitude * MathF.Sin(u / layer.Period) + layer.Detail * MathF.Sin(u / layer.DetailPeriod));
            if (height <= 0)
            {
                continue;
            }

            canvas.Fill(x, ground - height, 1, height, layer.Fill);
            canvas.Fill(x, ground - height, 1, 1, layer.Rim);
        }
    }

    private static void PaintGround(PixelCanvas canvas, BiomeStyle style, int ground, float distance)
    {
        canvas.Fill(0, ground, canvas.Width, GroundRows + 1, style.Ground);
        canvas.Fill(0, ground + 1, canvas.Width, 2, style.Road);
        int offset = (int)distance % RoadMarkSpacing;
        for (int x = -offset; x < canvas.Width; x += RoadMarkSpacing)
        {
            canvas.Fill(x, ground + 2, 2, 1, style.RoadMark);
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
