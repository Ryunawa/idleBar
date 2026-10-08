namespace IdleBar.Inn;

public sealed class TeapotStation : Station
{
    public const float GoldenFrom = 3f;
    public const float GoldenUntil = 6f;
    public const float StewedAt = 10f;

    public override Drink Drink => Drink.Tea;

    public override string Hint => (Ready, Steeping, InZone) switch
    {
        (not null, _, _) => "Thé prêt, en attente",
        (_, true, true) => "Clique maintenant !",
        (_, true, _) => "Reclique quand c'est doré",
        _ => "Clique pour infuser le thé",
    };

    public override bool Active => Steeping;

    public override float Progress => Steeped / StewedAt;

    public override float ZoneStart => GoldenFrom / StewedAt;

    public override float ZoneEnd => GoldenUntil / StewedAt;

    public float Steeped { get; private set; }

    public bool Steeping { get; private set; }

    public bool Golden => Steeped is >= GoldenFrom and <= GoldenUntil;

    public override void Press()
    {
        if (Ready is not null)
        {
            return;
        }

        if (!Steeping)
        {
            Steeping = true;
            Steeped = 0;
            return;
        }

        Pour();
    }

    public override void Update(float delta)
    {
        if (!Steeping)
        {
            return;
        }

        Steeped += delta;
        if (Steeped >= StewedAt)
        {
            Pour();
        }
    }

    private void Pour()
    {
        bool golden = Golden;
        Steeping = false;
        Steeped = 0;
        Complete(new PreparedDrink(Drink.Tea, golden));
    }
}
