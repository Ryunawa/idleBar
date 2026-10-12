using System;

namespace IdleBar.Frontier;

public static class RetreatRules
{
    public static int SoulsFor(int maxStage) =>
        maxStage < Balance.RetreatStage ? 0 : (int)Math.Floor(Math.Pow(maxStage / Balance.SoulDivisor, Balance.SoulExponent));

    public static int Apply(FrontierState state, EventLog events)
    {
        int souls = SoulsFor(state.MaxStage);
        if (souls < 1)
        {
            return 0;
        }

        state.Souls += souls;
        state.Retreats++;
        state.IntendantUnlocked = true;
        state.Gold = 0;
        state.Stage = 1;
        state.MaxStage = 1;
        state.Kills = 0;
        state.BossHeld = false;
        state.Fighters = [1, 0, 0];
        state.Buildings = new int[FrontierState.BuildingCount];
        state.Monster = null;
        state.RespawnIn = Balance.BossRetrySeconds;
        Array.Clear(state.Timers);
        state.FuryLeft = 0;
        state.FrenzyLeft = 0;
        Convoys.Clear(state);
        events.Add(new Retreated(souls, Economy.SoulMultiplier(state.Souls)));
        return souls;
    }
}
