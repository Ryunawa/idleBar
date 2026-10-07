using Godot;
using IdleBar.Trade;

namespace IdleBar.Pixel;

public static class BiomeStyles
{
    private static readonly Color Stars = new("3a4555");

    private static readonly BiomeStyle Plain = new(
        new Color("18202b"),
        Stars,
        new SilhouetteLayer(new Color("222c38"), new Color("2c3846"), 6, 2, 11f, 1, 4.3f, 0.15f),
        new SilhouetteLayer(new Color("26402a"), new Color("31522f"), 3, 1, 7f, 1, 3.1f, 0.4f),
        new Color("2c4424"),
        new Color("5a4632"),
        new Color("6e5840"),
        [ScenerySprites.LeafyTree, ScenerySprites.Bush, ScenerySprites.Bush, ScenerySprites.Fence],
        28,
        new Color("3d5e2c"),
        true);

    private static readonly BiomeStyle Forest = new(
        new Color("151d22"),
        Stars,
        new SilhouetteLayer(new Color("1b2a22"), new Color("22352a"), 9, 2, 3.2f, 2, 1.7f, 0.15f),
        new SilhouetteLayer(new Color("203526"), new Color("2a4630"), 6, 2, 2.5f, 1, 1.3f, 0.4f),
        new Color("263a20"),
        new Color("4e3c2a"),
        new Color("604a34"),
        [ScenerySprites.Pine, ScenerySprites.Pine, ScenerySprites.LeafyTree, ScenerySprites.Bush],
        14,
        new Color("35532c"),
        true);

    private static readonly BiomeStyle Mountain = new(
        new Color("171d28"),
        Stars,
        new SilhouetteLayer(new Color("29303b"), new Color("9aa4b0"), 11, 5, 9f, 2, 3.3f, 0.1f),
        new SilhouetteLayer(new Color("343a43"), new Color("454c56"), 4, 2, 5f, 1, 2.2f, 0.35f),
        new Color("3a3631"),
        new Color("57493b"),
        new Color("6b5b4a"),
        [ScenerySprites.Rock, ScenerySprites.Pine, ScenerySprites.Rock],
        26,
        new Color("4b5236"));

    private static readonly BiomeStyle Coast = new(
        new Color("172233"),
        Stars,
        new SilhouetteLayer(new Color("1d3352"), new Color("4f7fb8"), 4, 0, 1f, 1, 1.9f, 0.25f),
        new SilhouetteLayer(new Color("6b5a3e"), new Color("7f6b4a"), 2, 1, 9f, 0, 1f, 0.45f),
        new Color("7a6646"),
        new Color("5e4c34"),
        new Color("8a7552"),
        [ScenerySprites.Palm, ScenerySprites.Rock],
        34);

    private static readonly BiomeStyle Desert = new(
        new Color("1f1c24"),
        Stars,
        new SilhouetteLayer(new Color("3a3029"), new Color("4a3d33"), 5, 3, 14f, 1, 5f, 0.12f),
        new SilhouetteLayer(new Color("5a4632"), new Color("6b5640"), 2, 2, 10f, 0, 1f, 0.4f),
        new Color("7a6040"),
        new Color("5f4a33"),
        new Color("8a6e4c"),
        [ScenerySprites.Cactus, ScenerySprites.Rock, ScenerySprites.Cactus],
        40);

    private static readonly BiomeStyle Marsh = new(
        new Color("161d1e"),
        Stars,
        new SilhouetteLayer(new Color("1e2a26"), new Color("263530"), 5, 1, 6f, 1, 2.5f, 0.15f),
        new SilhouetteLayer(new Color("22332c"), new Color("2e5040"), 2, 1, 4f, 1, 1.8f, 0.4f),
        new Color("2a3626"),
        new Color("4a3f2e"),
        new Color("5c4f3a"),
        [ScenerySprites.Reeds, ScenerySprites.Reeds, ScenerySprites.LeafyTree, ScenerySprites.Bush],
        18,
        new Color("4a5a34"),
        true);

    public static BiomeStyle For(Biome biome) => biome switch
    {
        Biome.Coast => Coast,
        Biome.Forest => Forest,
        Biome.Marsh => Marsh,
        Biome.Mountain => Mountain,
        Biome.Desert => Desert,
        _ => Plain,
    };
}
