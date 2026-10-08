namespace IdleBar.Inn;

public sealed class TapStation : Station
{
    public const float PerfectFrom = 0.8f;
    public const float Overflow = 1.3f;
    private const float FillSeconds = 2f;

    public override Drink Drink => Drink.Beer;

    public override string Hint => Ready is not null ? "Bière prête, en attente" : "Maintiens le clic pour tirer";

    public override bool Active => Pouring || Level > 0;

    public override float Progress => Level / Overflow;

    public override float ZoneStart => PerfectFrom / Overflow;

    public override float ZoneEnd => 1f / Overflow;

    public float Level { get; private set; }

    public bool Pouring { get; private set; }

    public override void Press()
    {
        if (Ready is null)
        {
            Pouring = true;
        }
    }

    public override void Release()
    {
        if (!Pouring)
        {
            return;
        }

        Pouring = false;
        if (Level >= PerfectFrom)
        {
            Fill();
        }
    }

    public override void Update(float delta)
    {
        if (!Pouring)
        {
            return;
        }

        Level += delta / FillSeconds;
        if (Level >= Overflow)
        {
            Pouring = false;
            Fill();
        }
    }

    private void Fill()
    {
        bool perfect = Level <= 1f;
        Level = 0;
        Complete(new PreparedDrink(Drink.Beer, perfect));
    }
}
