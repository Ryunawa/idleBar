using System;

namespace IdleBar.Inn;

public sealed class SlidingDrink
{
    private const float Speed = 150f;

    public SlidingDrink(PreparedDrink drink, Patron patron, float x)
    {
        Drink = drink;
        Patron = patron;
        X = x;
    }

    public PreparedDrink Drink { get; }

    public Patron Patron { get; }

    public float X { get; private set; }

    public bool Arrived => Math.Abs(X - Patron.X) < 0.01f;

    public void Update(float delta) => X = Steps.Toward(X, Patron.X, Speed * delta);
}
