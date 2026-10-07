using System;
using System.Collections.Generic;
using Godot;

namespace IdleBar.Pixel;

public static class PixelFrame
{
    public const int Scale = 2;
    private const int Center = 2;

    public static StyleBoxTexture Box(Color fill, IReadOnlyList<PixelRing> rings, int padding, Color? rivet = null, bool openBottom = false)
    {
        int size = rings.Count * 2 + Center;
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                image.SetPixel(x, y, ColorAt(x, y, size, fill, rings, openBottom));
            }
        }

        if (rivet is Color stud && rings.Count >= 3)
        {
            int at = rings.Count / 2;
            foreach ((int x, int y) in new[] { (at, at), (size - 1 - at, at), (at, size - 1 - at), (size - 1 - at, size - 1 - at) })
            {
                image.SetPixel(x, y, stud);
            }
        }

        image.Resize(size * Scale, size * Scale, Image.Interpolation.Nearest);
        int margin = rings.Count * Scale;
        int bottom = openBottom ? 0 : margin;
        return new StyleBoxTexture
        {
            Texture = ImageTexture.CreateFromImage(image),
            TextureMarginLeft = margin,
            TextureMarginTop = margin,
            TextureMarginRight = margin,
            TextureMarginBottom = margin,
            ContentMarginLeft = margin + padding,
            ContentMarginTop = margin + padding / 2,
            ContentMarginRight = margin + padding,
            ContentMarginBottom = bottom + padding / 2,
        };
    }

    public static StyleBoxTexture Raised(Color fill, Color light, Color dark, Color outline, int padding) =>
        Box(fill, [PixelRing.Solid(outline), new PixelRing(light, dark)], padding);

    public static StyleBoxTexture Inset(Color fill, Color light, Color dark, Color outline, int padding) =>
        Box(fill, [PixelRing.Solid(outline), new PixelRing(dark, light)], padding);

    public static Texture2D Arrow(Color color, bool up = false, bool small = false)
    {
        string[] rows = small ? ["#####", ".###.", "..#.."] : ["#######", ".#####.", "..###..", "...#..."];
        if (up)
        {
            Array.Reverse(rows);
        }

        Image image = Image.CreateEmpty(rows[0].Length, rows.Length, false, Image.Format.Rgba8);
        for (int y = 0; y < rows.Length; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                image.SetPixel(x, y, rows[y][x] == '#' ? color : Colors.Transparent);
            }
        }

        image.Resize(rows[0].Length * Scale, rows.Length * Scale, Image.Interpolation.Nearest);
        return ImageTexture.CreateFromImage(image);
    }

    private static Color ColorAt(int x, int y, int size, Color fill, IReadOnlyList<PixelRing> rings, bool openBottom)
    {
        int last = size - 1;
        int ring = Math.Min(Math.Min(x, y), Math.Min(last - x, openBottom ? int.MaxValue : last - y));
        if (ring >= rings.Count)
        {
            return fill;
        }

        bool corner = ring == 0 && (x == 0 || x == last) && (y == 0 || (!openBottom && y == last));
        if (corner)
        {
            return Colors.Transparent;
        }

        PixelRing band = rings[ring];
        bool topLeft = (y == ring && x != last - ring) || (x == ring && y != last - ring);
        return topLeft ? band.TopLeft : band.BottomRight;
    }
}
