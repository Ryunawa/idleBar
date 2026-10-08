namespace IdleBar.Inn;

public sealed class CiderStation : RhythmStation
{
    public override Drink Drink => Drink.Cider;

    public override int BeatsNeeded => 4;

    public override float ZoneStart => 0.62f;

    public override float ZoneEnd => 0.86f;

    public override string Hint => (Ready, Active) switch
    {
        (not null, _) => "Cidre prêt, en attente",
        (_, true) => $"Presse quand c'est doré ({Beats}/{BeatsNeeded})",
        _ => "Clique pour lancer le pressoir",
    };

    protected override float Period => 0.9f;

    protected override bool PingPong => false;

    protected override int HitsForPerfect => 3;
}
