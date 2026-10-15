using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class SkillText
{
    public static string Now(FrontierState state, SkillKind kind) => kind switch
    {
        SkillKind.Smite => $"Un éclair frappe le monstre : {SkillCasts.SmiteHits(state):0} coups du héros d'un coup.",
        SkillKind.Fury => $"Dégâts ×{Balance.FuryFactor:0} pendant {SkillCasts.Duration(state, kind):0} s.",
        SkillKind.HolyShield => $"L'équipe ne subit aucun dégât pendant {SkillCasts.Duration(state, kind):0} s.",
        SkillKind.GoldRain => $"Gagne tout de suite {SkillCasts.RainSeconds(state):0} s de production.",
        _ => Skills.Of(kind).Effect,
    };

    public static string Recharge(FrontierState state, SkillKind kind) => $"recharge {LaneHints.Wait(Economy.SkillCooldown(state, kind))}";

    public static string Short(FrontierState state, SkillKind kind) =>
        SkillCasts.Wait(state, kind) > 0 ? $"prête dans {LaneHints.Wait(SkillCasts.Wait(state, kind))}" : "prête";

    public static string Status(FrontierState state, SkillKind kind) => SkillCasts.Wait(state, kind) > 0
        ? $"Prête dans {LaneHints.Wait(SkillCasts.Wait(state, kind))}."
        : SkillCasts.Ready(state, kind) ? $"Prête ({Recharge(state, kind)})." : Waiting(kind);

    private static string Waiting(SkillKind kind) => kind switch
    {
        SkillKind.Rally => "Prête, mais personne n'est à terre.",
        SkillKind.Lure => "Prête, mais la poule dorée est déjà là.",
        _ => "Prête dès qu'un monstre est au contact.",
    };
}
