using System;
using Godot;

namespace IdleBar.Pixel;

public static class PasserbyPainter
{
    public const int PaneLeft = 1;
    public const int PaneTop = 5;
    public const int PaneWidth = WindowPainter.Width - 2;
    public const int PaneHeight = 7;

    private const float ArriveShare = 0.3f;

    private static readonly Color Frame = PixelPalette.Resolve('B');

    public static int TravelWidth => PaneWidth + PatronSprites.Width;

    public static Rect2I Pane(int windowX, int top) => new(windowX + PaneLeft, top + PaneTop, PaneWidth, PaneHeight);

    public static void Paint(PixelCanvas canvas, int top, int windowX, PatronLook look, float progress, float time)
    {
        Rect2I pane = Pane(windowX, top);
        bool lingering = progress is >= ArriveShare and < 1 - ArriveShare;
        int figure = pane.Position.X - PatronSprites.Width + (int)MathF.Round(Stroll(progress) * TravelWidth);
        int bob = lingering ? 0 : (int)(time * 6) % 2;
        int from = Math.Max(figure, pane.Position.X);
        int to = Math.Min(figure + PatronSprites.Width, pane.End.X);
        if (to <= from)
        {
            return;
        }

        PixelSprite sprite = PatronSprites.For(look, lingering ? Gaze.Front : Gaze.Right);
        Rect2I region = new(from - figure, 0, to - from, PaneHeight - bob);
        canvas.DrawRegion(sprite, from, pane.Position.Y + bob, region, Colors.White);
        canvas.Fill(windowX + WindowPainter.Width / 2, pane.Position.Y, 1, PaneHeight, Frame);
        canvas.Fill(pane.Position.X, top + PaneTop + 3, PaneWidth, 1, Frame);
    }

    private static float Stroll(float progress) => progress switch
    {
        < ArriveShare => 0.5f * progress / ArriveShare,
        < 1 - ArriveShare => 0.5f,
        _ => 0.5f + 0.5f * (progress - (1 - ArriveShare)) / ArriveShare,
    };
}
