using System;
using System.Collections.Generic;
using Godot;

namespace IdleBar.Pixel;

public static class PixelPalette
{
    private static readonly Dictionary<char, Color> Symbols = new()
    {
        ['k'] = new Color("14161a"),
        ['w'] = new Color("e6dcc3"),
        ['W'] = new Color("b8ad94"),
        ['b'] = new Color("7a5230"),
        ['B'] = new Color("4e331d"),
        ['o'] = new Color("a07850"),
        ['O'] = new Color("6e5034"),
        ['h'] = new Color("e6edf3"),
        ['s'] = new Color("8a96a3"),
        ['S'] = new Color("5c636d"),
        ['t'] = new Color("7d858f"),
        ['g'] = new Color("f2c14e"),
        ['a'] = new Color("f0883e"),
        ['L'] = new Color("f7d774"),
        ['v'] = new Color("2f5a1a"),
        ['V'] = new Color("4f8a2b"),
        ['l'] = new Color("7fb24a"),
        ['c'] = new Color("5b8c3a"),
        ['C'] = new Color("3f6a26"),
        ['m'] = new Color("8a8a3a"),
        ['r'] = new Color("a8473b"),
        ['R'] = new Color("7a2f27"),
        ['y'] = new Color("d8c8a0"),
        ['Y'] = new Color("a8987a"),
        ['x'] = new Color("2b2f36"),
        ['p'] = new Color("f1c27d"),
        ['q'] = new Color("4a7bd0"),
    };

    public static Color Resolve(char symbol) =>
        Symbols.TryGetValue(symbol, out Color color)
            ? color
            : throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Couleur de pixel inconnue.");
}
