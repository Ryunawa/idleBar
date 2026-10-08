using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class CounterHelper
{
    private const float BaseSpeed = 48f;
    private const float SpeedPerLevel = 8f;
    private const float PourSeconds = 0.7f;
    private const float ServeSeconds = 0.5f;
    private const float MinRest = 2f;
    private const float MaxRest = 6f;
    private const float MinWipe = 3f;
    private const float MaxWipe = 6f;
    private const int Beside = 8;

    private readonly Random _random;
    private readonly List<HelperOrder> _orders = [];
    private float _goal;
    private float _duration;
    private float _elapsed;

    public CounterHelper(Random random)
    {
        _random = random;
    }

    public int Level { get; private set; }

    public bool Hired => Level > 0;

    public int Capacity => Level + 1;

    public float X { get; private set; } = float.NaN;

    public int Heading { get; private set; }

    public HelperTask Task { get; private set; }

    public IReadOnlyList<PreparedDrink> Tray => Task == HelperTask.Carrying ? _orders.Select(order => order.Drink).ToList() : [];

    public static float Delay(int level) => level switch
    {
        >= 3 => 20f,
        2 => 30f,
        1 => 45f,
        _ => float.MaxValue,
    };

    public void Hire(int level)
    {
        Level = level;
        if (!Hired)
        {
            Drop();
            X = float.NaN;
        }
    }

    public void Update(float delta, TavernLayout layout, IReadOnlyList<Station> stations, IReadOnlyList<Patron> patrons, Action<Patron, PreparedDrink> deliver)
    {
        if (!Hired || stations.Count == 0)
        {
            return;
        }

        if (float.IsNaN(X))
        {
            X = HelperSpots.Home(layout);
            Begin(HelperTask.Resting, Between(MinRest, MaxRest));
        }

        _elapsed += delta;
        switch (Task)
        {
            case HelperTask.Resting or HelperTask.Strolling or HelperTask.Wiping when Needy(patrons).FirstOrDefault() is Patron patron:
                _goal = StationFor(stations, patron.Order);
                Begin(HelperTask.Fetching, 0);
                break;
            case HelperTask.Resting when _elapsed >= _duration:
                _goal = HelperSpots.Wipe(layout, _random);
                Begin(HelperTask.Strolling, 0);
                break;
            case HelperTask.Strolling when Walk(delta):
                Begin(HelperTask.Wiping, Between(MinWipe, MaxWipe));
                break;
            case HelperTask.Wiping when _elapsed >= _duration:
                Begin(HelperTask.Resting, Between(MinRest, MaxRest));
                break;
            case HelperTask.Fetching when Walk(delta):
                _orders.AddRange(Needy(patrons).Take(Capacity).Select(each => new HelperOrder(each, new PreparedDrink(each.Order, false, true))));
                Begin(HelperTask.Pouring, PourSeconds * _orders.Count);
                break;
            case HelperTask.Pouring when _elapsed >= _duration:
                _orders.RemoveAll(order => !order.Patron.Wants(order.Drink.Drink));
                _orders.Sort((left, right) => left.Patron.X.CompareTo(right.Patron.X));
                foreach (HelperOrder order in _orders)
                {
                    order.Patron.Incoming = true;
                }

                Begin(_orders.Count > 0 ? HelperTask.Carrying : HelperTask.Resting, MinRest);
                break;
            case HelperTask.Carrying:
                Carry(delta, deliver);
                break;
            case HelperTask.Serving when _elapsed >= _duration:
                Begin(_orders.Count > 0 ? HelperTask.Carrying : HelperTask.Resting, Between(MinRest, MaxRest));
                break;
        }
    }

    private void Carry(float delta, Action<Patron, PreparedDrink> deliver)
    {
        foreach (HelperOrder gone in _orders.Where(order => order.Patron.Phase != PatronPhase.Waiting).ToList())
        {
            _orders.Remove(gone);
        }

        if (_orders.Count == 0)
        {
            Begin(HelperTask.Resting, MinRest);
            return;
        }

        HelperOrder next = _orders[0];
        _goal = next.Patron.X - Beside;
        if (!Walk(delta))
        {
            return;
        }

        _orders.RemoveAt(0);
        deliver(next.Patron, next.Drink);
        Begin(HelperTask.Serving, ServeSeconds);
        Heading = 1;
    }

    private IEnumerable<Patron> Needy(IReadOnlyList<Patron> patrons)
    {
        float delay = Delay(Level);
        return patrons.Where(patron => patron.Visit is null && patron.Wants(patron.Order) && patron.Waited >= delay).OrderByDescending(patron => patron.Waited);
    }

    private static int StationFor(IReadOnlyList<Station> stations, Drink drink) =>
        stations.FirstOrDefault(station => station.Drink == drink)?.X ?? stations[0].X;

    private bool Walk(float delta)
    {
        Heading = Math.Sign(_goal - X);
        X = Steps.Toward(X, _goal, (BaseSpeed + SpeedPerLevel * Level) * delta);
        return Math.Abs(X - _goal) < 0.01f;
    }

    private void Drop()
    {
        if (Task is HelperTask.Carrying or HelperTask.Serving)
        {
            foreach (HelperOrder order in _orders)
            {
                order.Patron.Incoming = false;
            }
        }

        _orders.Clear();
        Begin(HelperTask.Resting, MinRest);
    }

    private void Begin(HelperTask task, float duration)
    {
        Task = task;
        _elapsed = 0;
        _duration = duration;
        Heading = 0;
    }

    private float Between(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    private sealed record HelperOrder(Patron Patron, PreparedDrink Drink);
}
