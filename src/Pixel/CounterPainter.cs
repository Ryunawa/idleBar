using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class CounterPainter
{
    private const int SeamGap = 14;
    private const int Bottom = TavernRows.CounterTop - 1;

    private static readonly Color TopLight = PixelPalette.Resolve('o');
    private static readonly Color TopWood = PixelPalette.Resolve('b');
    private static readonly Color Front = PixelPalette.Resolve('O');
    private static readonly Color Seam = PixelPalette.Resolve('B');
    private static readonly Color Gold = PixelPalette.Resolve('g');

    public static void Paint(PixelCanvas canvas, int top)
    {
        int y = top + TavernRows.CounterTop;
        canvas.Fill(0, y, canvas.Width, 1, TopLight);
        canvas.Fill(0, y + 1, canvas.Width, 1, TopWood);
        canvas.Fill(0, y + 2, canvas.Width, canvas.Height - y - 2, Front);
        canvas.Fill(0, y + 2, canvas.Width, 1, Seam);
        canvas.Fill(0, canvas.Height - 1, canvas.Width, 1, Seam);
        for (int x = SeamGap / 2; x < canvas.Width; x += SeamGap)
        {
            canvas.Fill(x, y + 3, 1, canvas.Height - y - 4, Seam);
        }
    }

    public static void PaintDrinks(PixelCanvas canvas, int top, Tavern tavern)
    {
        int bottom = top + Bottom;
        foreach (SlidingDrink slide in tavern.Slides)
        {
            DrinkPainter.Paint(canvas, slide.Drink, (int)slide.X + 1, bottom);
        }
    }

    public static void Highlight(PixelCanvas canvas, int top, Rect2I bounds) =>
        canvas.Fill(bounds.Position.X, top + TavernRows.CounterTop, bounds.Size.X, 1, Gold);
}
