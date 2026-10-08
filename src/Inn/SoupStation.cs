namespace IdleBar.Inn;

public sealed class SoupStation : RhythmStation
{
    public override Drink Drink => Drink.Soup;

    public override int BeatsNeeded => 3;

    public override float ZoneStart => 0.38f;

    public override float ZoneEnd => 0.62f;

    public override string Hint => (Ready, Active) switch
    {
        (not null, _) => "Soupe prête, en attente",
        (_, true) => $"Touille quand c'est doré ({Beats}/{BeatsNeeded})",
        _ => "Clique pour remuer la soupe",
    };

    protected override float Period => 1.4f;

    protected override bool PingPong => true;

    protected override int HitsForPerfect => 3;
}
