namespace IdleBar.Online;

public sealed class SyncClock
{
    private const double RetrySeconds = 20;

    private double _sinceAttempt;

    public void Advance(double delta) => _sinceAttempt += delta;

    public bool RetryDue(SessionStatus status) => status == SessionStatus.Offline && _sinceAttempt >= RetrySeconds;

    public void Attempted() => _sinceAttempt = 0;
}
