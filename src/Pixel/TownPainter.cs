using System;
using Godot;

namespace IdleBar.Pixel;

public static class TownPainter
{
    private static readonly Color Track = new("2d333b");
    private static readonly Color TrackDone = new("f2c14e");

    private static readonly TownPlot[] Layout =
    [
        new(TownSprites.House, 0),
        new(TownSprites.Lamp, 12),
        new(TownSprites.TallHouse, 17),
        new(TownSprites.Stall, 28),
        new(TownSprites.House, 41),
        new(TownSprites.Lamp, 53),
        new(TownSprites.TallHouse, 58),
    ];

    public static void PaintHouses(PixelCanvas canvas, int start)
    {
        int ground = LandscapePainter.GroundTop(canvas);
        foreach (TownPlot plot in Layout)
        {
            canvas.Draw(plot.Sprite, start + plot.Offset, ground - plot.Sprite.Height + 1);
        }
    }

    public static void PaintProgress(PixelCanvas canvas, double progress)
    {
        int y = canvas.Height - 1;
        int done = (int)Math.Round(canvas.Width * Math.Clamp(progress, 0, 1));
        canvas.Fill(0, y, canvas.Width, 1, Track);
        canvas.Fill(0, y, done, 1, TrackDone);
    }
}
