using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Roles
{
    public static IReadOnlyList<RoleInfo> All { get; } =
    [
        new(HeroRole.Warrior, "warrior", "Guerrier", 1, 1, 0.6, false, "Frappe fort au contact, et chaque coup retarde celui du monstre."),
        new(HeroRole.Tank, "tank", "Tank", 0.8, 2.5, 0.8, false, "Encaisse tous les coups des monstres : le reste de l'équipe est protégé."),
        new(HeroRole.Archer, "archer", "Archer", 0.9, 0.6, 0.5, true, "Tire de loin, rarement visé, et un tir sur cinq est critique."),
        new(HeroRole.Mage, "mage", "Mage", 0.95, 0.6, 2.4, true, "Un gros sort toutes les quelques secondes, plus fort contre les boss."),
        new(HeroRole.Support, "support", "Soutien", 0.75, 1, 0.8, false, "Soigne toute l'équipe et fait revenir plus vite les personnages tombés."),
    ];

    public static RoleInfo Of(HeroRole role) => All[(int)role];

    public static HeroRole? Parse(string? id) => All.FirstOrDefault(role => role.Id == id)?.Role;

    public static IReadOnlySet<HeroRole> OnFront(FrontierState state) => new HashSet<HeroRole>(state.Allies.Select(ally => ally.Role)) { state.Role };
}
