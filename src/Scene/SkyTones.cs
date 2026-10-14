using System.Collections.Generic;

namespace IdleBar.Scene;

public sealed record SkyTones(IReadOnlyList<Rgba> Bands, Rgba FogLight, Rgba FogDark, float Stars, bool Lit)
{
    public const float NightLine = 0.45f;
    private const float DuskLine = 0.5f;

    public static readonly int[] NightRamp = [0, 0, 1, 1, 5, 1, 13, 6, 2, 4, 9, 3, 1, 1, 2, 5];
    public static readonly int[] DuskRamp = [0, 1, 2, 3, 2, 2, 13, 15, 8, 8, 9, 3, 13, 2, 8, 9];

    private static readonly Rgba[] Night = [Rgba.Hex("120f1e"), Rgba.Hex("181a33"), Rgba.Hex("1f2442"), Rgba.Hex("283055")];
    private static readonly Rgba[] Day = [Rgba.Hex("4c9fd8"), Rgba.Hex("69b3e2"), Rgba.Hex("8cc6ea"), Rgba.Hex("b2dbf0")];
    private static readonly Rgba[] Dusk = [Rgba.Hex("2c3054"), Rgba.Hex("5a3a6e"), Rgba.Hex("a2507f"), Rgba.Hex("e8787c")];
    private static readonly Rgba Overcast = Rgba.Hex("7d8799");

    public static SkyTones For(SceneLight light)
    {
        bool wet = light.Rain || light.Snow;
        Rgba[] bands = new Rgba[Night.Length];
        for (int band = 0; band < bands.Length; band++)
        {
            Rgba color = Night[band].Mix(Day[band], light.Daylight).Mix(Dusk[band], light.Warmth * 0.8f);
            bands[band] = PicoFilter.Nearest(wet ? color.Mix(Overcast.Mix(Night[band], 1 - light.Daylight), 0.45f) : color);
        }

        Rgba fogLight = Rgba.Hex("5a6589").Mix(Rgba.Hex("e9eef6"), light.Daylight);
        Rgba fogDark = Rgba.Hex("454d6b").Mix(Rgba.Hex("cfd8e6"), light.Daylight);
        float stars = wet ? 0 : (0.5f - light.Daylight) * 2;
        return new SkyTones(bands, fogLight, fogDark, stars, light.Night || light.Warmth > DuskLine);
    }

    public Rgba Hill(Rgba day, SceneLight light) =>
        Tone(light) is IReadOnlyList<int> ramp ? PicoFilter.Ramp(day, ramp) : day;

    private static IReadOnlyList<int>? Tone(SceneLight light) =>
        light.Daylight < NightLine ? NightRamp : light.Warmth > DuskLine ? DuskRamp : null;
}
