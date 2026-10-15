using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class FrontierSession
{
    private const string DevicePath = "user://device.id";

    private readonly SessionKeeper _keeper;
    private readonly FrontierApi _api;
    private readonly SessionAccess _access;
    private readonly SyncClock _clock = new();
    private readonly LocalCopy _local = new(ProjectSettings.GlobalizePath("user://"));
    private readonly Guid _session = DeviceId.Read(ProjectSettings.GlobalizePath(DevicePath));
    private int _revision = -1;
    private int _generation;
    private int _refusals;
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

    public event Action? Saved;

    public int SavedRetreats { get; private set; } = -1;

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

    public Task<string> TokenAsync() => _access.TokenAsync();

    public void Reset()
    {
        Remember();
        _generation++;
        _busy = false;
        _saveSoon = false;
        _refusals = 0;
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

    private bool Stale(int generation) => generation != _generation;

    private void SetStatus(SessionStatus status)
    {
        Status = status;
        Changed?.Invoke();
    }
}
