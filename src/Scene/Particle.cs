namespace IdleBar.Scene;

public sealed class Particle
{
    public Particle(ParticleKind kind, float x, float y, float speedX, float speedY, float gravity, float life, Rgba color)
    {
        Kind = kind;
        X = x;
        Y = y;
        SpeedX = speedX;
        SpeedY = speedY;
        Gravity = gravity;
        Life = life;
        Max = life;
        Color = color;
    }

    public ParticleKind Kind { get; }

    public float X { get; set; }

    public float Y { get; set; }

    public float SpeedX { get; }

    public float SpeedY { get; set; }

    public float Gravity { get; }

    public float Life { get; set; }

    public float Max { get; }

    public Rgba Color { get; }
}
