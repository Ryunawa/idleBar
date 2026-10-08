using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class RoomPainter
{
    private const int StationShelfWidth = 21;
    private const int PanelGap = 10;
    private const int JointGap = 16;
    private const int FloorMargin = 14;

    private static readonly Color Wall = PixelPalette.Resolve('D');
    private static readonly Color Joint = PixelPalette.Resolve('d');
    private static readonly Color Wainscot = PixelPalette.Resolve('B');
    private static readonly Color Trim = PixelPalette.Resolve('O');
    private static readonly Color Beam = PixelPalette.Resolve('B');
    private static readonly Color Plank = PixelPalette.Resolve('b');
    private static readonly Color Iron = PixelPalette.Resolve('Z');
    private static readonly Color Doorway = PixelPalette.Resolve('X');
    private static readonly Color Night = new("16233a");

    private static DecorPlan? _plan;

    public static DecorPlan PlanFor(TavernLayout layout, int width)
    {
        int floorStart = layout.SeatXs[^1] + FloorMargin;
        if (_plan is null || _plan.Width != width || _plan.FloorStart != floorStart)
        {
            _plan = DecorPlan.For(width, floorStart);
        }

        return _plan;
    }

    public static void Paint(PixelCanvas canvas, int top, TavernLayout layout, SkyLight sky, float time, bool doorOpen)
    {
        DecorPlan plan = PlanFor(layout, canvas.Width);
        DecorPainter.PaintCeiling(canvas, top);
        PaintWall(canvas, top);
        DecorPainter.PaintPosts(canvas, top, plan);
        DecorPainter.PaintShelf(canvas, top, 1, StationShelfWidth);
        DecorPainter.PaintWall(canvas, top, plan, sky, time);
        PropPainter.PaintFloor(canvas, top, plan, time);
        PaintDoor(canvas, top, layout.DoorX, sky, doorOpen);
        canvas.Fill(0, top, canvas.Width, 1, Beam);
        canvas.Fill(0, top + 1, canvas.Width, 1, Trim);
        DecorPainter.PaintHanging(canvas, top, plan, sky, time);
    }

    private static void PaintWall(PixelCanvas canvas, int top)
    {
        int wainscot = top + TavernRows.Wainscot;
        canvas.Fill(0, top, canvas.Width, wainscot - top, Wall);
        for (int row = top + 6, band = 0; row < wainscot; row += 5, band++)
        {
            canvas.Fill(0, row, canvas.Width, 1, Joint);
            for (int x = band % 2 * JointGap / 2; x < canvas.Width; x += JointGap)
            {
                canvas.Fill(x, row - 4, 1, 4, Joint);
            }
        }

        canvas.Fill(0, wainscot, canvas.Width, TavernRows.CounterTop - TavernRows.Wainscot, Wainscot);
        canvas.Fill(0, wainscot, canvas.Width, 1, Trim);
        for (int x = PanelGap / 2; x < canvas.Width; x += PanelGap)
        {
            canvas.Fill(x, wainscot + 2, 1, 3, Joint);
        }
    }

    private static void PaintDoor(PixelCanvas canvas, int top, int x, SkyLight sky, bool open)
    {
        int y = top + 5;
        int height = TavernRows.CounterTop - 5;
        canvas.Fill(x, y, TavernLayout.DoorWidth, height, Beam);
        if (open)
        {
            canvas.Fill(x + 1, y + 1, TavernLayout.DoorWidth - 2, height - 1, Doorway);
            canvas.Fill(x + 1, y + 1, TavernLayout.DoorWidth - 2, 6, sky.Horizon(Night).Darkened(0.3f));
            canvas.Fill(x + 1, y + 1, 2, height - 1, Plank);
            return;
        }

        canvas.Fill(x + 1, y + 1, TavernLayout.DoorWidth - 2, height - 1, Plank);
        canvas.Fill(x + 4, y + 1, 1, height - 1, Trim);
        canvas.Fill(x + 7, y + 1, 1, height - 1, Trim);
        canvas.Fill(x + 1, y + 4, TavernLayout.DoorWidth - 2, 1, Beam);
        canvas.Fill(x + 1, y + height - 4, TavernLayout.DoorWidth - 2, 1, Beam);
        canvas.Fill(x + 8, y + 8, 1, 2, Iron);
    }
}
