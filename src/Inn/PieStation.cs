namespace IdleBar.Inn;

public sealed class PieStation : Station
{
    public const float BakeSeconds = 9f;

    public override Drink Drink => Drink.Pie;

    public override string Hint => (Ready, Baking) switch
    {
        (not null, _) => "Tourte prête, en attente",
        (_, true) => "Sors-la quand elle est dorée",
        _ => "Clique pour enfourner une tourte",
    };

    public override bool Active => Baking;

    public override float Progress => Baked / BakeSeconds;

    public override float ZoneStart => 0.6f;

    public override float ZoneEnd => 0.8f;

    public float Baked { get; private set; }

    public bool Baking { get; private set; }

    public override void Press()
    {
        if (Ready is not null)
        {
            return;
        }

        if (!Baking)
        {
            Baking = true;
            Baked = 0;
            return;
        }

        TakeOut();
    }

    public override void Update(float delta)
    {
        if (!Baking)
        {
            return;
        }

        Baked += delta;
        if (Baked >= BakeSeconds)
        {
            TakeOut();
        }
    }

    private void TakeOut()
    {
        bool golden = InZone;
        Baking = false;
        Baked = 0;
        Complete(new PreparedDrink(Drink.Pie, golden));
    }
}
