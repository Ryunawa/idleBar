using Godot;

namespace IdleBar.Pixel;

public static class CaravanPainter
{
    private const float StepSeconds = 0.2f;

    private static readonly Color Harness = new("4e331d");

    public static void PaintTravelling(PixelCanvas canvas, float time, double progress)
    {
        bool stepping = (int)(time / StepSeconds) % 2 == 0;
        PaintCaravan(
            canvas,
            canvas.Width * 3 / 10,
            stepping ? CaravanSprites.WagonRolling : CaravanSprites.WagonTurning,
            stepping ? CaravanSprites.OxStepping : CaravanSprites.OxStriding);
        TownPainter.PaintProgress(canvas, progress);
    }

    public static void PaintInTown(PixelCanvas canvas)
    {
        TownPainter.PaintHouses(canvas, canvas.Width * 11 / 20);
        PaintCaravan(canvas, canvas.Width / 5, CaravanSprites.WagonRolling, CaravanSprites.OxResting);
    }

    private static void PaintCaravan(PixelCanvas canvas, int x, PixelSprite wagon, PixelSprite ox)
    {
        int wheelsBottom = LandscapePainter.GroundTop(canvas) + 2;
        canvas.Draw(wagon, x, wheelsBottom - wagon.Height + 1);
        canvas.Fill(x + wagon.Width - 2, wheelsBottom - 4, 4, 1, Harness);
        canvas.Draw(ox, x + wagon.Width + 1, wheelsBottom - ox.Height + 1);
    }
}
