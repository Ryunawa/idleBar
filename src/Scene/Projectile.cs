namespace IdleBar.Scene;

public sealed class Projectile
{
    public Projectile(bool magic, float fromX, float fromY, float toX, float toY, float duration, double amount, bool big = false, bool holy = false)
    {
        Magic = magic;
        Big = big;
        Holy = holy;
        FromX = fromX;
        FromY = fromY;
        ToX = toX;
        ToY = toY;
        Duration = duration;
        Amount = amount;
    }

    public bool Magic { get; }

    public bool Big { get; }

    public bool Holy { get; }

    public float FromX { get; }

    public float FromY { get; }

    public float ToX { get; }

    public float ToY { get; }

    public float Duration { get; }

    public double Amount { get; }

    public float Time { get; set; }

    public bool Landed { get; set; }

    public float Progress => System.Math.Min(1, Time / Duration);
}
