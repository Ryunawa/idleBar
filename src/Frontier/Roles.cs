using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Roles
{
    public static IReadOnlyList<RoleInfo> All { get; } =
    [
        new(HeroRole.Warrior, "warrior", "Guerrier", 1, 1, 0.6, false, "Frappe fort au contact, et un coup sur trois étourdit le monstre.",
            "Gagne son xp en étourdissant les monstres."),
        new(HeroRole.Tank, "tank", "Tank", 0.5, 3, 0.8, false, "Peu de dégâts, mais il encaisse tous les coups des monstres, amortis de 40 % : le reste de l'équipe est protégé.",
            "Gagne son xp en encaissant les coups."),
        new(HeroRole.Archer, "archer", "Archer", 1, 0.55, 0.5, true, "Tire de loin, rarement visé, et un tir sur cinq est critique.",
            "Gagne son xp avec ses tirs critiques."),
        new(HeroRole.Mage, "mage", "Mage", 1, 0.5, 0.8, true, "Trois petits sorts, puis un gros, plus fort contre les boss.",
            "Gagne son xp avec ses gros sorts."),
        new(HeroRole.Support, "support", "Soutien", 0.5, 0.9, 0.8, false, "Peu de dégâts, mais il soigne toute l'équipe et fait revenir plus vite les personnages tombés.",
            "Gagne son xp en relevant les personnages tombés."),
    ];

    public static RoleInfo Of(HeroRole role) => All[(int)role];

    public static HeroRole? Parse(string? id) => All.FirstOrDefault(role => role.Id == id)?.Role;

    public static IReadOnlySet<HeroRole> OnFront(FrontierState state) => new HashSet<HeroRole>(Team.Members(state).Select(member => Team.Role(state, member)));
}
