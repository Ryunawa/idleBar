using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class Tavern
{
    private const int Seats = 4;
    private const float FirstArrival = 2f;
    private const float MinArrival = 10f;
    private const float MaxArrival = 26f;
    private const float MinDrinking = 10f;
    private const float MaxDrinking = 18f;
    private const float QuickService = 20f;
    private const double SecondRoundChance = 0.3;
    private const int MaxRounds = 2;
    private const double BeerShare = 0.6;

    private readonly Random _random;
    private readonly List<Patron> _patrons = [];
    private readonly List<SlidingDrink> _slides = [];
    private float _untilArrival = FirstArrival;

    public Tavern(Random random, long coins, int served, int perfect)
    {
        _random = random;
        Coins = coins;
        Served = served;
        Perfect = perfect;
        Stations = [new TapStation(), new TeapotStation()];
        foreach (Station station in Stations)
        {
            station.Prepared += drink => Prepared?.Invoke(new Preparation(drink, station.X));
        }

        Arrange(TavernLayout.MinWidth);
    }

    public event Action<Payment>? Paid;

    public event Action<Preparation>? Prepared;

    public long Coins { get; private set; }

    public int Served { get; private set; }

    public int Perfect { get; private set; }

    public IReadOnlyList<Station> Stations { get; }

    public IReadOnlyList<Patron> Patrons => _patrons;

    public IReadOnlyList<SlidingDrink> Slides => _slides;

    public TavernLayout Layout { get; private set; } = null!;

    public int Waiting => _patrons.Count(patron => patron.Phase is PatronPhase.Thinking or PatronPhase.Waiting);

    public void Arrange(int width)
    {
        Layout = TavernLayout.For(width, Seats);
        for (int index = 0; index < Stations.Count; index++)
        {
            Stations[index].X = Layout.StationXs[index];
        }
    }

    public void Press(int station) => Stations[station].Press();

    public void Release(int station) => Stations[station].Release();

    public void Update(float delta)
    {
        foreach (Station station in Stations)
        {
            station.Update(delta);
        }

        UpdateArrivals(delta);
        UpdatePatrons(delta);
        Dispatch();
        UpdateSlides(delta);
        _patrons.RemoveAll(patron => patron.Phase == PatronPhase.Gone);
    }

    private void UpdateArrivals(float delta)
    {
        _untilArrival -= delta;
        if (_untilArrival > 0)
        {
            return;
        }

        List<int> free = Enumerable.Range(0, Layout.SeatXs.Count).Where(seat => _patrons.All(patron => patron.Seat != seat)).ToList();
        if (free.Count == 0)
        {
            return;
        }

        int chosen = free[_random.Next(free.Count)];
        _patrons.Add(new Patron(_random.Next(), chosen, Layout.DoorCenter, ChooseOrder()));
        _untilArrival = MinArrival + (float)_random.NextDouble() * (MaxArrival - MinArrival);
    }

    private void UpdatePatrons(float delta)
    {
        foreach (Patron patron in _patrons)
        {
            patron.Update(delta, Layout.SeatXs[patron.Seat], Layout.DoorCenter);
            if (patron.OutOfPatience)
            {
                patron.Leave();
                Pay(DrinkMenu.Parting, patron, false);
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
            patron.OrderAgain(ChooseOrder());
            return;
        }

        patron.Leave();
    }

    private void Dispatch()
    {
        foreach (Station station in Stations)
        {
            if (station.Ready is null)
            {
                continue;
            }

            Patron? patron = _patrons.Where(each => each.Wants(station.Drink)).MaxBy(each => each.Waited);
            if (patron is not null && station.Take() is PreparedDrink drink)
            {
                patron.Incoming = true;
                _slides.Add(new SlidingDrink(drink, patron, station.X));
            }
        }
    }

    private void UpdateSlides(float delta)
    {
        foreach (SlidingDrink slide in _slides)
        {
            slide.Update(delta);
            if (slide.Arrived)
            {
                Deliver(slide);
            }
        }

        _slides.RemoveAll(slide => slide.Arrived);
    }

    private void Deliver(SlidingDrink slide)
    {
        Patron patron = slide.Patron;
        bool quick = patron.Waited < QuickService;
        int amount = DrinkMenu.Price(slide.Drink.Drink) + (slide.Drink.Perfect ? DrinkMenu.PerfectTip : 0) + (quick ? DrinkMenu.QuickTip : 0);
        patron.Serve(slide.Drink, MinDrinking + (float)_random.NextDouble() * (MaxDrinking - MinDrinking));
        Served++;
        Perfect += slide.Drink.Perfect ? 1 : 0;
        Pay(amount, patron, slide.Drink.Perfect);
    }

    private void Pay(int amount, Patron patron, bool perfect)
    {
        Coins += amount;
        Paid?.Invoke(new Payment(amount, (int)MathF.Round(patron.X), perfect));
    }

    private Drink ChooseOrder() => _random.NextDouble() < BeerShare ? Drink.Beer : Drink.Tea;
}
