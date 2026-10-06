using IdleBar.Trade;

namespace IdleBar.Ui;

public static class BiomeText
{
    public static string Describe(Biome biome) => biome switch
    {
        Biome.Coast => "côte",
        Biome.Forest => "forêt",
        Biome.Marsh => "marais",
        Biome.Mountain => "montagne",
        Biome.Desert => "désert",
        _ => "plaine",
    };
}
