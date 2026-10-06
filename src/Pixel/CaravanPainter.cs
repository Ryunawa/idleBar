using Godot;

namespace IdleBar.Pixel;

public static class CaravanPainter
{
    private const float StepSeconds = 0.2f;
    private const string BanditsHazard = "bandits";
    private const string TollHazard = "peage";

    private static readonly Color Harness = new("4e331d");

    public static void PaintTravelling(PixelCanvas canvas, float time, double progress, CaravanLook look)
    {
        bool stepping = (int)(time / StepSeconds) % 2 == 0;
        PaintCaravan(
            canvas,
            canvas.Width * 3 / 10,
            CaravanSprites.Wagon(stepping, look),
            stepping ? CaravanSprites.OxStepping : CaravanSprites.OxStriding);
        TownPainter.PaintProgress(canvas, progress);
    }

    public static void PaintInTown(PixelCanvas canvas, CaravanLook look)
    {
        TownPainter.PaintHouses(canvas, canvas.Width * 11 / 20);
        PaintCaravan(canvas, canvas.Width / 5, CaravanSprites.Wagon(true, look), CaravanSprites.OxResting);
    }

    public static void PaintHalted(PixelCanvas canvas, double progress, CaravanLook look, string? hazard)
    {
        int x = canvas.Width * 3 / 10;
        PixelSprite wagon = CaravanSprites.Wagon(true, look);
        PaintCaravan(canvas, x, wagon, CaravanSprites.OxResting);
        int front = x + wagon.Width + CaravanSprites.OxResting.Width + 4;
        int bottom = LandscapePainter.GroundTop(canvas) + 2;
        switch (hazard)
        {
            case BanditsHazard:
                canvas.Draw(HazardSprites.Bandit, front, bottom - HazardSprites.Bandit.Height + 1);
                canvas.Draw(HazardSprites.Bandit, front + 7, bottom - HazardSprites.Bandit.Height + 1);
                break;
            case TollHazard:
                canvas.Draw(HazardSprites.Barrier, front, bottom - HazardSprites.Barrier.Height + 1);
                break;
        }

        TownPainter.PaintProgress(canvas, progress);
    }

    private static void PaintCaravan(PixelCanvas canvas, int x, PixelSprite wagon, PixelSprite ox)
    {
        int wheelsBottom = LandscapePainter.GroundTop(canvas) + 2;
        canvas.Draw(wagon, x, wheelsBottom - wagon.Height + 1);
        canvas.Fill(x + wagon.Width - 2, wheelsBottom - 4, 4, 1, Harness);
        canvas.Draw(ox, x + wagon.Width + 1, wheelsBottom - ox.Height + 1);
    }
}
