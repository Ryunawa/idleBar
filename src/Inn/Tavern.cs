using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class Tavern
{
    private const int StartingSeats = 4;
    private const float MinDrinking = 10f;
    private const float MaxDrinking = 18f;
    private const float QuickService = 20f;
    private const double SecondRoundChance = 0.3;
    private const int MaxRounds = 2;
    private const double FavoriteChance = 0.7;

    private readonly Random _random;
    private readonly List<Patron> _patrons = [];
    private readonly List<Station> _stations = [];
    private readonly ServiceDesk _desk = new();
    private readonly Arrivals _arrivals;
    private int _seats = StartingSeats;
    private int _width = TavernLayout.MinWidth;

    public Tavern(Random random)
    {
        _random = random;
        _arrivals = new Arrivals(random);
        Configure(StartingSeats, DrinkMenu.Starters, 0);
    }

    public event Action<Payment>? Paid;

    public event Action<Preparation>? Prepared;

    public event Action<GuestService>? GuestServed;

    public IReadOnlyList<Station> Stations => _stations;

    public IReadOnlyList<Patron> Patrons => _patrons;

    public IReadOnlyList<SlidingDrink> Slides => _desk.Slides;

    public TavernLayout Layout { get; private set; } = null!;

    public int Helper { get; private set; }

    public bool Open { get; set; } = true;

    public IRegularBook? Book { get; set; }

    public int Waiting => _patrons.Count(patron => patron.Phase is PatronPhase.Thinking or PatronPhase.Waiting);

    public void Configure(int seats, IReadOnlyList<Drink> menu, int helper)
    {
        Helper = helper;
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
        Dismiss(_patrons.Where(patron => patron.Visit is long visit && patron.Awaited && guests.All(guest => guest.Visit != visit)).ToList());
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

    public void Press(int station) => _stations[station].Press();

    public void Release(int station) => _stations[station].Release();

    public void Update(float delta)
    {
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
        _desk.Help(Helper, _stations, _patrons);
        _desk.Update(delta, Deliver);
        _patrons.RemoveAll(patron => patron.Phase == PatronPhase.Gone);
    }

    public Drink OrderFor(string? regular)
    {
        if (regular is not null && Book?.Favorite(regular) is Drink favorite && _stations.Any(station => station.Drink == favorite) && _random.NextDouble() < FavoriteChance)
        {
            return favorite;
        }

        int roll = _random.Next(_stations.Sum(station => DrinkMenu.Appetite(station.Drink)));
        foreach (Station station in _stations)
        {
            roll -= DrinkMenu.Appetite(station.Drink);
            if (roll < 0)
            {
                return station.Drink;
            }
        }

        return _stations[0].Drink;
    }

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
        }
    }

    private void FinishRound(Patron patron)
    {
        if (patron.Visit is null && patron.Rounds + 1 < MaxRounds && _random.NextDouble() < SecondRoundChance)
        {
            patron.OrderAgain(OrderFor(patron.Regular));
            return;
        }

        patron.Leave();
    }

    private void Deliver(SlidingDrink slide)
    {
        Patron patron = slide.Patron;
        PreparedDrink drink = slide.Drink;
        patron.Serve(drink, MinDrinking + (float)_random.NextDouble() * (MaxDrinking - MinDrinking));
        if (patron.Visit is long visit)
        {
            GuestServed?.Invoke(new GuestService(visit, drink.Perfect, (int)MathF.Round(patron.X)));
            return;
        }

        bool quick = !drink.Helped && patron.Waited < QuickService;
        int amount = DrinkMenu.Price(drink.Drink) + (drink.Perfect ? DrinkMenu.PerfectTip : 0) + (quick ? DrinkMenu.QuickTip : 0);
        Pay(amount, patron, drink.Perfect, drink.Drink);
    }

    private void Pay(int amount, Patron patron, bool perfect, Drink? drink) =>
        Paid?.Invoke(new Payment(amount, (int)MathF.Round(patron.X), perfect, drink, drink is null ? null : patron.Regular));
}
