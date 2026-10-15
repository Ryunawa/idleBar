using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class SkillSave
{
    private static readonly SkillKind[] Legacy = [SkillKind.Smite, SkillKind.Fury, SkillKind.GoldRain];

    public static void Grant(FrontierState state, IReadOnlyList<double> cooldowns)
    {
        for (int slot = 0; slot < Legacy.Length; slot++)
        {
            if (SkillTree.Blocker(state, Legacy[slot]) is null)
            {
                state.Learned[(int)Legacy[slot]] = 1;
                state.Slots[slot] = Legacy[slot];
                state.Cooldowns[(int)Legacy[slot]] = Remaining(cooldowns, slot);
                state.PlacedThisRun[(int)Legacy[slot]] = true;
            }
        }
    }

    public static void Restore(FrontierState state, FrontierSave save)
    {
        IReadOnlyList<string> ids = save.Skills ?? [];
        IReadOnlyList<int> levels = save.SkillLevels ?? [];
        int[] wanted = new int[FrontierState.NodeCount];
        for (int index = 0; index < ids.Count && index < levels.Count; index++)
        {
            if (Skills.Parse(ids[index]) is SkillKind kind)
            {
                wanted[(int)kind] = Math.Clamp(levels[index], 0, Skills.Of(kind).MaxLevel);
            }
        }

        foreach (SkillInfo skill in Skills.All.OrderBy(skill => Skills.Position(skill.Kind)))
        {
            while (SkillTree.Level(state, skill.Kind) < wanted[(int)skill.Kind] && SkillTree.Blocker(state, skill.Kind) is null)
            {
                state.Learned[(int)skill.Kind]++;
            }
        }

        for (int index = 0; index < ids.Count; index++)
        {
            if (Skills.Parse(ids[index]) is SkillKind kind && Skills.Of(kind).Active)
            {
                state.Cooldowns[(int)kind] = Remaining(save.SkillCooldowns ?? [], index);
                state.PlacedThisRun[(int)kind] = SkillTree.Known(state, kind) && (save.Placed?.ElementAtOrDefault(index) ?? false);
            }
        }

        for (int slot = 0; slot < FrontierState.SlotCount; slot++)
        {
            if (Skills.Parse(save.Slots?.ElementAtOrDefault(slot)) is SkillKind kind && Skills.Of(kind).Active && SkillTree.Known(state, kind) && Array.IndexOf(state.Slots, kind) < 0)
            {
                state.Slots[slot] = kind;
                state.PlacedThisRun[(int)kind] = true;
            }
        }
    }

    public static IReadOnlyList<string> Ids(FrontierState state) => [.. Kept(state).Select(skill => skill.Id)];

    public static IReadOnlyList<int> Levels(FrontierState state) => [.. Kept(state).Select(skill => SkillTree.Level(state, skill.Kind))];

    public static IReadOnlyList<double> Cooldowns(FrontierState state) => [.. Kept(state).Select(skill => skill.Active ? state.Cooldowns[(int)skill.Kind] : 0)];

    public static IReadOnlyList<bool> PlacedFlags(FrontierState state) =>
        [.. Kept(state).Select(skill => SkillTree.Known(state, skill.Kind) && skill.Active && state.PlacedThisRun[(int)skill.Kind])];

    public static IReadOnlyList<double> LegacyCooldowns(FrontierState state) => [.. Legacy.Select(kind => state.Cooldowns[(int)kind])];

    public static IReadOnlyList<string?> SlotIds(FrontierState state) => [.. state.Slots.Select(kind => kind is SkillKind placed ? Skills.Of(placed).Id : null)];

    private static IEnumerable<SkillInfo> Kept(FrontierState state) =>
        Skills.All.Where(skill => SkillTree.Known(state, skill.Kind) || skill.Active && state.Cooldowns[(int)skill.Kind] > 0);

    private static double Remaining(IReadOnlyList<double> cooldowns, int index) =>
        index < cooldowns.Count && double.IsFinite(cooldowns[index]) ? Math.Max(0, cooldowns[index]) : 0;
}
