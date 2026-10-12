using System;

namespace IdleBar.Scene;

public static class SceneText
{
    public static string Duration(double seconds)
    {
        if (seconds >= 3600)
        {
            double hours = Math.Floor(seconds / 3600);
            int minutes = (int)Math.Floor(seconds % 3600 / 60);
            return minutes == 0 ? $"{hours} H" : $"{hours} H {minutes:00}";
        }

        return seconds >= 60 ? $"{Math.Floor(seconds / 60)} MIN" : $"{Math.Floor(seconds)} S";
    }
}
