using System.Collections.Generic;

namespace IdleBar.Scene;

public sealed record BiomeLook(Rgba Grass, Rgba Tuft, Rgba Dirt, Rgba Pebble, Rgba FarHill, Rgba NearHill, Rgba Tree, bool Desert)
{
    public static IReadOnlyList<BiomeLook> All { get; } =
    [
        new(Palette.Green, Palette.Leaf, Palette.Wood, Palette.Bark, Rgba.Hex("86aec4"), Rgba.Hex("5c9b74"), Rgba.Hex("3e7a55"), false),
        new(Palette.Leaf, Palette.Pine, Rgba.Hex("6b4a3a"), Palette.Soil, Rgba.Hex("5e7f86"), Rgba.Hex("2f6447"), Palette.Deep, false),
        new(Palette.Sand, Palette.Peach, Palette.Peach, Palette.Tan, Rgba.Hex("e3c08f"), Rgba.Hex("d9a066"), Palette.Leaf, true),
        new(Palette.White, Palette.Silver, Palette.Silver, Palette.Gray, Rgba.Hex("b7c7dc"), Rgba.Hex("e3ecf5"), Palette.Pine, false),
    ];

    public static BiomeLook Of(int biome) => All[biome % All.Count];
}
