using System.Collections.Generic;

namespace IdleBar.Scene;

public sealed record BiomeLook(Rgba Grass, Rgba Tuft, Rgba Dirt, Rgba Pebble, Rgba FarHill, Rgba NearHill, Rgba Tree, bool Desert)
{
    public static IReadOnlyList<BiomeLook> All { get; } =
    [
        new(Palette.Green, Palette.Leaf, Palette.Wood, Palette.Bark, Palette.PicoWhite, Palette.LightGrey, Palette.Lavender, false),
        new(Palette.Leaf, Palette.Pine, Palette.Brown, Palette.Soil, Palette.LightGrey, Palette.Lavender, Palette.DarkGreen, false),
        new(Palette.Sand, Palette.Peach, Palette.Peach, Palette.Tan, Palette.PicoWhite, Palette.LightPeach, Palette.Lavender, true),
        new(Palette.White, Palette.Silver, Palette.Silver, Palette.Gray, Palette.PicoWhite, Palette.LightGrey, Palette.Lavender, false),
    ];

    public static BiomeLook Of(int biome) => All[biome % All.Count];
}
