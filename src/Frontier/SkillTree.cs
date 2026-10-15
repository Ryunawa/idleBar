using System;
using System.Linq;

namespace IdleBar.Frontier;

public static class SkillTree
{
    public static int Points(FrontierState state) => Math.Max(0, state.BestStage - 1) / Balance.BossEvery;

    public static int Spent(FrontierState state) => Skills.All.Sum(skill => skill.Cost * state.Learned[(int)skill.Kind]);

    public static int Free(FrontierState state) => Math.Max(0, Points(state) - Spent(state));

    public static int NextPointStage(FrontierState state) => (Points(state) + 1) * Balance.BossEvery + 1;

    public static int Level(FrontierState state, SkillKind kind) => state.Learned[(int)kind];

    public static bool Known(FrontierState state, SkillKind kind) => Level(state, kind) > 0;

    public static bool CanRedistribute(FrontierState state) => state.MaxStage <= Balance.RedistributeStage;

    public static double Pace(FrontierState state, SkillKind kind) =>
        1 + Balance.BranchRecharge * (Skills.Recharger(Skills.BranchOf(kind)) is SkillKind upgrade ? Level(state, upgrade) : 0);

    public static string? Blocker(FrontierState state, SkillKind kind)
    {
        SkillInfo skill = Skills.Of(kind);
        if (Level(state, kind) >= skill.MaxLevel)
        {
            return skill.MaxLevel > 1 ? "Niveau maximum" : "Apprise";
        }

        if (Skills.Prerequisite(kind) is SkillKind before && !Known(state, before))
        {
            return $"Demande {Skills.Of(before).Name}";
        }

        return Free(state) < skill.Cost ? $"Demande {skill.Cost} point{(skill.Cost > 1 ? "s" : string.Empty)} libre{(skill.Cost > 1 ? "s" : string.Empty)}" : null;
    }

    public static bool Learn(FrontierState state, SkillKind kind, EventLog events)
    {
        if (Blocker(state, kind) is not null)
        {
            return false;
        }

        state.Learned[(int)kind]++;
        events.Add(new SkillsChanged());
        return true;
    }

    public static bool Redistribute(FrontierState state, EventLog events)
    {
        if (!CanRedistribute(state) || Spent(state) == 0)
        {
            return false;
        }

        Array.Clear(state.Learned);
        Array.Clear(state.Slots);
        Array.Clear(state.PlacedThisRun);
        events.Add(new SkillsChanged());
        return true;
    }

    public static bool RestartsIfPlaced(FrontierState state, SkillKind kind) =>
        !CanRedistribute(state) && state.PlacedThisRun[(int)kind] && Array.IndexOf(state.Slots, kind) < 0;

    public static void NewRun(FrontierState state)
    {
        Array.Clear(state.PlacedThisRun);
        foreach (SkillKind? kind in state.Slots)
        {
            if (kind is SkillKind placed)
            {
                state.PlacedThisRun[(int)placed] = true;
            }
        }
    }

    public static bool Place(FrontierState state, int slot, SkillKind? kind, EventLog events)
    {
        if (slot < 0 || slot >= FrontierState.SlotCount || state.Slots[slot] == kind
            || kind is SkillKind wanted && (!Skills.Of(wanted).Active || !Known(state, wanted)))
        {
            return false;
        }

        int from = kind is null ? -1 : Array.IndexOf(state.Slots, kind);
        if (from >= 0)
        {
            state.Slots[from] = state.Slots[slot];
        }
        else if (kind is SkillKind placed)
        {
            if (RestartsIfPlaced(state, placed))
            {
                state.Cooldowns[(int)placed] = Math.Max(state.Cooldowns[(int)placed], Economy.SkillCooldown(state, placed));
            }

            state.PlacedThisRun[(int)placed] = true;
        }

        state.Slots[slot] = kind;
        events.Add(new SkillsChanged());
        return true;
    }
}
