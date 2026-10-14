using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace IdleBar.Scene;

public static class ArtFont
{
    public const int Height = 5;

    private const int CacheLimit = 512;

    private static readonly ConcurrentDictionary<string, string> Plain = new();

    private static readonly (int X, int Y)[] Outline = [(-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)];

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = ["010", "101", "111", "101", "101"], ['B'] = ["110", "101", "110", "101", "110"], ['C'] = ["011", "100", "100", "100", "011"],
        ['D'] = ["110", "101", "101", "101", "110"], ['E'] = ["111", "100", "110", "100", "111"], ['F'] = ["111", "100", "110", "100", "100"],
        ['G'] = ["011", "100", "101", "101", "011"], ['H'] = ["101", "101", "111", "101", "101"], ['I'] = ["111", "010", "010", "010", "111"],
        ['J'] = ["001", "001", "001", "101", "010"], ['K'] = ["101", "101", "110", "101", "101"], ['L'] = ["100", "100", "100", "100", "111"],
        ['M'] = ["10001", "11011", "10101", "10001", "10001"], ['N'] = ["1001", "1101", "1011", "1001", "1001"], ['O'] = ["010", "101", "101", "101", "010"],
        ['P'] = ["110", "101", "110", "100", "100"], ['Q'] = ["010", "101", "101", "110", "011"], ['R'] = ["110", "101", "110", "101", "101"],
        ['S'] = ["011", "100", "010", "001", "110"], ['T'] = ["111", "010", "010", "010", "010"], ['U'] = ["101", "101", "101", "101", "111"],
        ['V'] = ["101", "101", "101", "101", "010"], ['W'] = ["10001", "10001", "10101", "11011", "10001"], ['X'] = ["101", "101", "010", "101", "101"],
        ['Y'] = ["101", "101", "010", "010", "010"], ['Z'] = ["111", "001", "010", "100", "111"],
        ['0'] = ["111", "101", "101", "101", "111"], ['1'] = ["010", "110", "010", "010", "111"], ['2'] = ["111", "001", "111", "100", "111"],
        ['3'] = ["111", "001", "011", "001", "111"], ['4'] = ["101", "101", "111", "001", "001"], ['5'] = ["111", "100", "111", "001", "111"],
        ['6'] = ["111", "100", "111", "101", "111"], ['7'] = ["111", "001", "001", "010", "010"], ['8'] = ["111", "101", "111", "101", "111"],
        ['9'] = ["111", "101", "111", "001", "111"],
        [' '] = ["00", "00", "00", "00", "00"], ['.'] = ["0", "0", "0", "0", "1"], [','] = ["00", "00", "00", "01", "10"],
        [':'] = ["0", "1", "0", "1", "0"], ['!'] = ["1", "1", "1", "0", "1"], ['?'] = ["111", "001", "010", "000", "010"],
        ['\''] = ["1", "1", "0", "0", "0"], ['-'] = ["000", "000", "111", "000", "000"], ['+'] = ["000", "010", "111", "010", "000"],
        ['/'] = ["001", "001", "010", "100", "100"], ['%'] = ["101", "001", "010", "100", "101"], ['×'] = ["000", "101", "010", "101", "000"],
        ['('] = ["01", "10", "10", "10", "01"], [')'] = ["10", "01", "01", "01", "10"], ['>'] = ["100", "010", "001", "010", "100"],
        ['«'] = ["0000", "0101", "1010", "0101", "0000"], ['»'] = ["0000", "1010", "0101", "1010", "0000"], ['_'] = ["000", "000", "000", "000", "111"],
    };

    public static string Normalize(string text)
    {
        if (Plain.TryGetValue(text, out string? cached))
        {
            return cached;
        }

        if (Plain.Count >= CacheLimit)
        {
            Plain.Clear();
        }

        return Plain[text] = Strip(text);
    }

    public static bool Covers(string text) => Normalize(text).All(Glyphs.ContainsKey);

    public static int Width(string text)
    {
        string plain = Normalize(text);
        return plain.Length == 0 ? 0 : plain.Sum(symbol => Glyph(symbol)[0].Length + 1) - 1;
    }

    public static int Draw(ArtRaster raster, string text, int x, int y, Rgba color, Rgba? outline = null)
    {
        string plain = Normalize(text);
        if (outline is Rgba border)
        {
            foreach ((int dx, int dy) in Outline)
            {
                Stamp(raster, plain, x + dx, y + dy, border);
            }
        }

        return Stamp(raster, plain, x, y, color);
    }

    private static int Stamp(ArtRaster raster, string plain, int x, int y, Rgba color)
    {
        int cursor = x;
        foreach (char symbol in plain)
        {
            string[] glyph = Glyph(symbol);
            for (int row = 0; row < Height; row++)
            {
                for (int column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] == '1')
                    {
                        raster.Set(cursor + column, y + row, color);
                    }
                }
            }

            cursor += glyph[0].Length + 1;
        }

        return cursor - x - 1;
    }

    private static string Strip(string text)
    {
        StringBuilder plain = new();
        foreach (char symbol in text.Replace('’', '\'').Replace("œ", "oe").Replace("Œ", "OE").Replace("æ", "ae").Replace("Æ", "AE").Replace("ß", "ss")
            .Replace('ø', 'o').Replace('Ø', 'O').Replace('ð', 'd').Replace('Ð', 'D').Replace("þ", "th").Replace("Þ", "TH").Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(symbol) != UnicodeCategory.NonSpacingMark)
            {
                plain.Append(char.ToUpperInvariant(symbol));
            }
        }

        return plain.ToString();
    }

    private static string[] Glyph(char symbol) => Glyphs.TryGetValue(symbol, out string[]? glyph) ? glyph : Glyphs['?'];
}
