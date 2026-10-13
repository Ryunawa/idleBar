namespace IdleBar.Frontier;

public sealed class Boost
{
    public Boost(string source, int gold, int damage, double left)
    {
        Source = source;
        Gold = gold;
        Damage = damage;
        Left = left;
    }

    public string Source { get; }

    public int Gold { get; }

    public int Damage { get; }

    public double Left { get; set; }
}
