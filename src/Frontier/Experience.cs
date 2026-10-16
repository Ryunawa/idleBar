using System;

namespace IdleBar.Frontier;

public static class Experience
{
    public static double Unit(FrontierState state, int stage) =>
        (Economy.MonsterHp(stage) * Balance.XpRatio + Balance.XpFlat) * Economy.Wealth(state);

    public static double ToNext(FrontierState state, FighterKind kind) =>
        Fighters.Of(kind).Cost * Math.Pow(Balance.XpGrowth, state.Level(kind));

    public static double Share(FrontierState state, FighterKind kind) =>
        state.Level(kind) <= 0 ? 0 : Math.Clamp(state.Xp[(int)kind] / ToNext(state, kind), 0, 1);

    public static void Gain(FrontierState state, FighterKind kind, double weight, int stage, EventLog events)
    {
        int index = (int)kind;
        if (state.Level(kind) <= 0 || weight <= 0)
        {
            return;
        }

        state.Xp[index] += Unit(state, stage) * weight * (1 + Math.Min(Balance.GearXpCap, state.Gears[index].Xp)) * GuildResearch.XpFactor(state);
        int before = state.Level(kind);
        while (state.Xp[index] >= ToNext(state, kind))
        {
            state.Xp[index] -= ToNext(state, kind);
            state.Fighters[index]++;
        }

        if (state.Level(kind) > before)
        {
            events.Add(new LeveledUp(kind, state.Level(kind)));
        }
    }

    public static double Lose(FrontierState state, FighterKind kind, double share)
    {
        int index = (int)kind;
        double lost = Math.Clamp(share * ToNext(state, kind), 0, Math.Max(0, state.Xp[index]));
        state.Xp[index] -= lost;
        return lost / ToNext(state, kind);
    }
}
