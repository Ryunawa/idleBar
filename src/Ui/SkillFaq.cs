using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public static class SkillFaq
{
    public static string Slots =>
        $"Ce sont tes trois emplacements de compétences : un clic lance la compétence placée, et un emplacement vide ouvre l'onglet « Compétences », où tu choisis ce que chacun contient. Une compétence ne va que dans un emplacement à la fois, et seules les compétences placées se lancent. Chaque compétence a sa propre recharge, qui continue de courir hors des emplacements, et une compétence que tu viens d'apprendre est prête tout de suite. En début de partie (tant qu'elle ne dépasse pas le niveau {Balance.RedistributeStage}), tu places et retires tes compétences librement : chacune garde sa recharge, mais chaque placement compte déjà pour le reste de la partie. Ensuite, une compétence placée dans un emplacement garde sa recharge la première fois de la partie, mais si elle a déjà été placée pendant cette partie, début compris, elle repart avec sa recharge complète. Échanger deux compétences déjà placées ne change pas leurs recharges, et une compétence restée en place au repli compte comme déjà placée dans la partie suivante. La chapelle accélère toutes les recharges : {Balance.ChapelBonus * 100:0} % par niveau, jusqu'à {1 + Balance.ChapelMax:0} fois plus vite.";

    public static string Tree =>
        $"Ton record donne un point de compétence après chaque boss vaincu pour la première fois (au niveau {Balance.BossEvery + 1}, {2 * Balance.BossEvery + 1}, {3 * Balance.BossEvery + 1}…), et tu gardes tes points et tes compétences au repli. L'arbre a trois branches de cinq nœuds, et chaque nœud demande le précédent de sa branche. Une compétence active coûte 1 point (Jugement, {Skills.Of(SkillKind.Judgment).Cost}) ; une amélioration coûte 1 point par niveau, jusqu'au niveau {Skills.Of(SkillKind.Thunder).MaxLevel}. Il faut {Skills.All.Sum(skill => skill.Cost * skill.MaxLevel)} points pour tout apprendre. Tu dépenses tes points libres quand tu veux ; tout redistribuer est gratuit, mais seulement en début de partie, tant qu'elle ne dépasse pas le niveau {Balance.RedistributeStage} (juste après un repli, par exemple). "
        + $"Foudre : Frappe céleste (un éclair : {Balance.SmiteHits:0} coups du héros d'un coup, {Wait(SkillKind.Smite)}), Orage (+{Balance.ThunderHits:0} coups à la Frappe céleste par niveau), Tempête (+{Balance.StormBossBonus * 100:0} % de dégâts aux boss pour toute l'équipe pendant {Lasts(SkillKind.Storm)}, {Wait(SkillKind.Storm)}), Paratonnerre (compétences de la Foudre rechargées {Balance.BranchRecharge * 100:0} % plus vite par niveau), Jugement (retire {Balance.JudgmentShare * 100:0} % des PV max du monstre, {Balance.JudgmentBossShare * 100:0} % à un boss, {Wait(SkillKind.Judgment)}). "
        + $"Fureur : Furie (dégâts ×{Balance.FuryFactor:0} pendant {Lasts(SkillKind.Fury)}, {Wait(SkillKind.Fury)}), Rage durable (+{Balance.RageSeconds:0} s de Furie par niveau), Bouclier sacré (l'équipe ne subit aucun dégât pendant {Lasts(SkillKind.HolyShield)}, {Wait(SkillKind.HolyShield)}), Cri de ralliement (relève ton héros et tes compagnons tombés à {Balance.RallyHealth * 100:0} % de leurs PV, {Wait(SkillKind.Rally)}), Ténacité (+{Balance.TenacitySeconds:0} s de Bouclier sacré par niveau). "
        + $"Fortune : Pluie d'or ({Balance.GoldRainSeconds:0} s de production d'un coup, {Wait(SkillKind.GoldRain)}), Averse (+{Balance.DownpourSeconds:0} s de production à la Pluie d'or par niveau), Moisson (or des monstres ×{Balance.HarvestGold:0} pendant {Lasts(SkillKind.Harvest)}, {Wait(SkillKind.Harvest)}), Appeau (la poule dorée arrive tout de suite, sans retarder la suivante, {Wait(SkillKind.Lure)}), Prospérité (compétences de la Fortune rechargées {Balance.BranchRecharge * 100:0} % plus vite par niveau).";

    private static string Wait(SkillKind kind) => $"recharge {Skills.Of(kind).Cooldown / 60:0} min";

    private static string Lasts(SkillKind kind) => $"{Skills.Of(kind).Duration:0} s";
}
