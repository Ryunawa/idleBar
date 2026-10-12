using System;

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
                state.ConvoyTimer = Balance.ConvoyEvery;
                state.ConvoySequence++;
                state.Convoys.Add(new Convoy(state.ConvoySequence));
            }
        }

        for (int index = state.Convoys.Count - 1; index >= 0; index--)
        {
            Convoy convoy = state.Convoys[index];
            convoy.Time += dt;
            if (!convoy.Delivered && convoy.Time >= Balance.ConvoyTravel)
            {
                convoy.Delivered = true;
                state.Supply = Math.Min(Balance.SupplyMax, state.Supply + 1);
                state.SupplyLeft = Balance.SupplySeconds;
                events.Add(new Supplied(state.Supply));
            }

            if (convoy.Time >= Balance.ConvoyTravel * 2)
            {
                state.Convoys.RemoveAt(index);
            }
        }

        if (state.Supply > 0)
        {
            state.SupplyLeft -= dt;
            if (state.SupplyLeft <= 0)
            {
                state.Supply = 0;
            }
        }
    }

    public static void Clear(FrontierState state)
    {
        state.Convoys.Clear();
        state.Supply = 0;
        state.SupplyLeft = 0;
        state.ConvoyTimer = 2;
    }
}
