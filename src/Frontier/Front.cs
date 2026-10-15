using System;

namespace IdleBar.Frontier;

public static class Front
{
    public static int Floor(int stage) => (stage - 1) / Balance.BossEvery * Balance.BossEvery + 1;

    public static bool CallBoss(FrontierState state, EventLog events)
    {
        if (!state.BossHeld)
        {
            return false;
        }

        state.BossHeld = false;
        events.Add(new BossCalled());
        state.Monster = null;
        state.RespawnIn = Balance.RespawnSeconds;
        state.Stage = (state.Stage / Balance.BossEvery + 1) * Balance.BossEvery - 1;
        Advance(state, events);
        return true;
    }

    public static void Advance(FrontierState state, EventLog events)
    {
        state.Stage++;
        state.Kills = 0;
        if (state.Stage > state.MaxStage)
        {
            state.MaxStage = state.Stage;
            if (state.Stage > state.BestStage)
            {
                state.BestStage = state.Stage;
                if (state.BestStage % Balance.BossEvery == 1)
                {
                    events.Add(new PointEarned(SkillTree.Points(state)));
                }
            }

            foreach (PlotInfo plot in Plots.All)
            {
                if (plot.Unlock == state.MaxStage)
                {
                    events.Add(new PlotConquered(plot.Index));
                }
            }

            foreach (FighterInfo fighter in Fighters.All)
            {
                if (fighter.Unlock > 1 && fighter.Unlock == state.MaxStage)
                {
                    events.Add(new CompanionReady(fighter.Kind));
                }
            }
        }

        events.Add(new StageReached(state.Stage, Economy.IsBossStage(state.Stage)));
    }

    public static void Fail(FrontierState state, EventLog events)
    {
        events.Add(new BossFailed());
        Recede(state, true);
    }

    public static void Recede(FrontierState state, bool boss)
    {
        state.Monster = null;
        state.RespawnIn = Balance.BossRetrySeconds;
        state.Kills = 0;
        state.Stage = boss ? Math.Max(1, state.Stage - 1) : Math.Max(Floor(state.Stage), state.Stage - 1);
        state.BossHeld = state.BossHeld || boss;
    }
}
