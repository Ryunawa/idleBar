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

    private static readonly Color Outline = new("1a1411");
    private static readonly Color Cream = PixelPalette.Resolve('f');
    private static readonly Color Impatient = PixelPalette.Resolve('a');

    public static void PaintBodies(PixelCanvas canvas, int top, Tavern tavern, float time)
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
            Color modulate = Colors.White with { A = Visibility(patron, tavern.Layout) };
            canvas.Draw(PatronSprites.For(PatronLook.From(patron.Look), gaze), Left(patron), top + TavernRows.PatronTop - bob, modulate);
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
            int center = (int)MathF.Round(patron.X);
            if (patron.Phase == PatronPhase.Thinking)
            {
                PaintBubble(canvas, top, center, Cream);
                int dots = (int)(patron.PhaseTime * 3) % 4;
                for (int dot = 0; dot < dots; dot++)
                {
                    canvas.Fill(center - 2 + dot * 2, top + TavernRows.BubbleTop + 3, 1, 1, Outline);
                }
            }
            else if (patron.Phase == PatronPhase.Waiting && !patron.Incoming)
            {
                float worry = Math.Clamp((patron.Waited / Patron.Patience - 0.5f) * 2, 0, 1);
                PaintBubble(canvas, top, center, Cream.Lerp(Impatient, worry * 0.55f));
                PixelSprite icon = patron.Order == Drink.Beer ? TavernSprites.BeerIcon : TavernSprites.TeaIcon;
                canvas.Draw(icon, center - 2, top + TavernRows.BubbleTop + 1);
            }
        }
    }

    private static void PaintBubble(PixelCanvas canvas, int top, int center, Color fill)
    {
        int left = center - BubbleWidth / 2;
        int y = top + TavernRows.BubbleTop;
        canvas.Fill(left + 1, y, BubbleWidth - 2, BubbleHeight, Outline);
        canvas.Fill(left, y + 1, BubbleWidth, BubbleHeight - 2, Outline);
        canvas.Fill(left + 1, y + 1, BubbleWidth - 2, BubbleHeight - 2, fill);
        canvas.Fill(center, y + BubbleHeight, 1, 1, Outline);
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
