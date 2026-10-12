using System;
using System.Globalization;

namespace IdleBar.Scene;

public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255)
{
    public static Rgba Hex(string hex) => new(
        byte.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    public Rgba Mix(Rgba other, float amount)
    {
        float t = Math.Clamp(amount, 0, 1);
        return new Rgba(Lerp(R, other.R, t), Lerp(G, other.G, t), Lerp(B, other.B, t), Lerp(A, other.A, t));
    }

    public Rgba WithAlpha(float alpha) => this with { A = (byte)Math.Clamp(alpha * 255, 0, 255) };

    private static byte Lerp(byte from, byte to, float t) => (byte)Math.Round(from + (to - from) * t);
}
