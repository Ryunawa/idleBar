using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class Arrivals
{
    private const float FirstArrival = 2f;
    private const float MinArrival = 6f;
    private const float MaxArrival = 16f;

    private readonly Random _random;
    private readonly List<GuestVisit> _guests = [];
    private float _untilArrival = FirstArrival;

    public Arrivals(Random random)
    {
        _random = random;
    }

    public void Expect(IReadOnlyList<GuestVisit> guests, IReadOnlyList<Patron> patrons)
    {
        _guests.RemoveAll(waiting => guests.All(guest => guest.Visit != waiting.Visit));
        foreach (GuestVisit guest in guests.Where(guest => _guests.All(waiting => waiting.Visit != guest.Visit) && patrons.All(patron => patron.Visit != guest.Visit)))
        {
            _guests.Add(guest);
        }
    }

    public Patron? Admit(float delta, Tavern tavern)
    {
        _untilArrival -= delta;
        List<int> free = Enumerable.Range(0, tavern.Layout.SeatXs.Count).Where(seat => tavern.Patrons.All(patron => !patron.HoldsSeat || patron.Seat != seat)).ToList();
        if (free.Count == 0 || !tavern.Open)
        {
            return null;
        }

        int seat = free[_random.Next(free.Count)];
        if (_guests.Count > 0)
        {
            GuestVisit guest = _guests[0];
            _guests.RemoveAt(0);
            Drink order = tavern.Stations.Any(station => station.Drink == guest.Order) ? guest.Order : tavern.Stations[0].Drink;
            return new Patron(_random.Next(), seat, tavern.Layout.DoorCenter, order) { Visit = guest.Visit, Guest = guest.Name, Dozing = guest.Dozing };
        }

        if (_untilArrival > 0)
        {
            return null;
        }

        _untilArrival = MinArrival + (float)_random.NextDouble() * (MaxArrival - MinArrival);
        string? regular = tavern.Book?.Pick(tavern.Patrons.Select(patron => patron.Regular).OfType<string>().ToList(), _random);
        return new Patron(_random.Next(), seat, tavern.Layout.DoorCenter, tavern.OrderFor(regular), regular);
    }
}
