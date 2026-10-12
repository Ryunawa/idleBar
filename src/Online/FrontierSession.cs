using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class FrontierSession
{
    private const string LocalPath = "user://frontier.json";

    private readonly SessionKeeper _keeper;
    private readonly FrontierApi _api;
    private readonly SessionAccess _access;
    private readonly SyncClock _clock = new();
    private readonly LocalCopy _local = new(ProjectSettings.GlobalizePath(LocalPath));
    private readonly Guid _session = Guid.NewGuid();
    private int _revision = -1;
    private bool _busy;
    private bool _saveSoon;

    public FrontierSession(SessionKeeper keeper, FrontierApi api)
    {
        _keeper = keeper;
        _api = api;
        _access = new SessionAccess(keeper, api.GetRequirementsAsync);
    }

    public event Action? Changed;

    public event Action<LoadPlan>? Loaded;

    public event Action<string>? Noticed;

    public event Action<int, string?>? Gifted;

    public SessionStatus Status { get; private set; } = SessionStatus.SignedOut;

    public FrontierGame? Game { get; set; }

    public void Start()
    {
        _keeper.Restore();
        if (_keeper.SignedIn)
        {
            _ = ConnectAsync();
        }
    }

    public Task<bool> RefreshAsync() => ConnectAsync();

    public Task<bool> ResumeAsync() => ConnectAsync();

    public void RequestSave() => _saveSoon = true;

    public void Reset()
    {
        Game = null;
        _revision = -1;
        SetStatus(SessionStatus.SignedOut);
    }

    public void Tick(double delta)
    {
        if (!_keeper.SignedIn || _busy)
        {
            return;
        }

        _clock.Advance(delta);
        if (Status == SessionStatus.Offline && _clock.RetryDue())
        {
            _ = ConnectAsync();
        }
        else if (Status == SessionStatus.Ready && Game is not null && (_saveSoon || _clock.SaveDue()))
        {
            _ = SaveAsync();
        }
    }

    public async Task BeginAsync()
    {
        string token = await _access.TokenAsync();
        FrontierReply reply = await _api.StartAsync(token, _session);
        if (reply.Frontier is FrontierData data)
        {
            Adopt(data, null);
            if (data.VeteranSouls > 0)
            {
                Gifted?.Invoke(data.VeteranSouls, data.VeteranName);
            }
        }
    }

    private async Task<bool> ConnectAsync()
    {
        _busy = true;
        _clock.Attempted();
        if (Game is null && Status is SessionStatus.SignedOut or SessionStatus.Offline)
        {
            SetStatus(SessionStatus.Connecting);
        }

        try
        {
            string token = await _access.TokenAsync();
            FrontierReply reply = await _api.ClaimAsync(token, _session);
            if (reply.Frontier is not FrontierData data)
            {
                SetStatus(SessionStatus.NeedsStart);
                return true;
            }

            Adopt(data, _keeper.UserId is string player ? _local.Read(player) : null);
            return true;
        }
        catch (Exception exception) when (Fails(exception))
        {
            PlayOffline();
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    private void Adopt(FrontierData data, LocalSave? local)
    {
        if (Game is not null && data.Revision == _revision)
        {
            _saveSoon = true;
            SetStatus(SessionStatus.Ready);
            return;
        }

        LoadPlan plan = FrontierLoader.Plan(data, local, DateTimeOffset.UtcNow);
        _revision = data.Revision;
        _saveSoon = plan.Unsent;
        SetStatus(SessionStatus.Ready);
        Loaded?.Invoke(plan);
    }

    private void PlayOffline()
    {
        if (Game is not null || _keeper.UserId is not string player || _local.Read(player) is not LocalSave local)
        {
            return;
        }

        _revision = local.Revision;
        Loaded?.Invoke(FrontierLoader.Offline(local, DateTimeOffset.UtcNow));
    }

    private bool Fails(Exception exception)
    {
        switch (exception)
        {
            case CloudAuthException:
                GD.PushWarning($"Session refusée : {exception.Message}");
                _keeper.SignOut();
                Reset();
                return true;
            case OutdatedClientException:
                SetStatus(SessionStatus.UpdateRequired);
                return true;
            case ActionRefusedException:
            case not null when TransportFailure.Matches(exception):
                GD.PushWarning($"Serveur injoignable : {exception.Message}");
                SetStatus(SessionStatus.Offline);
                return true;
            default:
                return false;
        }
    }

    private void SetStatus(SessionStatus status)
    {
        Status = status;
        Changed?.Invoke();
    }
}
