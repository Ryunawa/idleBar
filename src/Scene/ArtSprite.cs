using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Scene;

public sealed class ArtSprite
{
    private const char Transparent = '.';

    private readonly Rgba?[] _pixels;
    private ArtSprite? _mirrored;

    private ArtSprite(int width, int height, Rgba?[] pixels)
    {
        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public ArtSprite Mirrored => _mirrored ??= Flip();

    public int FirstRow => Enumerable.Range(0, Height).FirstOrDefault(y => Enumerable.Range(0, Width).Any(x => this[x, y] is not null));

    public Rgba? this[int x, int y] => _pixels[y * Width + x];

    public static ArtSprite Parse(IReadOnlyList<string> rows) => Parse(rows, Palette.Symbols);

    public static ArtSprite Parse(IReadOnlyList<string> rows, IReadOnlyDictionary<char, Rgba> overrides) =>
        Parse(rows, symbol => overrides.TryGetValue(symbol, out Rgba color) ? color : Palette.Symbols[symbol]);

    public static IEnumerable<char> UnknownSymbols(IReadOnlyList<string> rows, IReadOnlyDictionary<char, Rgba>? overrides = null) =>
        rows.SelectMany(row => row)
            .Where(symbol => symbol != Transparent && !Palette.Symbols.ContainsKey(symbol) && overrides?.ContainsKey(symbol) != true)
            .Distinct();

    public ArtSprite Silhouette(Rgba color)
    {
        Rgba?[] pixels = [.. _pixels.Select(pixel => pixel is null ? (Rgba?)null : color)];
        return new ArtSprite(Width, Height, pixels);
    }

    public ArtSprite Faded(Rgba toward, float amount)
    {
        Rgba?[] pixels = [.. _pixels.Select(pixel => pixel is Rgba color ? color.Mix(toward, amount) : (Rgba?)null)];
        return new ArtSprite(Width, Height, pixels);
    }

    public ArtSprite Over(ArtSprite top)
    {
        int width = Math.Max(Width, top.Width);
        int height = Math.Max(Height, top.Height);
        Rgba?[] pixels = new Rgba?[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = top.At(x, y) ?? At(x, y);
            }
        }

        return new ArtSprite(width, height, pixels);
    }

    public ArtSprite Lying()
    {
        int[] rows = [.. Enumerable.Range(0, Height).Where(y => Enumerable.Range(0, Width).Any(x => this[x, y] is not null))];
        int[] columns = [.. Enumerable.Range(0, Width).Where(x => Enumerable.Range(0, Height).Any(y => this[x, y] is not null))];
        int length = rows[^1] - rows[0] + 1;
        int thickness = columns[^1] - columns[0] + 1;
        Rgba?[] pixels = new Rgba?[length * thickness];
        for (int y = 0; y < thickness; y++)
        {
            for (int x = 0; x < length; x++)
            {
                pixels[y * length + x] = this[columns[^1] - y, rows[0] + x];
            }
        }

        return new ArtSprite(length, thickness, pixels);
    }

    private Rgba? At(int x, int y) => x < Width && y < Height ? this[x, y] : null;

    private static ArtSprite Parse(IReadOnlyList<string> rows, Func<char, Rgba> resolve)
    {
        int width = rows.Max(row => row.Length);
        Rgba?[] pixels = new Rgba?[width * rows.Count];
        for (int y = 0; y < rows.Count; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] != Transparent)
                {
                    pixels[y * width + x] = resolve(rows[y][x]);
                }
            }
        }

        return new ArtSprite(width, rows.Count, pixels);
    }

    private ArtSprite Flip()
    {
        Rgba?[] pixels = new Rgba?[_pixels.Length];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                pixels[y * Width + x] = _pixels[y * Width + Width - 1 - x];
            }
        }

        return new ArtSprite(Width, Height, pixels) { _mirrored = this };
    }
}
