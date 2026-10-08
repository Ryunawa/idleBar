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
        ['q'] = new Color("4f79b8"),
        ['Q'] = new Color("2f4b7a"),
        ['d'] = new Color("2a1f19"),
        ['D'] = new Color("3a2b22"),
        ['u'] = new Color("4a3729"),
        ['e'] = new Color("d9912b"),
        ['f'] = new Color("f6ead0"),
        ['i'] = new Color("dcc888"),
        ['j'] = new Color("c98a2c"),
        ['J'] = new Color("6b3e1e"),
        ['n'] = new Color("a3adb6"),
        ['N'] = new Color("5f6872"),
        ['z'] = new Color("ffd27a"),
        ['Z'] = new Color("2c2d31"),
        ['G'] = new Color("3f7d55"),
        ['P'] = new Color("7a3550"),
        ['I'] = new Color("c4572f"),
        ['X'] = new Color("140f0c"),
    };

    public static Color Resolve(char symbol) =>
        Symbols.TryGetValue(symbol, out Color color)
            ? color
            : throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Couleur de pixel inconnue.");
}
