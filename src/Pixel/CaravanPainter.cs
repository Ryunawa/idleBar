using System;
using Godot;

namespace IdleBar.Pixel;

public static class CaravanPainter
{
    public const int TrainHeight = 10;

    private const float StepSeconds = 0.2f;
    private const float SwishSeconds = 0.25f;
    private const int SwishCycle = 14;
    private const float BobSeconds = 0.5f;
    private const int Coupling = 1;
    private const string BanditsHazard = "bandits";
    private const string TollHazard = "peage";

    private static readonly Color Harness = new("4e331d");

    public static int RoadX(PixelCanvas canvas) => canvas.Width * 3 / 10;

    public static int TrainWidth(int wagons) =>
        Math.Max(wagons, 1) * (CaravanSprites.Wagon(true, CaravanLook.Plain).Width + Coupling) + CaravanSprites.OxResting.Width;

    public static void PaintTravelling(PixelCanvas canvas, float time, double progress, CaravanLook look)
    {
        bool stepping = (int)(time / StepSeconds) % 2 == 0;
        PaintTrain(canvas, RoadX(canvas), look, CaravanSprites.Wagon(stepping, look), stepping ? CaravanSprites.OxStepping : CaravanSprites.OxStriding);
        TownPainter.PaintProgress(canvas, progress);
    }

    public static void PaintParked(PixelCanvas canvas, int x, CaravanLook look, Ambience ambience) =>
        PaintTrain(canvas, x, look, CaravanSprites.Wagon(true, look), RestingOx(ambience.Time));

    public static void PaintHalted(PixelCanvas canvas, double progress, CaravanLook look, string? hazard, float time)
    {
        int x = RoadX(canvas);
        PaintTrain(canvas, x, look, CaravanSprites.Wagon(true, look), RestingOx(time));
        int front = x + TrainWidth(look.Wagons) + 4;
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

    private static void PaintTrain(PixelCanvas canvas, int x, CaravanLook look, PixelSprite wagon, PixelSprite ox)
    {
        int wheelsBottom = LandscapePainter.GroundTop(canvas) + 2;
        int wagons = Math.Max(look.Wagons, 1);
        for (int index = 0; index < wagons; index++)
        {
            int wagonX = x + index * (wagon.Width + Coupling);
            canvas.Draw(wagon, wagonX, wheelsBottom - wagon.Height + 1);
            canvas.Fill(wagonX + wagon.Width - 2, wheelsBottom - 4, 2 + Coupling + 1, 1, Harness);
        }

        canvas.Draw(ox, x + wagons * (wagon.Width + Coupling), wheelsBottom - ox.Height + 1);
    }
}