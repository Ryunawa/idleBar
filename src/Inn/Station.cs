using System;

namespace IdleBar.Inn;

public abstract class Station
{
    public event Action<PreparedDrink>? Prepared;

    public abstract Drink Drink { get; }

    public abstract string Hint { get; }

    public abstract bool Active { get; }

    public abstract float Progress { get; }

    public abstract float ZoneStart { get; }

    public abstract float ZoneEnd { get; }

    public bool InZone => Active && Progress >= ZoneStart && Progress <= ZoneEnd;

    public PreparedDrink? Ready { get; private set; }

    public int X { get; set; }

    public abstract void Press();

    public virtual void Release()
    {
    }

    public abstract void Update(float delta);

    public PreparedDrink? Take()
    {
        PreparedDrink? ready = Ready;
        Ready = null;
        return ready;
    }

    protected void Complete(PreparedDrink drink)
    {
        Ready = drink;
        Prepared?.Invoke(drink);
    }
}
