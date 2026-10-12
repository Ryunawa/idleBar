namespace IdleBar.Scene;

public sealed class Villager
{
    public Villager(float x, int direction, float speed, Rgba shirt, Rgba hair, float phase)
    {
        X = x;
        Direction = direction;
        Speed = speed;
        Shirt = shirt;
        Hair = hair;
        Phase = phase;
    }

    public float X { get; set; }

    public int Direction { get; set; }

    public float Speed { get; }

    public Rgba Shirt { get; }

    public Rgba Hair { get; }

    public float Phase { get; }

    public float Pause { get; set; }
}
