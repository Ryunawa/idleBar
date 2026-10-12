using System.Collections.Generic;

namespace IdleBar.Scene;

public static class Palette
{
    public static readonly Rgba Ink = Rgba.Hex("181425");
    public static readonly Rgba Navy = Rgba.Hex("262b44");
    public static readonly Rgba Slate = Rgba.Hex("3a4466");
    public static readonly Rgba Steel = Rgba.Hex("5a6988");
    public static readonly Rgba Gray = Rgba.Hex("8b9bb4");
    public static readonly Rgba Silver = Rgba.Hex("c0cbdc");
    public static readonly Rgba White = Rgba.Hex("ffffff");
    public static readonly Rgba Red = Rgba.Hex("e43b44");
    public static readonly Rgba Wine = Rgba.Hex("a22633");
    public static readonly Rgba Orange = Rgba.Hex("f77622");
    public static readonly Rgba Gold = Rgba.Hex("feae34");
    public static readonly Rgba Yellow = Rgba.Hex("fee761");
    public static readonly Rgba Green = Rgba.Hex("63c74d");
    public static readonly Rgba Leaf = Rgba.Hex("3e8948");
    public static readonly Rgba Pine = Rgba.Hex("265c42");
    public static readonly Rgba Deep = Rgba.Hex("193c3e");
    public static readonly Rgba Blue = Rgba.Hex("0099db");
    public static readonly Rgba NavyBlue = Rgba.Hex("124e89");
    public static readonly Rgba Cyan = Rgba.Hex("2ce8f5");
    public static readonly Rgba Plum = Rgba.Hex("68386c");
    public static readonly Rgba Pink = Rgba.Hex("b55088");
    public static readonly Rgba Rose = Rgba.Hex("f6757a");
    public static readonly Rgba Skin = Rgba.Hex("e8b796");
    public static readonly Rgba Tan = Rgba.Hex("c28569");
    public static readonly Rgba Sand = Rgba.Hex("ead4aa");
    public static readonly Rgba Peach = Rgba.Hex("e4a672");
    public static readonly Rgba Wood = Rgba.Hex("b86f50");
    public static readonly Rgba Bark = Rgba.Hex("733e39");
    public static readonly Rgba Soil = Rgba.Hex("3e2731");
    public static readonly Rgba Brick = Rgba.Hex("be4a2f");
    public static readonly Rgba Rust = Rgba.Hex("d77643");

    public static IReadOnlyDictionary<char, Rgba> Symbols { get; } = new Dictionary<char, Rgba>
    {
        ['k'] = Ink, ['n'] = Navy, ['N'] = Slate, ['d'] = Steel, ['A'] = Gray, ['a'] = Silver, ['w'] = White,
        ['r'] = Red, ['R'] = Wine, ['o'] = Orange, ['y'] = Gold, ['Y'] = Yellow,
        ['g'] = Green, ['G'] = Leaf, ['h'] = Pine, ['q'] = Deep, ['u'] = Blue, ['U'] = NavyBlue, ['c'] = Cyan,
        ['p'] = Plum, ['P'] = Pink, ['i'] = Rose, ['s'] = Skin, ['m'] = Tan, ['t'] = Sand, ['T'] = Peach,
        ['B'] = Wood, ['b'] = Bark, ['l'] = Soil, ['e'] = Brick, ['E'] = Rust,
    };
}
