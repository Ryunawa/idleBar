using System;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public static class PatronPainter
{
    private const float FadeSeconds = 0.5f;
    private const float FadeDistance = 6f;
    private const float StepRate = 6f;
    private const float SipEvery = 3.2f;
    private const float SipSeconds = 0.7f;
    private const float BlinkEvery = 3.7f;
    private const float BlinkSeconds = 0.14f;
    private const int BubbleWidth = 9;
    private const int BubbleHeight = 7;
    private const float GhostAlpha = 0.7f;
    private const int SideOffset = 9;

    private static readonly Color Outline = new("1a1411");
    private static readonly Color Cream = PixelPalette.Resolve('f');
    private static readonly Color Impatient = PixelPalette.Resolve('a');
    private static readonly Color Heart = PixelPalette.Resolve('r');

    public static void PaintBodies(PixelCanvas canvas, int top, Tavern tavern, float time, Func<Patron, PatronLook?> lookOf)
    {
        foreach (Patron patron in tavern.Patrons)
        {
            Gaze gaze = patron.Heading switch
            {
                < 0 => Gaze.Left,
                > 0 => Gaze.Right,
                _ => Blinking(patron, time) ? Gaze.Closed : Gaze.Front,
            };
            int bob = patron.Heading != 0 && (int)(time * StepRate + patron.Look % 7) % 2 == 0 ? 1 : 0;
            float ghostly = patron.Regular == RegularLooks.Ghost ? GhostAlpha : 1;
            Color modulate = Colors.White with { A = Visibility(patron, tavern.Layout) * ghostly };
            PatronLook look = lookOf(patron) ?? PatronLook.From(patron.Look);
            canvas.Draw(PatronSprites.For(look, gaze), Left(patron), top + TavernRows.PatronTop - bob, modulate);
        }
    }

    public static void PaintGlasses(PixelCanvas canvas, int top, Tavern tavern, float time)
    {
        foreach (Patron patron in tavern.Patrons)
        {
            if (patron.Phase != PatronPhase.Drinking || patron.Glass is not PreparedDrink glass)
            {
                continue;
            }

            bool sipping = glass.Drink == Drink.Beer && (patron.PhaseTime + patron.Look % 5) % SipEvery < SipSeconds && patron.PhaseTime > 1;
            int bottom = sipping ? top + TavernRows.PatronTop + PatronSprites.MouthRow + 3 : top + TavernRows.CounterTop - 1;
            DrinkPainter.Paint(canvas, glass, (int)MathF.Round(patron.X) + (sipping ? -1 : 1), bottom);
        }
    }

    public static void PaintBubbles(PixelCanvas canvas, int top, Tavern tavern, float time)
    {
        foreach (Patron patron in tavern.Patrons)
        {
            int head = (int)MathF.Round(patron.X);
            bool beside = patron.Visit is not null;
            int center = beside ? head + SideOffset : head;
            int y = top + (beside ? TavernRows.PatronTop : TavernRows.BubbleTop);
            if (patron.Phase == PatronPhase.Thinking)
            {
                PaintBubble(canvas, center, y, Cream, beside);
                int dots = (int)(patron.PhaseTime * 3) % 4;
                for (int dot = 0; dot < dots; dot++)
                {
                    canvas.Fill(center - 2 + dot * 2, y + 3, 1, 1, Outline);
                }
            }
            else if (patron.Phase == PatronPhase.Waiting && !patron.Incoming)
            {
                float worry = Math.Clamp((patron.Waited / Patron.Patience - 0.5f) * 2, 0, 1);
                PaintBubble(canvas, center, y, Cream.Lerp(Impatient, worry * 0.55f), beside);
                canvas.Draw(DrinkPainter.Icon(patron.Order), center - 2, y + 1);
                if (!beside && patron.Regular is not null)
                {
                    PaintHeart(canvas, center + BubbleWidth / 2 + 1, top + TavernRows.BubbleTop + 1);
                }
            }
        }
    }

    private static void PaintBubble(PixelCanvas canvas, int center, int y, Color fill, bool beside)
    {
        int left = center - BubbleWidth / 2;
        canvas.Fill(left + 1, y, BubbleWidth - 2, BubbleHeight, Outline);
        canvas.Fill(left, y + 1, BubbleWidth, BubbleHeight - 2, Outline);
        canvas.Fill(left + 1, y + 1, BubbleWidth - 2, BubbleHeight - 2, fill);
        if (beside)
        {
            canvas.Fill(left - 1, y + 3, 1, 1, Outline);
            return;
        }

        canvas.Fill(center, y + BubbleHeight, 1, 1, Outline);
    }

    private static void PaintHeart(PixelCanvas canvas, int x, int y)
    {
        canvas.Fill(x, y, 1, 1, Heart);
        canvas.Fill(x + 2, y, 1, 1, Heart);
        canvas.Fill(x, y + 1, 3, 1, Heart);
        canvas.Fill(x + 1, y + 2, 1, 1, Heart);
    }

    private static int Left(Patron patron) => (int)MathF.Round(patron.X) - PatronSprites.Width / 2;

    private static bool Blinking(Patron patron, float time) => (time + patron.Look % 11 * 0.3f) % BlinkEvery < BlinkSeconds;

    private static float Visibility(Patron patron, TavernLayout layout) => patron.Phase switch
    {
        PatronPhase.Entering => Math.Min(1, patron.PhaseTime / FadeSeconds),
        PatronPhase.Leaving => Math.Clamp(Math.Abs(patron.X - layout.DoorCenter) / FadeDistance, 0, 1),
        _ => 1,
    };
}
