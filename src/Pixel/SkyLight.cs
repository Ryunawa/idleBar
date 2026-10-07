using System;
using Godot;

namespace IdleBar.Pixel;

public readonly record struct SkyLight(float Daylight, float Warmth, float SunCourse, float MoonCourse)
{
    private const float DawnStart = 5f;
    private const float DawnEnd = 7.5f;
    private const float DuskStart = 18.5f;
    private const float DuskEnd = 21f;
    private const float NightLength = 24f - DuskEnd + DawnStart;
    private const float DayLift = 0.45f;
    private const float WarmStrength = 0.35f;

    private static readonly Color DaySky = new("46678c");
    private static readonly Color DayHorizon = new("7f98ae");
    private static readonly Color DuskSky = new("3d3a66");
    private static readonly Color DuskHorizon = new("d08453");

    public bool Lit => Daylight < 0.5f;

    public static SkyLight At(DateTime local)
    {
        float hour = (float)local.TimeOfDay.TotalHours;
        float daylight = hour switch
        {
            < DawnStart => 0,
            < DawnEnd => (hour - DawnStart) / (DawnEnd - DawnStart),
            < DuskStart => 1,
            < DuskEnd => 1 - (hour - DuskStart) / (DuskEnd - DuskStart),
            _ => 0,
        };
        float warmth = Peak(hour, DawnStart, DawnEnd) + Peak(hour, DuskStart, DuskEnd);
        float sun = hour is >= DawnStart and <= DuskEnd ? (hour - DawnStart) / (DuskEnd - DawnStart) : -1;
        float moon = sun < 0 ? (hour - DuskEnd + 24) % 24 / NightLength : -1;
        return new SkyLight(daylight, warmth, sun, moon);
    }

    public Color Shade(Color night)
    {
        float lift = 1 + DayLift * Daylight * (1 - night.Luminance);
        Color lit = new(Math.Min(1, night.R * lift), Math.Min(1, night.G * lift), Math.Min(1, night.B * lift), night.A);
        return lit.Lerp(new Color(lit.R, lit.G * 0.8f, lit.B * 0.62f, lit.A), Warmth * WarmStrength);
    }

    public Color Sky(Color night) => night.Lerp(DaySky, Daylight).Lerp(DuskSky, Warmth * 0.55f);

    public Color Horizon(Color night) => night.Lightened(0.06f).Lerp(DayHorizon, Daylight).Lerp(DuskHorizon, Warmth * 0.8f);

    private static float Peak(float hour, float start, float end)
    {
        float middle = (start + end) / 2;
        float half = (end - start) / 2;
        return Math.Max(0, 1 - Math.Abs(hour - middle) / half);
    }
}
