using System.Collections.Generic;

namespace IdleBar.Frontier;

public static class Skills
{
    public static IReadOnlyList<SkillInfo> All { get; } =
    [
        new(SkillKind.Smite, "Frappe céleste", 600, 0, 15, "Un éclair frappe le monstre : 25 coups du héros d'un coup."),
        new(SkillKind.Fury, "Furie", 900, 30, 25, "Dégâts ×2 pendant 30 s."),
        new(SkillKind.GoldRain, "Pluie d'or", 1_800, 0, 35, "Gagne tout de suite 30 s de production."),
    ];

    public static SkillInfo Of(SkillKind kind) => All[(int)kind];

    public static bool Unlocked(FrontierState state, SkillKind kind) => state.MaxStage >= Of(kind).Unlock;
}
