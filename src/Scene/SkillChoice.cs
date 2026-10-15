using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public sealed record SkillChoice(SkillKind? Kind, string Name, string? Note = null)
{
    public const string Restart = "Placée maintenant, elle repartirait avec sa recharge complète : elle a déjà été placée pendant cette partie.";

    public static IReadOnlyList<SkillChoice> For(FrontierState state) =>
    [
        new(null, "Vide"),
        .. Skills.All.Where(skill => skill.Active && SkillTree.Known(state, skill.Kind)).Select(skill => Of(state, skill)),
    ];

    private static SkillChoice Of(FrontierState state, SkillInfo skill) => SkillTree.RestartsIfPlaced(state, skill.Kind)
        ? new SkillChoice(skill.Kind, $"{skill.Name} · recharge complète", Restart)
        : new SkillChoice(skill.Kind, skill.Name);
}
