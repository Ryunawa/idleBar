using Godot;
using IdleBar.Scene;

namespace IdleBar.Ui;

public static class SpriteTexture
{
    public static ImageTexture From(ArtSprite sprite)
    {
        byte[] data = new byte[sprite.Width * sprite.Height * 4];
        for (int y = 0; y < sprite.Height; y++)
        {
            for (int x = 0; x < sprite.Width; x++)
            {
                if (sprite[x, y] is Rgba color)
                {
                    int index = (y * sprite.Width + x) * 4;
                    data[index] = color.R;
                    data[index + 1] = color.G;
                    data[index + 2] = color.B;
                    data[index + 3] = color.A;
                }
            }
        }

        return ImageTexture.CreateFromImage(Image.CreateFromData(sprite.Width, sprite.Height, false, Image.Format.Rgba8, data));
    }
}
