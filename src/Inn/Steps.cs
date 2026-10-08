using System;

namespace IdleBar.Inn;

public static class Steps
{
    public static float Toward(float from, float to, float step) =>
        Math.Abs(to - from) <= step ? to : from + Math.Sign(to - from) * step;
}
