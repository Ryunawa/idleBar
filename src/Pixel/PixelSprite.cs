using System.Collections.Generic;
using System.Linq;
using Godot;

namespace IdleBar.Pixel;

public sealed class PixelSprite
{
    private const char Transparent = '.';

    private static readonly Dictionary<char, Color> NoColors = [];

    private PixelSprite? _mirrored;

    private PixelSprite(Texture2D texture, int width, int height)
    {
        Texture = texture;
        Width = width;
        Height = height;
    }

    public Texture2D Texture { get; }

    public int Width { get; }

    public int Height { get; }

    public PixelSprite Mirrored => _mirrored ??= Flip();

    public static PixelSprite Parse(params string[] rows) => Parse(NoColors, rows);

    public static PixelSprite Parse(IReadOnlyDictionary<char, Color> colors, params string[] rows)
    {
        int width = rows.Max(row => row.Length);
        Image image = Image.CreateEmpty(width, rows.Length, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        for (int y = 0; y < rows.Length; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                char symbol = rows[y][x];
                if (symbol != Transparent)
                {
                    image.SetPixel(x, y, colors.TryGetValue(symbol, out Color color) ? color : PixelPalette.Resolve(symbol));
                }
            }
        }

        return new PixelSprite(ImageTexture.CreateFromImage(image), width, rows.Length);
    }

    private PixelSprite Flip()
    {
        Image image = Texture.GetImage();
        image.FlipX();
        return new PixelSprite(ImageTexture.CreateFromImage(image), Width, Height) { _mirrored = this };
    }
}
