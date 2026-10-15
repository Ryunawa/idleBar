using System;

namespace IdleBar.Frontier;

public static class GoldenChicken
{
    public static double Progress(FrontierState state) => (state.Chicken ?? 0) / Balance.ChickenFlight;

    public static void Update(FrontierState state, double dt, Random random, EventLog events)
    {
        if (state.Chicken is double flight)
        {
            if (state.Lured)
            {
                state.ChickenTimer -= dt;
            }

            flight += dt;
            if (flight >= Balance.ChickenFlight)
            {
                Leave(state, random);
                return;
            }

            state.Chicken = flight;
            return;
        }

        state.ChickenTimer -= dt;
        if (state.ChickenTimer <= 0)
        {
            state.Chicken = 0;
            events.Add(new ChickenAppeared());
        }
    }

    public static bool Catch(FrontierState state, Random random, EventLog events)
    {
        if (state.Chicken is null)
        {
            return false;
        }

        Leave(state, random);
        state.Chickens++;
        state.FrenzyLeft = Balance.FrenzySeconds;
        events.Add(new FrenzyStarted());
        return true;
    }

    public static void Lure(FrontierState state, EventLog events)
    {
        state.Chicken = 0;
        state.Lured = true;
        events.Add(new ChickenAppeared());
    }

    private static void Leave(FrontierState state, Random random)
    {
        state.Chicken = null;
        if (!state.Lured)
        {
            state.ChickenTimer = (Balance.ChickenMin + random.NextDouble() * (Balance.ChickenMax - Balance.ChickenMin)) / Economy.ChickenPace(state);
        }

        state.Lured = false;
    }
}
