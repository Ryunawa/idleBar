using System;

namespace IdleBar.Frontier;

public static class Absence
{
    public static double Apply(FrontierState state, double seconds, EventLog events)
    {
        double away = Math.Max(0, seconds);
        double counted = Math.Min(away, Economy.AbsenceCap(state));
        double gold = Economy.BaseGoldPerSecond(state) * counted;
        state.Earn(gold);
        for (int index = 0; index < state.Cooldowns.Length; index++)
        {
            state.Cooldowns[index] = Math.Max(0, state.Cooldowns[index] - away);
        }

        state.FuryLeft = 0;
        state.FrenzyLeft = 0;
        Skirmish.Restore(state);
        Help.Elapse(state, away, events);
        state.Chicken = null;
        state.ChickenTimer = Math.Max(Balance.ChickenFirst, state.ChickenTimer - away);
        Convoys.Clear(state);
        events.Add(new Returned(away, counted, gold));
        return gold;
    }
}
