using Godot;

namespace IdleBar.Pixel;

public static class CaravanPainter
{
    private const float StepSeconds = 0.2f;
    private const float SwishSeconds = 0.25f;
    private const int SwishCycle = 14;
    private const float BobSeconds = 0.5f;
    private const string BanditsHazard = "bandits";
    private const string TollHazard = "peage";

    private static readonly Color Harness = new("4e331d");

    public static int RoadX(PixelCanvas canvas) => canvas.Width * 3 / 10;

    public static int TownX(PixelCanvas canvas) => canvas.Width / 5;

    public static void PaintTravelling(PixelCanvas canvas, float time, double progress, CaravanLook look)
    {
        bool stepping = (int)(time / StepSeconds) % 2 == 0;
        PaintCaravan(
            canvas,
            RoadX(canvas),
            CaravanSprites.Wagon(stepping, look),
            stepping ? CaravanSprites.OxStepping : CaravanSprites.OxStriding);
        TownPainter.PaintProgress(canvas, progress);
    }

    public static void PaintInTown(PixelCanvas canvas, CaravanLook look, Ambience ambience)
    {
        TownPainter.PaintHouses(canvas, canvas.Width * 11 / 20, ambience);
        PaintCaravan(canvas, TownX(canvas), CaravanSprites.Wagon(true, look), RestingOx(ambience.Time));
    }

    public static void PaintHalted(PixelCanvas canvas, double progress, CaravanLook look, string? hazard, float time)
    {
        int x = RoadX(canvas);
        PixelSprite wagon = CaravanSprites.Wagon(true, look);
        PaintCaravan(canvas, x, wagon, RestingOx(time));
        int front = x + wagon.Width + CaravanSprites.OxResting.Width + 4;
        int bottom = LandscapePainter.GroundTop(canvas) + 2;
        int bob = (int)(time / BobSeconds) % 2;
        switch (hazard)
        {
            case BanditsHazard:
                canvas.Draw(HazardSprites.Bandit, front, bottom - HazardSprites.Bandit.Height + 1 - bob);
                canvas.Draw(HazardSprites.Bandit, front + 7, bottom - HazardSprites.Bandit.Height + bob);
                break;
            case TollHazard:
                canvas.Draw(HazardSprites.Barrier, front, bottom - HazardSprites.Barrier.Height + 1);
                break;
        }

        TownPainter.PaintProgress(canvas, progress);
    }

    private static PixelSprite RestingOx(float time) =>
        (int)(time / SwishSeconds) % SwishCycle is 0 or 2 ? CaravanSprites.OxSwishing : CaravanSprites.OxResting;

    private static void PaintCaravan(PixelCanvas canvas, int x, PixelSprite wagon, PixelSprite ox)
    {
        int wheelsBottom = LandscapePainter.GroundTop(canvas) + 2;
        canvas.Draw(wagon, x, wheelsBottom - wagon.Height + 1);
        canvas.Fill(x + wagon.Width - 2, wheelsBottom - 4, 4, 1, Harness);
        canvas.Draw(ox, x + wagon.Width + 1, wheelsBottom - ox.Height + 1);
    }
}