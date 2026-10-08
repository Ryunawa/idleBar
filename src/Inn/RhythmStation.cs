namespace IdleBar.Inn;

public abstract class RhythmStation : Station
{
    private const float GiveUpSeconds = 15f;

    private float _elapsed;
    private int _hits;

    public override bool Active => Beats > 0 || _elapsed > 0;

    public int Beats { get; private set; }

    public abstract int BeatsNeeded { get; }

    protected abstract float Period { get; }

    protected abstract bool PingPong { get; }

    protected abstract int HitsForPerfect { get; }

    public override float Progress
    {
        get
        {
            float phase = _elapsed % Period / Period;
            return PingPong ? 1 - System.Math.Abs(1 - 2 * phase) : phase;
        }
    }

    public override void Press()
    {
        if (Ready is not null)
        {
            return;
        }

        if (!Active)
        {
            _elapsed = 0.0001f;
            return;
        }

        _hits += InZone ? 1 : 0;
        Beats++;
        if (Beats >= BeatsNeeded)
        {
            Finish(_hits >= HitsForPerfect);
        }
    }

    public override void Update(float delta)
    {
        if (!Active)
        {
            return;
        }

        _elapsed += delta;
        if (_elapsed >= GiveUpSeconds)
        {
            Finish(false);
        }
    }

    private void Finish(bool perfect)
    {
        _elapsed = 0;
        _hits = 0;
        Beats = 0;
        Complete(new PreparedDrink(Drink, perfect));
    }
}
