using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class Tavern
{
    private const int StartingSeats = 4;
    private const float MinDrinking = 18f;
    private const float MaxDrinking = 30f;
    private const double SecondRoundChance = 0.4;
    private const int MaxRounds = 2;

    private readonly Random _random;
    private readonly List<Patron> _patrons = [];
    private readonly List<Station> _stations = [];
    private readonly ServiceDesk _desk = new();
    private readonly Arrivals _arrivals;
    private readonly GuestRoster _roster = new();
    private int _seats = StartingSeats;
    private float _boost;
    private int _width = TavernLayout.MinWidth;

    public Tavern(Random random)
    {
        _random = random;
        _arrivals = new Arrivals(random);
        Helper = new CounterHelper(random);
        Configure(StartingSeats, DrinkMenu.Starters, 0);
    }

    public event Action<Payment>? Paid;

    public event Action<Preparation>? Prepared;

    public event Action<GuestService>? GuestServed;

    public IReadOnlyList<Station> Stations => _stations;

    public IReadOnlyList<Patron> Patrons => _patrons;

    public IReadOnlyList<SlidingDrink> Slides => _desk.Slides;

    public TavernLayout Layout { get; private set; } = null!;

    public CounterHelper Helper { get; }

    public bool Open { get; set; } = true;

    public bool Interactive { get; init; } = true;

    public bool AutoServe { get; init; }

    public IRegularBook? Book { get; set; }

    public bool Boosted => _boost > 0;

    public int Waiting => _patrons.Count(patron => patron.Phase is PatronPhase.Thinking or PatronPhase.Waiting);

    public void Configure(int seats, IReadOnlyList<Drink> menu, int helper)
    {
        Helper.Hire(helper);
        _seats = seats;
        _stations.RemoveAll(station => !menu.Contains(station.Drink));
        foreach (Drink drink in menu.Where(drink => _stations.All(station => station.Drink != drink)))
        {
            Station station = StationFactory.For(drink);
            station.Prepared += prepared => Prepared?.Invoke(new Preparation(prepared, station.X));
            _stations.Add(station);
        }

        _stations.Sort((left, right) => left.Drink.CompareTo(right.Drink));
        Dismiss(_patrons.Where(patron => patron.Seat >= seats || (patron.Seated && !menu.Contains(patron.Order))).ToList());
        Arrange(_width);
    }

    public void Expect(IReadOnlyList<GuestVisit> guests)
    {
        _arrivals.Expect(guests, _patrons);
        _roster.Apply(guests, _patrons);

        Dismiss(_patrons.Where(patron =>
            patron.Visit is long visit
            && patron.Phase is not (PatronPhase.Leaving or PatronPhase.Gone)
            && guests.All(guest => guest.Visit != visit)).ToList());
    }

    public void Arrange(int width)
    {
        _width = width;
        Layout = TavernLayout.For(width, _seats, _stations.Count);
        for (int index = 0; index < _stations.Count; index++)
        {
            _stations[index].X = Layout.StationXs[index];
        }
    }

    public void Boost(float seconds) => _boost = Math.Max(_boost, seconds);

    public void Press(int station) => _stations[station].Press();

    public void Release(int station) => _stations[station].Release();

    public void Update(float delta)
    {
        _boost = Math.Max(0, _boost - delta);
        foreach (Station station in _stations)
        {
            station.Update(delta);
        }

        if (_arrivals.Admit(delta, this) is Patron arrival)
        {
            _patrons.Add(arrival);
        }

        UpdatePatrons(delta);
        _desk.Dispatch(_stations, _patrons);
        _desk.ServeUnattended(_patrons, _stations, _roster.Served, AutoServe);
        Helper.Update(delta, Layout, _stations, _patrons, Deliver);
        _desk.Update(delta, slide => Deliver(slide.Patron, slide.Drink));
        _patrons.RemoveAll(patron => patron.Phase == PatronPhase.Gone);
    }

    public Drink OrderFor(string? regular) => Orders.Pick(regular, Book, _stations, _random);

    private void Dismiss(IEnumerable<Patron> patrons)
    {
        foreach (Patron patron in patrons)
        {
            _desk.Forget(patron);
            patron.Leave();
        }
    }

    private void UpdatePatrons(float delta)
    {
        foreach (Patron patron in _patrons)
        {
            int seat = Math.Min(patron.Seat, Layout.SeatXs.Count - 1);
            patron.Update(delta, Layout.SeatXs[seat], Layout.DoorCenter);
            if (patron.OutOfPatience)
            {
                patron.Leave();
                Pay(DrinkMenu.Parting, patron, false, null);
            }
            else if (patron.Finished)
            {
                FinishRound(patron);
            }
            else if (patron.DoneMingling)
            {
                Lounge.Part(patron);
            }
        }
    }

    private void FinishRound(Patron patron)
    {
        if (patron.Visit is not null)
        {
            patron.KeepDrinking();
            return;
        }

        if (patron.Rounds + 1 < MaxRounds && _random.NextDouble() < SecondRoundChance)
        {
            patron.OrderAgain(OrderFor(patron.Regular));
            return;
        }

        if (!Lounge.TryMingle(patron, Layout, _patrons, _random))
        {
            patron.Leave();
        }
    }

    private void Deliver(Patron patron, PreparedDrink drink)
    {
        patron.Serve(drink, MinDrinking + (float)_random.NextDouble() * (MaxDrinking - MinDrinking));
        if (patron.Visit is long visit)
        {
            if (!drink.Helped)
            {
                GuestServed?.Invoke(new GuestService(visit, drink.Perfect, (int)MathF.Round(patron.X)));
            }

            return;
        }

        int amount = DrinkMenu.Bill(drink, patron.Waited);
        Pay(Boosted ? amount * 2 : amount, patron, drink.Perfect, drink.Drink);
    }

    private void Pay(int amount, Patron patron, bool perfect, Drink? drink) =>
        Paid?.Invoke(new Payment(amount, (int)MathF.Round(patron.X), perfect, drink, drink is null ? null : patron.Regular));
}
