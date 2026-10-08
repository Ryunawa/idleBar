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
    private const int ChatWidth = 7;
    private const int ChatHeight = 6;
    private const float ChatTurn = 2.6f;
    private const float ChatShown = 1.7f;
    private const int Glyphs = 4;
    private const int NoteGlyph = 2;

    private static readonly Color Outline = new("1a1411");
    private static readonly Color Cream = PixelPalette.Resolve('f');
    private static readonly Color Impatient = PixelPalette.Resolve('a');
    private static readonly Color Heart = new("e8576b");

    public static void PaintBodies(PixelCanvas canvas, int top, Tavern tavern, float time, Func<Patron, PatronLook?> lookOf)
    {
        foreach (Patron patron in tavern.Patrons)
        {
            Gaze gaze = (patron.Heading != 0 ? patron.Heading : patron.Facing) switch
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
            if (patron.Phase is not (PatronPhase.Drinking or PatronPhase.Mingling) || patron.Heading != 0 || patron.Glass is not PreparedDrink glass)
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
            bool beside = patron.Visit is not null || patron.Regular is not null;
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
            }
            else if (patron.Phase == PatronPhase.Mingling && patron.Heading == 0 && Chatting(patron, time) is int glyph)
            {
                int side = patron.Facing == 0 ? 1 : -patron.Facing;
                PaintChat(canvas, beside ? head + side * SideOffset : head, y, glyph, beside ? side : 0);
            }
        }
    }

    private static int? Chatting(Patron patron, float time)
    {
        int turn = (int)(time / ChatTurn);
        if (time % ChatTurn >= ChatShown)
        {
            return null;
        }

        bool speaks = patron.Facing switch
        {
            > 0 => turn % 2 == 0,
            < 0 => turn % 2 == 1,
            _ => turn % 3 == 0,
        };
        return !speaks ? null : patron.Facing == 0 ? NoteGlyph : (turn * 7 + patron.Look % 97) % Glyphs;
    }

    private static void PaintChat(PixelCanvas canvas, int center, int y, int glyph, int side)
    {
        int left = center - ChatWidth / 2;
        canvas.Fill(left + 1, y, ChatWidth - 2, ChatHeight, Outline);
        canvas.Fill(left, y + 1, ChatWidth, ChatHeight - 2, Outline);
        canvas.Fill(left + 1, y + 1, ChatWidth - 2, ChatHeight - 2, Cream);
        if (side == 0)
        {
            canvas.Fill(center, y + ChatHeight, 1, 1, Outline);
        }
        else
        {
            canvas.Fill(side > 0 ? left - 1 : left + ChatWidth, y + 3, 1, 1, Outline);
        }

        int x = left + 1;
        int row = y + 1;
        switch (glyph)
        {
            case 0:
                canvas.Fill(x, row + 2, 1, 1, Outline);
                canvas.Fill(x + 2, row + 2, 1, 1, Outline);
                canvas.Fill(x + 4, row + 2, 1, 1, Outline);
                break;
            case 1:
                canvas.Fill(x + 2, row, 1, 2, Outline);
                canvas.Fill(x + 2, row + 3, 1, 1, Outline);
                break;
            case NoteGlyph:
                canvas.Fill(x + 3, row, 1, 3, Outline);
                canvas.Fill(x + 4, row, 1, 1, Outline);
                canvas.Fill(x + 1, row + 2, 2, 2, Outline);
                break;
            default:
                PaintHeart(canvas, x + 1, row + 1);
                break;
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

    public static void PaintHeart(PixelCanvas canvas, int x, int y)
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
