using System;

namespace IdleBar.Desktop;

public static class ScreenFit
{
    public const float ReferenceWidth = 1920;
    public const float ReferenceHeight = 1080;

    public static float Scale(int width, int height, float dpiScale) =>
        MathF.Max(dpiScale, MathF.Min(width / ReferenceWidth, height / ReferenceHeight));
}
