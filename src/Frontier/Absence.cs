using System;

namespace IdleBar.Frontier;

public static class Absence
{
    public static double Apply(FrontierState state, double seconds, EventLog events)
    {
        double away = Math.Max(0, seconds);
        double counted = Math.Min(away, Economy.AbsenceCap(state));
        SkillCasts.Stop(state);
        state.FrenzyLeft = 0;
        Convoys.Clear(state);
        double gold = Economy.AbsenceGoldPerSecond(state) * counted;
        state.Earn(gold);
        SkillCasts.Elapse(state, away);
        Skirmish.Restore(state);
        Help.Elapse(state, away, events);
        state.Chicken = null;
        state.Lured = false;
        state.ChickenTimer = Math.Max(Balance.ChickenFirst, state.ChickenTimer - away);
        events.Add(new Returned(away, counted, gold));
        return gold;
    }
}
