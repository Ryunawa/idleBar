namespace IdleBar.Frontier;

public sealed class Ally
{
    public Ally(long id, HeroLook look, HeroRole role, double ratio, double left)
    {
        Id = id;
        Look = look;
        Role = role;
        Ratio = ratio;
        Left = left;
    }

    public long Id { get; }

    public HeroLook Look { get; }

    public HeroRole Role { get; }

    public double Ratio { get; }

    public double Left { get; set; }

    public double Health { get; set; } = 1;

    public double Downed { get; set; }

    public double Timer { get; set; }

    public long Shots { get; set; }
}
