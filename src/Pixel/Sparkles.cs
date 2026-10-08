using Godot;

namespace IdleBar.Pixel;

public static class Sparkles
{
    private const float Rate = 8f;

    private static readonly Color Gold = PixelPalette.Resolve('L');
    private static readonly Color White = PixelPalette.Resolve('h');
    private static readonly int[] OffsetsX = [-2, 6, -1, 7, 2];
    private static readonly int[] OffsetsY = [-1, -2, -4, 1, -5];

    public static void Paint(PixelCanvas canvas, int x, int y, float time)
    {
        int frame = (int)(time * Rate);
        for (int spark = 0; spark < OffsetsX.Length; spark++)
        {
            if ((frame + spark) % 3 == 0)
            {
                continue;
            }

            Color color = (frame + spark) % 2 == 0 ? Gold : White;
            canvas.Fill(x + OffsetsX[spark], y + OffsetsY[spark], 1, 1, color);
            if ((frame + spark) % 4 == 1)
            {
                canvas.Fill(x + OffsetsX[spark] - 1, y + OffsetsY[spark], 3, 1, color with { A = 0.6f });
                canvas.Fill(x + OffsetsX[spark], y + OffsetsY[spark] - 1, 1, 3, color with { A = 0.6f });
            }
        }
    }
}
