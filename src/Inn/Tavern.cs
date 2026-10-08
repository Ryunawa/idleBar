using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class Tavern
{
    private const int StartingSeats = 4;
    private const float FirstArrival = 2f;
    private const float MinArrival = 10f;
    private const float MaxArrival = 26f;
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
    private float _untilArrival = FirstArrival;
    private int _seats = StartingSeats;
    private int _width = TavernLayout.MinWidth;

    public Tavern(Random random)
    {
        _random = random;
        Configure(StartingSeats, DrinkMenu.Starters, 0);
    }

    public event Action<Payment>? Paid;

    public event Action<Preparation>? Prepared;

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
        foreach (Patron patron in _patrons.Where(patron => patron.Seat >= seats || (patron.Seated && !menu.Contains(patron.Order))))
        {
            _desk.Forget(patron);
            patron.Leave();
        }

        Arrange(_width);
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

        UpdateArrivals(delta);
        UpdatePatrons(delta);
        _desk.Dispatch(_stations, _patrons);
        _desk.Help(Helper, _stations, _patrons);
        _desk.Update(delta, Deliver);
        _patrons.RemoveAll(patron => patron.Phase == PatronPhase.Gone);
    }

    private void UpdateArrivals(float delta)
    {
        _untilArrival -= delta;
        if (_untilArrival > 0 || !Open)
        {
            return;
        }

        List<int> free = Enumerable.Range(0, Layout.SeatXs.Count).Where(seat => _patrons.All(patron => patron.Seat != seat)).ToList();
        if (free.Count == 0)
        {
            return;
        }

        string? regular = Book?.Pick(_patrons.Select(patron => patron.Regular).OfType<string>().ToList(), _random);
        _patrons.Add(new Patron(_random.Next(), free[_random.Next(free.Count)], Layout.DoorCenter, OrderFor(regular), regular));
        _untilArrival = MinArrival + (float)_random.NextDouble() * (MaxArrival - MinArrival);
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
        if (patron.Rounds + 1 < MaxRounds && _random.NextDouble() < SecondRoundChance)
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
        bool quick = !drink.Helped && patron.Waited < QuickService;
        int amount = DrinkMenu.Price(drink.Drink) + (drink.Perfect ? DrinkMenu.PerfectTip : 0) + (quick ? DrinkMenu.QuickTip : 0);
        patron.Serve(drink, MinDrinking + (float)_random.NextDouble() * (MaxDrinking - MinDrinking));
        Pay(amount, patron, drink.Perfect, drink.Drink);
    }

    private void Pay(int amount, Patron patron, bool perfect, Drink? drink) =>
        Paid?.Invoke(new Payment(amount, (int)MathF.Round(patron.X), perfect, drink, drink is null ? null : patron.Regular));

    private Drink OrderFor(string? regular)
    {
        if (regular is not null && Book?.Favorite(regular) is Drink favorite && _stations.Any(station => station.Drink == favorite) && _random.NextDouble() < FavoriteChance)
        {
            return favorite;
        }

        return ChooseOrder();
    }

    private Drink ChooseOrder()
    {
        int total = _stations.Sum(station => DrinkMenu.Appetite(station.Drink));
        int roll = _random.Next(total);
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
}
