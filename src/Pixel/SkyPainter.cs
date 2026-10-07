using System;
using Godot;

namespace IdleBar.Pixel;

public static class SkyPainter
{
    private const int StarDensity = 9;
    private const float TwinkleSeconds = 0.7f;
    private const int TwinkleOdds = 6;
    private const int CloudSpread = 90;
    private const int CloudMargin = 30;
    private const float CloudParallax = 0.08f;

    private static readonly Color Twinkle = new("8fa0b8");
    private static readonly Color Sun = new("f7d774");
    private static readonly Color SettingSun = new("f0883e");
    private static readonly Color Moon = new("f4f7fb");
    private static readonly Color NightCloud = new("2b3442");
    private static readonly Color DayCloud = new("b8c3ce");
    private static readonly Color DuskCloud = new("e8a07a");

    private static readonly PixelSprite SunDisc = PixelSprite.Parse(".hhh.", "hhhhh", "hhhhh", "hhhhh", ".hhh.");
    private static readonly PixelSprite MoonCrescent = PixelSprite.Parse(".hh.", "hh..", "hh..", ".hh.");

    public static void Paint(PixelCanvas canvas, BiomeStyle style, int ground, float distance, Ambience ambience)
    {
        SkyLight light = ambience.Light;
        PaintGradient(canvas, style, ground, light);
        PaintStars(canvas, style, ground, ambience);
        PaintBodies(canvas, ground, light);
        PaintClouds(canvas, ground, distance, ambience);
    }

    private static void PaintGradient(PixelCanvas canvas, BiomeStyle style, int ground, SkyLight light)
    {
        Color top = light.Sky(style.Sky);
        Color horizon = light.Horizon(style.Sky);
        for (int y = 0; y < ground; y++)
        {
            float depth = (float)y / Math.Max(ground - 1, 1);
            canvas.Fill(0, y, canvas.Width, 1, top.Lerp(horizon, depth * depth));
        }
    }

    private static void PaintStars(PixelCanvas canvas, BiomeStyle style, int ground, Ambience ambience)
    {
        float visibility = 1 - ambience.Light.Daylight;
        if (visibility <= 0)
        {
            return;
        }

        int skyHeight = Math.Max(ground - 6, 1);
        int beat = (int)(ambience.Time / TwinkleSeconds);
        for (int index = 0; index < canvas.Width / StarDensity; index++)
        {
            bool twinkling = LandscapePainter.Scatter(index * 7 + beat) % TwinkleOdds == 0;
            Color star = twinkling ? Twinkle : style.Stars;
            canvas.Fill(LandscapePainter.Scatter(index) % canvas.Width, LandscapePainter.Scatter(index + 500) % skyHeight, 1, 1, star with { A = visibility });
        }
    }

    private static void PaintBodies(PixelCanvas canvas, int ground, SkyLight light)
    {
        if (light.SunCourse >= 0)
        {
            Color tint = Sun.Lerp(SettingSun, light.Warmth);
            PaintBody(canvas, ground, light.SunCourse, SunDisc, tint);
        }
        else if (light.MoonCourse >= 0)
        {
            PaintBody(canvas, ground, light.MoonCourse, MoonCrescent, Moon);
        }
    }

    private static void PaintBody(PixelCanvas canvas, int ground, float course, PixelSprite sprite, Color tint)
    {
        int x = (int)(course * (canvas.Width + sprite.Width * 2)) - sprite.Width;
        int lowest = Math.Max(ground - 4, 2);
        int y = 1 + (int)MathF.Round((1 - MathF.Sin(MathF.PI * course)) * (lowest - 1));
        canvas.Fill(x - 1, y, sprite.Width + 2, sprite.Height, tint with { A = 0.1f });
        canvas.Fill(x, y - 1, sprite.Width, sprite.Height + 2, tint with { A = 0.1f });
        canvas.Draw(sprite, x, y, tint);
    }

    private static void PaintClouds(PixelCanvas canvas, int ground, float distance, Ambience ambience)
    {
        SkyLight light = ambience.Light;
        Color body = NightCloud.Lerp(DayCloud, light.Daylight).Lerp(DuskCloud, light.Warmth * 0.6f);
        Color belly = body.Darkened(0.12f);
        float opacity = 0.5f + 0.15f * light.Daylight;
        int span = canvas.Width + CloudMargin * 2;
        int count = canvas.Width / CloudSpread + 2;
        for (int index = 0; index < count; index++)
        {
            int width = 9 + LandscapePainter.Scatter(index + 40) % 8;
            int y = 1 + LandscapePainter.Scatter(index + 60) % Math.Max(ground / 3, 1);
            float speed = 1.1f + index % 3 * 0.35f;
            float travelled = ambience.Time * speed + distance * CloudParallax;
            int x = (int)(((LandscapePainter.Scatter(index + 80) % span - travelled) % span + span) % span) - CloudMargin;
            canvas.Fill(x + 3, y, Math.Max(width - 7, 1), 1, body with { A = opacity });
            canvas.Fill(x + 1, y + 1, width - 2, 1, body with { A = opacity });
            canvas.Fill(x, y + 2, width, 1, belly with { A = opacity });
        }
    }
}
