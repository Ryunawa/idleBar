using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Buildings
{
    public static IReadOnlyList<BuildingInfo> All { get; } =
    [
        new(BuildingKind.Farm, "farm", "Ferme", "Sa basse-cour attire la poule dorée : elle passe 1 % plus souvent par niveau, jusqu'à 2 fois plus souvent."),
        new(BuildingKind.Mill, "mill", "Moulin", "Ses réserves de farine nourrissent le village et le front quand tu n'es pas là : +10 min d'absence comptée par niveau, jusqu'à 24 h."),
        new(BuildingKind.Bakery, "bakery", "Boulangerie", "Chaque convoi de pain donne +5 % de dégâts pendant 25 s, 5 au plus. Ils partent 1 % plus souvent par niveau, jusqu'à 2 fois plus."),
        new(BuildingKind.Forge, "forge", "Forge", "+3 % de dégâts et de PV du héros par niveau."),
        new(BuildingKind.Market, "market", "Marché", "+2 % d'or des monstres par niveau."),
        new(BuildingKind.Tower, "tower", "Tour du mage", "+2 % de dégâts du second compagnon par niveau."),
        new(BuildingKind.Portal, "portal", "Portail", "Importe l'or d'un autre monde : +0,5 % de tout ton or par niveau, monstres compris."),
        new(BuildingKind.Barracks, "barracks", "Caserne", "+2 % de dégâts du premier compagnon et de PV des compagnons par niveau."),
        new(BuildingKind.Chapel, "chapel", "Chapelle", "Les compétences se rechargent 2 % plus vite par niveau, jusqu'à 3 fois plus vite."),
        new(BuildingKind.Tavern, "tavern", "Taverne", "Les villageois t'encouragent : tes clics frappent 3 % plus fort par niveau."),
        new(BuildingKind.Workshop, "workshop", "Atelier", "L'équipement et l'armure coûtent 0,5 % moins cher par niveau, jusqu'à moitié prix."),
        new(BuildingKind.Belfry, "belfry", "Beffroi", "+0,5 s au chrono des boss par niveau, jusqu'à +30 s."),
    ];

    public static BuildingInfo Of(BuildingKind kind) => All[(int)kind];

    public static BuildingKind? Parse(string? id) => All.FirstOrDefault(building => building.Id == id)?.Kind;
}
