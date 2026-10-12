using System;

namespace IdleBar.Frontier;

public static class GoldenChicken
{
    public static double Progress(FrontierState state) => (state.Chicken ?? 0) / Balance.ChickenFlight;

    public static void Update(FrontierState state, double dt, Random random, EventLog events)
    {
        if (state.Chicken is double flight)
        {
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

    private static void Leave(FrontierState state, Random random)
    {
        state.Chicken = null;
        state.ChickenTimer = (Balance.ChickenMin + random.NextDouble() * (Balance.ChickenMax - Balance.ChickenMin)) / Economy.ChickenPace(state);
    }
}
