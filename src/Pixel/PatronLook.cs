using System;
using Godot;

namespace IdleBar.Pixel;

public sealed record PatronLook(int Head, Color Skin, Color Hair, Color Clothes, Color Accent)
{
    private static readonly Color[] Skins = [new("f1c27d"), new("e0ac69"), new("c68642"), new("8d5524"), new("ffdbac")];
    private static readonly Color[] Hairs = [new("2b1d14"), new("5a3825"), new("a0522d"), new("e3c16f"), new("c9c9c9"), new("1c1c22")];
    private static readonly Color[] Clothing = [new("9c4040"), new("40689c"), new("557f3f"), new("7d5a96"), new("a3793f"), new("56565f"), new("3f8a86")];
    private static readonly Color[] Accents = [new("5a3a7a"), new("2f4f6f"), new("7a2f2f"), new("3f5a2f"), new("6b5a3a")];

    public static int SkinCount => Skins.Length;

    public static int HairCount => Hairs.Length;

    public static int ClothesCount => Clothing.Length;

    public static int AccentCount => Accents.Length;

    public static PatronLook Compose(int head, int skin, int hair, int clothes, int accent) => new(
        Math.Clamp(head, 0, PatronSprites.HeadCount - 1),
        Skins[Math.Clamp(skin, 0, Skins.Length - 1)],
        Hairs[Math.Clamp(hair, 0, Hairs.Length - 1)],
        Clothing[Math.Clamp(clothes, 0, Clothing.Length - 1)],
        Accents[Math.Clamp(accent, 0, Accents.Length - 1)]);

    public static PatronLook From(int seed)
    {
        Random random = new(seed);
        return new PatronLook(
            random.Next(PatronSprites.HeadCount),
            Skins[random.Next(Skins.Length)],
            Hairs[random.Next(Hairs.Length)],
            Clothing[random.Next(Clothing.Length)],
            Accents[random.Next(Accents.Length)]);
    }
}
