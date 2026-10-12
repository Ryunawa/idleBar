using System;

namespace IdleBar.Scene;

public sealed class ArtRaster
{
    public ArtRaster(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Data = new byte[Width * Height * 4];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Data { get; }

    public int OffsetY { get; set; }

    public int ClipAbove { get; set; } = int.MinValue;

    public void Clear() => Array.Clear(Data);

    public void CopyFrom(ArtRaster source) => Buffer.BlockCopy(source.Data, 0, Data, 0, Math.Min(Data.Length, source.Data.Length));

    public void Set(int x, int y, Rgba color)
    {
        int row = y + OffsetY;
        if (x >= 0 && row >= 0 && x < Width && row < Height && y >= ClipAbove)
        {
            Put((row * Width + x) * 4, color);
        }
    }

    public void Fill(int x, int y, int width, int height, Rgba color)
    {
        int left = Math.Max(0, x);
        int top = Math.Max(0, Math.Max(y, ClipAbove) + OffsetY);
        int right = Math.Min(Width, x + width);
        int bottom = Math.Min(Height, y + OffsetY + height);
        for (int row = top; row < bottom; row++)
        {
            for (int column = left; column < right; column++)
            {
                Put((row * Width + column) * 4, color);
            }
        }
    }

    public void Blit(ArtSprite sprite, int x, int y, bool mirrored = false, float alpha = 1)
    {
        ArtSprite source = mirrored ? sprite.Mirrored : sprite;
        for (int row = 0; row < source.Height; row++)
        {
            for (int column = 0; column < source.Width; column++)
            {
                if (source[column, row] is Rgba color)
                {
                    Set(x + column, y + row, alpha >= 1 ? color : color.WithAlpha(alpha * color.A / 255f));
                }
            }
        }
    }

    public void Line(int x0, int y0, int x1, int y1, Rgba color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = -Math.Abs(y1 - y0);
        int stepX = x0 < x1 ? 1 : -1;
        int stepY = y0 < y1 ? 1 : -1;
        int error = dx + dy;
        for (int guard = 0; guard < 512; guard++)
        {
            Set(x0, y0, color);
            if (x0 == x1 && y0 == y1)
            {
                return;
            }

            int doubled = 2 * error;
            if (doubled >= dy)
            {
                error += dy;
                x0 += stepX;
            }

            if (doubled <= dx)
            {
                error += dx;
                y0 += stepY;
            }
        }
    }

    public void Tint(Rgba tint, float strength)
    {
        if (strength <= 0)
        {
            return;
        }

        for (int offset = 0; offset < Data.Length; offset += 4)
        {
            if (Data[offset + 3] == 0)
            {
                continue;
            }

            Data[offset] = Mix(Data[offset], tint.R, strength);
            Data[offset + 1] = Mix(Data[offset + 1], tint.G, strength);
            Data[offset + 2] = Mix(Data[offset + 2], tint.B, strength);
        }
    }

    public void Over(ArtRaster layer, int shiftX = 0)
    {
        for (int row = 0; row < Math.Min(Height, layer.Height); row++)
        {
            for (int column = 0; column < layer.Width; column++)
            {
                int source = (row * layer.Width + column) * 4;
                byte alpha = layer.Data[source + 3];
                int x = column + shiftX;
                if (alpha == 0 || x < 0 || x >= Width)
                {
                    continue;
                }

                Put((row * Width + x) * 4, new Rgba(layer.Data[source], layer.Data[source + 1], layer.Data[source + 2], alpha));
            }
        }
    }

    private void Put(int offset, Rgba color)
    {
        if (color.A == 255)
        {
            Data[offset] = color.R;
            Data[offset + 1] = color.G;
            Data[offset + 2] = color.B;
            Data[offset + 3] = 255;
            return;
        }

        if (color.A == 0)
        {
            return;
        }

        float alpha = color.A / 255f;
        float below = Data[offset + 3] / 255f * (1 - alpha);
        float total = alpha + below;
        Data[offset] = (byte)Math.Round((color.R * alpha + Data[offset] * below) / total);
        Data[offset + 1] = (byte)Math.Round((color.G * alpha + Data[offset + 1] * below) / total);
        Data[offset + 2] = (byte)Math.Round((color.B * alpha + Data[offset + 2] * below) / total);
        Data[offset + 3] = (byte)Math.Round(total * 255);
    }

    private static byte Mix(byte from, byte to, float amount) => (byte)Math.Round(from + (to - from) * amount);
}
