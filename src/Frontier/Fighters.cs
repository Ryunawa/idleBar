using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Fighters
{
    public static IReadOnlyList<FighterInfo> All { get; } =
    [
        new(FighterKind.Hero, "hero", "Héros", 37, 1_610, 830, 45, HeroRole.Warrior, 1, "Ton personnage. Tes clics frappent avec lui."),
        new(FighterKind.First, "archer", "Premier compagnon", 58, 1_440, 1_190, 30, HeroRole.Archer, 11, "Rejoint l'équipe après le boss du niveau 10. Choisis sa classe."),
        new(FighterKind.Second, "mage", "Second compagnon", 178, 4_600, 9_550, 30, HeroRole.Mage, 31, "Plus fort, il rejoint l'équipe après le boss du niveau 30. Choisis sa classe."),
    ];

    public static FighterInfo Of(FighterKind kind) => All[(int)kind];

    public static bool IsCompanion(FighterKind kind) => kind != FighterKind.Hero;

    public static FighterKind? Parse(string? id) => All.FirstOrDefault(fighter => fighter.Id == id)?.Kind;

    public static bool Unlocked(FrontierState state, FighterKind kind) => state.MaxStage >= Of(kind).Unlock;
}
