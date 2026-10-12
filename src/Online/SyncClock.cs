namespace IdleBar.Online;

public sealed class SyncClock
{
    private const double SaveSeconds = 30;
    private const double RetrySeconds = 20;

    private double _sinceSave;
    private double _sinceAttempt;

    public void Advance(double delta)
    {
        _sinceSave += delta;
        _sinceAttempt += delta;
    }

    public bool SaveDue() => _sinceSave >= SaveSeconds;

    public bool RetryDue() => _sinceAttempt >= RetrySeconds;

    public void Saved() => _sinceSave = 0;

    public void Attempted() => _sinceAttempt = 0;
}
