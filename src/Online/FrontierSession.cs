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
    private int _generation;
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

    public SessionStatus Status { get; private set; } = SessionStatus.SignedOut;

    public FrontierGame? Game { get; set; }

    public void Start()
    {
        _keeper.Restore();
        if (_keeper.SignedIn)
        {
            _ = ConnectAsync(false);
        }
    }

    public Task<bool> RefreshAsync() => ConnectAsync(false);

    public Task<bool> ResumeAsync() => ConnectAsync(false);

    public void RequestSave() => _saveSoon = true;

    public void Reset()
    {
        Remember();
        _generation++;
        _busy = false;
        _saveSoon = false;
        Game = null;
        _revision = -1;
        SetStatus(SessionStatus.SignedOut);
    }

    public void Tick(double delta)
    {
        if (!_keeper.SignedIn)
        {
            return;
        }

        _clock.Advance(delta);
        if (Status == SessionStatus.Offline && _clock.SaveDue())
        {
            _clock.Saved();
            Remember();
        }

        if (_busy)
        {
            return;
        }

        if (Status == SessionStatus.Offline && _clock.RetryDue())
        {
            _ = ConnectAsync(Game is not null);
        }
        else if (Status == SessionStatus.Ready && Game is not null && (_saveSoon || _clock.SaveDue()))
        {
            _ = SaveAsync();
        }
    }

    public async Task BeginAsync()
    {
        int generation = _generation;
        string token = await _access.TokenAsync();
        FrontierReply reply = await _api.StartAsync(token, _session);
        if (!Stale(generation) && reply.Frontier is FrontierData data)
        {
            Adopt(data, null);
        }
    }

    private async Task<bool> ConnectAsync(bool gentle)
    {
        int generation = _generation;
        _busy = true;
        _clock.Attempted();
        if (Game is null && Status is SessionStatus.SignedOut or SessionStatus.Offline)
        {
            SetStatus(SessionStatus.Connecting);
        }

        try
        {
            string token = await _access.TokenAsync();
            FrontierReply reply = gentle ? await _api.RejoinAsync(token, _session) : await _api.ClaimAsync(token, _session);
            if (!Stale(generation))
            {
                Welcome(reply);
            }

            return !Stale(generation);
        }
        catch (Exception exception) when (Stale(generation) || Fails(exception))
        {
            if (!Stale(generation))
            {
                PlayOffline();
            }

            return false;
        }
        finally
        {
            if (!Stale(generation))
            {
                _busy = false;
            }
        }
    }

    private void Welcome(FrontierReply reply)
    {
        if (reply.Status == FrontierReply.Elsewhere)
        {
            Remember();
            SetStatus(SessionStatus.Elsewhere);
            Noticed?.Invoke(Away);
        }
        else if (reply.Frontier is FrontierData data)
        {
            Adopt(data, _keeper.UserId is string player ? _local.Read(player) : null);
        }
        else
        {
            SetStatus(SessionStatus.NeedsStart);
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

    private bool Stale(int generation) => generation != _generation;
}
