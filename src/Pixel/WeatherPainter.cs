using Godot;

namespace IdleBar.Pixel;

public static class WeatherPainter
{
    private const int DropSpacing = 3;
    private const float FallSpeed = 26f;

    private static readonly Color Gloom = new(0.05f, 0.07f, 0.1f, 0.35f);
    private static readonly Color Drop = new(0.56f, 0.7f, 0.85f, 0.75f);

    public static void PaintRain(PixelCanvas canvas, float time)
    {
        int ground = LandscapePainter.GroundTop(canvas);
        canvas.Fill(0, 0, canvas.Width, ground + LandscapePainter.GroundRows, Gloom);
        int fall = (int)(time * FallSpeed);
        for (int index = 0; index < canvas.Width / DropSpacing; index++)
        {
            int seed = LandscapePainter.Scatter(index + 700);
            int y = (seed + fall) % (ground + 2);
            int x = ((index * DropSpacing + seed % DropSpacing - y / 2) % canvas.Width + canvas.Width) % canvas.Width;
            canvas.Fill(x, y, 1, 2, Drop);
        }
    }
}
