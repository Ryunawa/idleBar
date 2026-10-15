using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Skills
{
    public const int PerBranch = 5;

    public static IReadOnlyList<SkillInfo> All { get; } =
    [
        new(SkillKind.Smite, "smite", "Frappe céleste", 1, 1, 600, 0, $"Un éclair frappe le monstre : {Balance.SmiteHits:0} coups du héros d'un coup."),
        new(SkillKind.Thunder, "thunder", "Orage", 1, 3, 0, 0, $"+{Balance.ThunderHits:0} coups du héros à la Frappe céleste par niveau."),
        new(SkillKind.Storm, "storm", "Tempête", 1, 1, 900, 20, $"Pendant 20 s, l'équipe inflige {Balance.StormBossBonus * 100:0} % de dégâts en plus aux boss."),
        new(SkillKind.LightningRod, "lightning_rod", "Paratonnerre", 1, 3, 0, 0, $"Les compétences de la Foudre se rechargent {Balance.BranchRecharge * 100:0} % plus vite par niveau."),
        new(SkillKind.Judgment, "judgment", "Jugement", 2, 1, 1_800, 0, $"Un trait de foudre retire {Balance.JudgmentShare * 100:0} % des PV max du monstre, {Balance.JudgmentBossShare * 100:0} % à un boss."),
        new(SkillKind.Fury, "fury", "Furie", 1, 1, 900, 30, $"Dégâts ×{Balance.FuryFactor:0} pendant 30 s."),
        new(SkillKind.LastingRage, "lasting_rage", "Rage durable", 1, 3, 0, 0, $"La Furie dure {Balance.RageSeconds:0} s de plus par niveau."),
        new(SkillKind.HolyShield, "holy_shield", "Bouclier sacré", 1, 1, 600, 6, "L'équipe ne subit aucun dégât pendant 6 s."),
        new(SkillKind.Rally, "rally", "Cri de ralliement", 1, 1, 900, 0, $"Relève tes combattants tombés, héros et compagnons, à {Balance.RallyHealth * 100:0} % de leurs PV."),
        new(SkillKind.Tenacity, "tenacity", "Ténacité", 1, 3, 0, 0, $"Le Bouclier sacré dure {Balance.TenacitySeconds:0} s de plus par niveau."),
        new(SkillKind.GoldRain, "gold_rain", "Pluie d'or", 1, 1, 1_800, 0, $"Gagne tout de suite {Balance.GoldRainSeconds:0} s de production."),
        new(SkillKind.Downpour, "downpour", "Averse", 1, 3, 0, 0, $"La Pluie d'or rapporte {Balance.DownpourSeconds:0} s de production de plus par niveau."),
        new(SkillKind.Harvest, "harvest", "Moisson", 1, 1, 1_200, 30, $"L'or des monstres est multiplié par {Balance.HarvestGold:0} pendant 30 s."),
        new(SkillKind.Lure, "lure", "Appeau", 1, 1, 1_800, 0, "La poule dorée arrive tout de suite."),
        new(SkillKind.Prosperity, "prosperity", "Prospérité", 1, 3, 0, 0, $"Les compétences de la Fortune se rechargent {Balance.BranchRecharge * 100:0} % plus vite par niveau."),
    ];

    public static IReadOnlyList<SkillBranch> Branches { get; } = [SkillBranch.Lightning, SkillBranch.Fury, SkillBranch.Fortune];

    public static SkillInfo Of(SkillKind kind) => All[(int)kind];

    public static IEnumerable<SkillInfo> In(SkillBranch branch) => All.Where(skill => skill.Branch == branch);

    public static SkillKind? Parse(string? id) => All.FirstOrDefault(skill => skill.Id == id)?.Kind;

    public static SkillBranch BranchOf(SkillKind kind) => (SkillBranch)((int)kind / PerBranch);

    public static int Position(SkillKind kind) => (int)kind % PerBranch + 1;

    public static SkillKind? Prerequisite(SkillKind kind) => Position(kind) > 1 ? kind - 1 : null;

    public static SkillKind? Recharger(SkillBranch branch) => branch switch
    {
        SkillBranch.Lightning => SkillKind.LightningRod,
        SkillBranch.Fortune => SkillKind.Prosperity,
        _ => null,
    };

    public static string Name(SkillBranch branch) => branch switch
    {
        SkillBranch.Lightning => "Foudre",
        SkillBranch.Fury => "Fureur",
        _ => "Fortune",
    };
}
