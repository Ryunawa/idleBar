using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Roles
{
    public static IReadOnlyList<RoleInfo> All { get; } =
    [
        new(HeroRole.Warrior, "warrior", "Guerrier", 1, 1, 0.6, false, Balance.StunCooldown,
            $"Frappe fort au contact. Passif : les monstres frappent {Percent(Balance.WarriorIntimidation)} moins souvent. Actif, toutes les {Balance.StunCooldown:0} s : un coup à {Percent(Balance.StunHitFactor)} qui étourdit le monstre {Balance.StunSeconds:0} s.",
            "Gagne surtout son xp avec son coup étourdissant ; ses autres coups en rapportent un peu."),
        new(HeroRole.Tank, "tank", "Gardien", 0.65, 3, 0.8, false, Balance.GuardCooldown,
            $"Peu de dégâts, mais trois fois plus de PV. Passif : attire les coups des monstres. Actif, toutes les {Balance.GuardCooldown:0} s : Rempart, {Percent(1 - Balance.GuardShare)} de dégâts subis en moins pendant {Balance.GuardSeconds:0} s.",
            "Gagne surtout son xp en encaissant les coups ; ses propres coups en rapportent un peu."),
        new(HeroRole.Archer, "archer", "Ranger", 0.85, 0.55, 0.5, true, Balance.VolleyCooldown,
            $"Tire de loin et se fait rarement viser. Passif : l'équipe attaque {Percent(Balance.RangerSpeed)} plus vite. Actif, toutes les {Balance.VolleyCooldown:0} s : une volée de {Balance.VolleyShots} tirs, tous critiques.",
            "Gagne surtout son xp avec sa volée ; ses autres tirs en rapportent un peu."),
        new(HeroRole.Mage, "mage", "Mage", 1, 0.5, 0.8, true, Balance.BlastCooldown,
            $"Lance ses sorts de loin. Passif : l'équipe inflige {Percent(Balance.MageBossBonus)} de dégâts en plus aux boss. Actif, toutes les {Balance.BlastCooldown:0} s : un sort à {Percent(Balance.BlastFactor)}.",
            "Gagne surtout son xp avec son grand sort ; ses autres sorts en rapportent un peu."),
        new(HeroRole.Support, "support", "Clerc", 0.5, 0.9, 0.8, false, Balance.RaiseCooldown,
            $"Peu de dégâts. Passif : l'équipe inflige {Percent(Balance.SupportBlessing)} de dégâts en plus. Actif, toutes les {Balance.RaiseCooldown:0} s : ressuscite les alliés tombés et rend à chacun {Percent(Balance.PrayerHeal)} de ses propres PV : plus il monte de niveau, plus il soigne.",
            "Gagne surtout son xp en ressuscitant ses alliés ; ses coups en rapportent un peu."),
    ];

    public static RoleInfo Of(HeroRole role) => All[(int)role];

    public static HeroRole? Parse(string? id) => All.FirstOrDefault(role => role.Id == id)?.Role;

    public static IReadOnlySet<HeroRole> OnFront(FrontierState state) => new HashSet<HeroRole>(Team.Members(state).Select(member => Team.Role(state, member)));

    private static string Percent(double ratio) => $"{ratio * 100:0} %";
}
