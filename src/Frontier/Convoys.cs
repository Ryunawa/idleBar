namespace IdleBar.Frontier;

public static class Convoys
{
    public static void Update(FrontierState state, double dt, EventLog events)
    {
        if (state.Level(BuildingKind.Bakery) > 0)
        {
            state.ConvoyTimer -= dt;
            if (state.ConvoyTimer <= 0)
            {
                state.ConvoyTimer = Balance.ConvoyEvery / Economy.ConvoyPace(state);
                state.ConvoySequence++;
                state.Convoys.Add(new Convoy(state.ConvoySequence));
            }
        }

        for (int index = state.Rations.Count - 1; index >= 0; index--)
        {
            state.Rations[index] -= dt;
            if (state.Rations[index] <= 0)
            {
                state.Rations.RemoveAt(index);
            }
        }

        for (int index = state.Convoys.Count - 1; index >= 0; index--)
        {
            Convoy convoy = state.Convoys[index];
            convoy.Time += dt;
            if (!convoy.Delivered && convoy.Time >= Balance.ConvoyTravel)
            {
                convoy.Delivered = true;
                Deliver(state);
                events.Add(new Supplied(state.Supply));
            }

            if (convoy.Time >= Balance.ConvoyTravel * 2)
            {
                state.Convoys.RemoveAt(index);
            }
        }
    }

    public static void Clear(FrontierState state)
    {
        state.Convoys.Clear();
        state.Rations.Clear();
        state.ConvoyTimer = 2;
    }

    private static void Deliver(FrontierState state)
    {
        if (state.Rations.Count < Balance.SupplyMax)
        {
            state.Rations.Add(Balance.SupplySeconds);
            return;
        }

        int oldest = 0;
        for (int index = 1; index < state.Rations.Count; index++)
        {
            if (state.Rations[index] < state.Rations[oldest])
            {
                oldest = index;
            }
        }

        state.Rations[oldest] = Balance.SupplySeconds;
    }
}
