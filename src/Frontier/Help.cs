using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Help
{
    public static double GoldFactor(FrontierState state) => 1 + state.Boosts.Sum(boost => boost.Gold) / 100.0;

    public static double DamageFactor(FrontierState state) => 1 + state.Boosts.Sum(boost => boost.Damage) / 100.0;

    public static void Sync(FrontierState state, IReadOnlyList<Ally> allies, IReadOnlyList<Boost> boosts, EventLog events)
    {
        state.Allies.RemoveAll(ally => allies.All(incoming => incoming.Id != ally.Id));
        foreach (Ally incoming in allies.Where(ally => ally.Left > 0))
        {
            if (state.Allies.Find(ally => ally.Id == incoming.Id) is Ally known)
            {
                known.Left = incoming.Left;
                continue;
            }

            state.Allies.Add(incoming);
            events.Add(new AllyArrived(incoming.Id, incoming.Look.Name, incoming.Role));
        }

        foreach (Boost boost in boosts.Where(boost => boost.Left > 0 && state.Boosts.All(known => known.Source != boost.Source)))
        {
            events.Add(new Boosted(boost.Source, boost.Gold, boost.Damage));
        }

        state.Boosts.Clear();
        state.Boosts.AddRange(boosts.Where(boost => boost.Left > 0));
    }

    public static void Elapse(FrontierState state, double seconds, EventLog events)
    {
        foreach (Ally ally in state.Allies)
        {
            ally.Left -= seconds;
            if (ally.Left <= 0)
            {
                events.Add(new AllyLeft(ally.Id, ally.Look.Name));
            }
        }

        state.Allies.RemoveAll(ally => ally.Left <= 0);
        foreach (Boost boost in state.Boosts)
        {
            boost.Left -= seconds;
        }

        state.Boosts.RemoveAll(boost => boost.Left <= 0);
    }
}
