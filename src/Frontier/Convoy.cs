namespace IdleBar.Frontier;

public sealed class Convoy
{
    public Convoy(long id)
    {
        Id = id;
    }

    public long Id { get; }

    public double Time { get; set; }

    public bool Delivered { get; set; }

    public double Progress => Time / Balance.ConvoyTravel;
}
