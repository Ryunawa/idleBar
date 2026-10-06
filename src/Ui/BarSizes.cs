using System;
using System.Linq;

namespace IdleBar.Ui;

public static class BarSizes
{
    private static readonly float[] Steps = [0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f];

    public static float Nearest(float size) => Steps.MinBy(step => Math.Abs(step - size));

    public static float Step(float size, int direction)
    {
        int index = Array.IndexOf(Steps, Nearest(size));
        return Steps[Math.Clamp(index + direction, 0, Steps.Length - 1)];
    }

    public static bool CanShrink(float size) => Nearest(size) > Steps[0];

    public static bool CanGrow(float size) => Nearest(size) < Steps[^1];

    public static string Describe(float size) => $"{(int)MathF.Round(size * 100)} %";
}
