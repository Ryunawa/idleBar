using Godot;

namespace IdleBar.Ui;

public static class TrayIcon
{
    private const int Size = 32;
    private const float InnerRadius = 10.5f;
    private const float OuterRadius = 14.5f;

    public static Texture2D Create()
    {
        Image image = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        Vector2 center = new(Size / 2f - 0.5f, Size / 2f - 0.5f);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float distance = new Vector2(x, y).DistanceTo(center);
                Color color = distance switch
                {
                    < InnerRadius => BarPalette.Gold,
                    < OuterRadius - 1 => BarPalette.GoldDark,
                    < OuterRadius => BarPalette.GoldDark with { A = OuterRadius - distance },
                    _ => Colors.Transparent,
                };
                image.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}
