using System;

namespace IdleBar.Frontier;

public static class RetreatRules
{
    public static int TotalSouls(double lifetimeGold) =>
        lifetimeGold <= 0 ? 0 : (int)Math.Min(int.MaxValue, Math.Floor(Balance.SoulScale * Math.Pow(lifetimeGold / Balance.SoulGoldBase, Balance.SoulPower)));

    public static double GoldForSouls(int souls) =>
        Balance.SoulGoldBase * Math.Pow(souls / Balance.SoulScale, 1 / Balance.SoulPower);

    public static double GoldToNextSoul(FrontierState state) =>
        Math.Max(0, GoldForSouls(Math.Max(TotalSouls(state.LifetimeGold), state.Souls) + 1) - state.LifetimeGold);

    public static int Pending(FrontierState state) =>
        state.MaxStage < Balance.RetreatStage ? 0 : Math.Max(0, TotalSouls(state.LifetimeGold) - state.Souls);

    public static int Apply(FrontierState state, EventLog events)
    {
        int souls = Pending(state);
        if (souls < 1)
        {
            return 0;
        }

        state.Souls += souls;
        state.Retreats++;
        state.Gold = 0;
        state.Stage = 1;
        state.MaxStage = 1;
        state.Kills = 0;
        state.BossHeld = false;
        state.Fighters = [1, 0, 0];
        state.Companions = new HeroRole?[FrontierState.FighterCount - 1];
        Array.Clear(state.CritMeters);
        state.Xp = new double[FrontierState.FighterCount];
        state.Armor = 0;
        state.Built = new BuildingKind?[FrontierState.PlotCount];
        state.Levels = new int[FrontierState.PlotCount];
        state.Monster = null;
        state.RespawnIn = Balance.BossRetrySeconds;
        Array.Clear(state.Timers);
        Skirmish.Restore(state);
        state.FuryLeft = 0;
        state.FrenzyLeft = 0;
        Actives.Stagger(state);
        Array.Clear(state.Guards);
        Array.Fill(state.Gears, Gear.None);
        Array.Fill(state.Worn, GearLook.None);
        Convoys.Clear(state);
        events.Add(new Retreated(souls, Economy.SoulMultiplier(state.Souls)));
        return souls;
    }
}
