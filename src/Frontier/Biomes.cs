using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Biomes
{
    public const int Length = 50;
    public const int MonstersPerBiome = 2;

    public static IReadOnlyList<Biome> All { get; } =
    [
        new("Plaine", ["Slime", "Gobelin"]),
        new("Forêt sombre", ["Loup", "Champignon"]),
        new("Désert", ["Scorpion", "Momie"]),
        new("Toundra", ["Yéti", "Golem de glace"]),
    ];

    public static IReadOnlyList<string> Bosses { get; } = ["Ogre", "Dragon"];

    public static int IndexOf(int stage) => (stage - 1) / Length % All.Count;

    public static int LoopOf(int stage) => (stage - 1) / (Length * All.Count);

    public static int Template(int stage, int kill) => IndexOf(stage) * MonstersPerBiome + (stage + kill) % MonstersPerBiome;

    public static int BossTemplate(int stage) => stage / Balance.BossEvery % Bosses.Count;

    public static string NameOf(Monster monster) =>
        monster.Boss ? Bosses[monster.Template] : All[monster.Template / MonstersPerBiome].Monsters[monster.Template % MonstersPerBiome];
}
