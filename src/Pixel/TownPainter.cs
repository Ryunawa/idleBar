using System;
using Godot;

namespace IdleBar.Pixel;

public static class TownPainter
{
    public const int Width = 66;

    private const float SmokeSpeed = 0.45f;
    private const int SmokePuffs = 3;
    private const int SmokeRise = 7;
    private const int SmokeDrift = 4;
    private const float FlickerSpeed = 11f;

    private static readonly Color Track = new("2d333b");
    private static readonly Color TrackDone = new("f2c14e");
    private static readonly Color Smoke = new("9aa3ad");
    private static readonly Color LampLight = new("f7d774");

    private static readonly TownPlot[] Layout =
    [
        new(TownPiece.House, 0),
        new(TownPiece.Lamp, 12),
        new(TownPiece.TallHouse, 17),
        new(TownPiece.Stall, 28),
        new(TownPiece.House, 41),
        new(TownPiece.Lamp, 53),
        new(TownPiece.TallHouse, 58),
    ];

    public static void PaintHouses(PixelCanvas canvas, int start, Ambience ambience)
    {
        int ground = LandscapePainter.GroundTop(canvas);
        bool lit = ambience.Light.Lit;
        foreach (TownPlot plot in Layout)
        {
            PixelSprite sprite = TownSprites.For(plot.Piece, lit);
            int x = start + plot.Offset;
            int top = ground - sprite.Height + 1;
            switch (plot.Piece)
            {
                case TownPiece.House:
                    PaintSmoke(canvas, x + TownSprites.ChimneyColumn, top - 1, ambience.Time + plot.Offset);
                    break;
                case TownPiece.Lamp when lit:
                    PaintLampGlow(canvas, x + TownSprites.LampHeadColumn, top, ground, ambience.Time + plot.Offset);
                    break;
            }

            canvas.Draw(sprite, x, top);
        }

        FolkPainter.PaintVillagers(canvas, start, Width, ambience);
    }

    public static void PaintProgress(PixelCanvas canvas, double progress)
    {
        int y = canvas.Height - 1;
        int done = (int)Math.Round(canvas.Width * Math.Clamp(progress, 0, 1));
        canvas.Fill(0, y, canvas.Width, 1, Track);
        canvas.Fill(0, y, done, 1, TrackDone);
    }

    private static void PaintSmoke(PixelCanvas canvas, int x, int y, float time)
    {
        for (int puff = 0; puff < SmokePuffs; puff++)
        {
            float age = (time * SmokeSpeed + (float)puff / SmokePuffs) % 1;
            int size = age > 0.5f ? 2 : 1;
            canvas.Fill(x + (int)(age * SmokeDrift), y - (int)(age * SmokeRise), size, 1, Smoke with { A = 0.5f * (1 - age) });
        }
    }

    private static void PaintLampGlow(PixelCanvas canvas, int x, int top, int ground, float time)
    {
        float flicker = 0.16f + 0.05f * MathF.Sin(time * FlickerSpeed) * MathF.Sin(time * 2.3f);
        canvas.Fill(x - 2, top - 1, 5, 4, LampLight with { A = flicker });
        canvas.Fill(x - 1, top - 2, 3, 6, LampLight with { A = flicker * 0.6f });
        canvas.Fill(x - 3, ground, 7, 1, LampLight with { A = flicker * 0.7f });
    }
}