using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Buildings
{
    public static IReadOnlyList<BuildingInfo> All { get; } =
    [
        new(BuildingKind.Farm, "Ferme", 0.8, 30, 1, "Des poules, des œufs, de l'or."),
        new(BuildingKind.Mill, "Moulin", 5, 400, 1, "Ses réserves de farine nourrissent le village quand tu n'es pas là : +10 min de production hors jeu par niveau, jusqu'à 24 h."),
        new(BuildingKind.Bakery, "Boulangerie", 25, 5_000, 11, "Envoie des convois de pain au front : +5 % de dégâts par livraison, 5 au plus."),
        new(BuildingKind.Forge, "Forge", 120, 60_000, 21, "+1 % de dégâts du héros par niveau."),
        new(BuildingKind.Market, "Marché", 600, 700_000, 31, "+2 % d'or des monstres par niveau."),
        new(BuildingKind.Tower, "Tour du mage", 1_500, 1_500_000, 41, "+3 % de dégâts de la mage par niveau."),
        new(BuildingKind.Portal, "Portail", 8_000, 25_000_000, 51, "Importe de l'or d'un autre monde."),
    ];

    public static BuildingInfo Of(BuildingKind kind) => All[(int)kind];
}
