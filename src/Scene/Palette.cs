using System.Collections.Generic;

namespace IdleBar.Scene;

public static class Palette
{
    public static readonly Rgba Black = Rgba.Hex("000000");
    public static readonly Rgba DarkBlue = Rgba.Hex("1d2b53");
    public static readonly Rgba DarkPurple = Rgba.Hex("7e2553");
    public static readonly Rgba DarkGreen = Rgba.Hex("008751");
    public static readonly Rgba Brown = Rgba.Hex("ab5236");
    public static readonly Rgba DarkGrey = Rgba.Hex("5f574f");
    public static readonly Rgba LightGrey = Rgba.Hex("c2c3c7");
    public static readonly Rgba PicoWhite = Rgba.Hex("fff1e8");
    public static readonly Rgba PicoRed = Rgba.Hex("ff004d");
    public static readonly Rgba PicoOrange = Rgba.Hex("ffa300");
    public static readonly Rgba PicoYellow = Rgba.Hex("ffec27");
    public static readonly Rgba PicoGreen = Rgba.Hex("00e436");
    public static readonly Rgba PicoBlue = Rgba.Hex("29adff");
    public static readonly Rgba Lavender = Rgba.Hex("83769c");
    public static readonly Rgba PicoPink = Rgba.Hex("ff77a8");
    public static readonly Rgba LightPeach = Rgba.Hex("ffccaa");

    public static readonly Rgba Ink = Black;
    public static readonly Rgba Navy = DarkBlue;
    public static readonly Rgba Slate = DarkGrey;
    public static readonly Rgba Steel = Lavender;
    public static readonly Rgba Gray = Lavender;
    public static readonly Rgba Silver = LightGrey;
    public static readonly Rgba White = PicoWhite;
    public static readonly Rgba Red = PicoRed;
    public static readonly Rgba Wine = DarkPurple;
    public static readonly Rgba Orange = PicoOrange;
    public static readonly Rgba Gold = PicoOrange;
    public static readonly Rgba Yellow = PicoYellow;
    public static readonly Rgba Green = PicoGreen;
    public static readonly Rgba Leaf = DarkGreen;
    public static readonly Rgba Pine = DarkGreen;
    public static readonly Rgba Deep = DarkBlue;
    public static readonly Rgba Blue = PicoBlue;
    public static readonly Rgba NavyBlue = DarkBlue;
    public static readonly Rgba Cyan = PicoBlue;
    public static readonly Rgba Plum = DarkPurple;
    public static readonly Rgba Pink = PicoPink;
    public static readonly Rgba Rose = PicoPink;
    public static readonly Rgba Skin = LightPeach;
    public static readonly Rgba Tan = Brown;
    public static readonly Rgba Sand = LightPeach;
    public static readonly Rgba Peach = LightPeach;
    public static readonly Rgba Wood = Brown;
    public static readonly Rgba Bark = DarkPurple;
    public static readonly Rgba Soil = DarkGrey;
    public static readonly Rgba Brick = Brown;
    public static readonly Rgba Rust = PicoOrange;

    public static IReadOnlyList<Rgba> Pico { get; } =
    [
        Black, DarkBlue, DarkPurple, DarkGreen, Brown, DarkGrey, LightGrey, PicoWhite,
        PicoRed, PicoOrange, PicoYellow, PicoGreen, PicoBlue, Lavender, PicoPink, LightPeach,
    ];

    public static IReadOnlyDictionary<char, Rgba> Symbols { get; } = new Dictionary<char, Rgba>
    {
        ['0'] = Black, ['1'] = DarkBlue, ['2'] = DarkPurple, ['3'] = DarkGreen, ['4'] = Brown, ['5'] = DarkGrey,
        ['6'] = LightGrey, ['7'] = PicoWhite, ['8'] = PicoRed, ['9'] = PicoOrange, ['a'] = PicoYellow, ['b'] = PicoGreen,
        ['c'] = PicoBlue, ['d'] = Lavender, ['e'] = PicoPink, ['f'] = LightPeach,
    };
}
