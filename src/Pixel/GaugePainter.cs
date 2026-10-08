using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class GaugePainter
{
    public const int Width = 16;
    private const int Row = TavernRows.CounterTop + 3;
    private const int Inner = Width - 2;
    private const float FlashRate = 6f;

    private static readonly Color Frame = PixelPalette.Resolve('X');
    private static readonly Color Empty = PixelPalette.Resolve('d');
    private static readonly Color Zone = PixelPalette.Resolve('g');
    private static readonly Color Filled = PixelPalette.Resolve('f');
    private static readonly Color Cursor = PixelPalette.Resolve('h');

    public static void Paint(PixelCanvas canvas, int top, int x, Station station, Color beyond, float time)
    {
        if (!station.Active)
        {
            return;
        }

        int y = top + Row;
        bool flash = station.InZone && (int)(time * FlashRate) % 2 == 0;
        canvas.Fill(x, y, Width, 4, flash ? Zone : Frame);
        canvas.Fill(x + 1, y + 1, Inner, 2, Empty);
        int zoneStart = (int)(station.ZoneStart * Inner);
        int zoneEnd = (int)System.MathF.Ceiling(station.ZoneEnd * Inner);
        canvas.Fill(x + 1 + zoneStart, y + 1, zoneEnd - zoneStart, 2, Zone with { A = station.InZone ? 1f : 0.55f });
        canvas.Fill(x + 1 + zoneEnd, y + 1, Inner - zoneEnd, 2, beyond with { A = 0.55f });
        int reached = System.Math.Clamp((int)(station.Progress * Inner), 0, Inner - 1);
        canvas.Fill(x + 1, y + 2, reached, 1, Filled with { A = 0.8f });
        canvas.Fill(x + 1 + reached, y, 1, 4, Cursor);
    }
}
