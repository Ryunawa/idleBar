namespace IdleBar.Online;

public sealed class SyncClock
{
    private const double PollSeconds = 30;
    private const double VisitPollSeconds = 5;
    private const double ReportSeconds = 15;
    private const double RetrySeconds = 20;
    private const double RingGapSeconds = 2;

    private double _sincePoll;
    private double _sinceReport;
    private double _sinceRing = RingGapSeconds;
    private bool _ringPending;

    public void Ring() => _ringPending = true;

    public void Advance(double delta)
    {
        _sincePoll += delta;
        _sinceReport += delta;
        _sinceRing += delta;
    }

    public bool ReportDue(SessionStatus status) =>
        (status == SessionStatus.Ready && _sinceReport >= ReportSeconds) || (status == SessionStatus.Offline && _sincePoll >= RetrySeconds);

    public bool PollDue(SessionStatus status, bool together, bool live)
    {
        double poll = status == SessionStatus.Offline ? RetrySeconds : together && !live ? VisitPollSeconds : PollSeconds;
        return _sincePoll >= poll || (_ringPending && _sinceRing >= RingGapSeconds);
    }

    public void StartPoll()
    {
        _ringPending = false;
        _sinceRing = 0;
    }

    public void Reported() => _sinceReport = 0;

    public void Synced() => _sincePoll = 0;
}
