using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Inn;

namespace IdleBar.Online;

public sealed class OnlineSession
{
    private readonly SessionKeeper _keeper;
    private readonly TavernApi _api;
    private readonly Doorbell _doorbell;
    private readonly ServiceLedger _ledger = new();
    private readonly SessionAccess _access;
    private long _lastRequest;
    private long _appliedRequest;
    private bool _syncing;
    private readonly SyncClock _clock = new();

    public OnlineSession(SessionKeeper keeper, TavernApi api, Doorbell doorbell)
    {
        _keeper = keeper;
        _api = api;
        _doorbell = doorbell;
        _access = new SessionAccess(keeper, api, doorbell);
        _doorbell.Rang += _clock.Ring;
    }

    public event Action? Changed;

    public event Action<TavernData?, TavernData>? Applied;

    public SessionStatus Status { get; private set; } = SessionStatus.SignedOut;

    public WorldData? World { get; private set; }

    public TavernData? Tavern { get; private set; }

    public bool Live => _doorbell.Connected;

    public bool Playing => Tavern is not null && Status is not (SessionStatus.SignedOut or SessionStatus.NeedsFounding);

    public long Coins => (Tavern?.Coins ?? 0) + _ledger.Coins;

    public void Start()
    {
        _keeper.Restore();
        if (_keeper.SignedIn)
        {
            _ = SyncAsync(Poll);
        }
    }

    public void Record(Payment payment)
    {
        if (Playing)
        {
            _ledger.Record(payment);
        }
    }

    public Task<bool> RefreshAsync() => SyncAsync(Poll);

    public void Reset()
    {
        _doorbell.Stop();
        _ledger.Clear();
        World = null;
        Tavern = null;
        Status = SessionStatus.SignedOut;
        Changed?.Invoke();
    }

    public void Tick(double delta)
    {
        _doorbell.Drain();
        if (!_keeper.SignedIn || _syncing || Status == SessionStatus.UpdateRequired)
        {
            return;
        }

        _clock.Advance(delta);
        bool together = Tavern is { Outing: not null } or { Guests.Count: > 0 } || Tavern?.Room is { Mine: false } or { Guests.Count: > 0 };
        if (!_ledger.Empty && _clock.ReportDue(Status))
        {
            _ = ReportAsync();
        }
        else if (_clock.PollDue(Status, together, Live))
        {
            _clock.StartPoll();
            _ = SyncAsync(Poll);
        }
    }

    public Task FlushAsync() => Status == SessionStatus.Ready && !_ledger.Empty && !_syncing ? ReportAsync() : Task.CompletedTask;

    public async Task PerformAsync(Func<string, Task<StateData>> request)
    {
        try
        {
            string token = await _access.TokenAsync();
            World ??= await _api.GetWorldAsync(token);
            long number = ++_lastRequest;
            Apply(number, await request(token));
        }
        catch (CloudAuthException exception)
        {
            GD.PushWarning($"Session refusée : {exception.Message}");
            _keeper.SignOut();
            Reset();
            throw;
        }
        catch (OutdatedClientException)
        {
            _doorbell.Stop();
            Status = SessionStatus.UpdateRequired;
            Changed?.Invoke();
            throw;
        }
        catch (Exception exception) when (TransportFailure.Matches(exception))
        {
            GD.PushWarning($"Serveur injoignable : {exception.Message}");
            Status = SessionStatus.Offline;
            Changed?.Invoke();
            throw;
        }
    }

    public async Task SendAsync(Func<string, Task<EmptyResult>> request) => await request(await _access.TokenAsync());

    public async Task<TResult> FetchAsync<TResult>(Func<string, Task<TResult>> request) => await request(await _access.TokenAsync());

    private Task<StateData> Poll(string token) => _api.SyncStateAsync(token, Tavern?.Room is { Mine: false });

    private async Task ReportAsync()
    {
        ServiceReport report = _ledger.Take();
        _clock.Reported();
        if (!await SyncAsync(token => _api.ReportAsync(token, report)) && _keeper.SignedIn)
        {
            _ledger.Restore(report);
        }
    }

    private async Task<bool> SyncAsync(Func<string, Task<StateData>> request)
    {
        _syncing = true;
        if (Status is SessionStatus.SignedOut or SessionStatus.Offline)
        {
            Status = SessionStatus.Connecting;
            Changed?.Invoke();
        }

        try
        {
            await PerformAsync(request);
            return true;
        }
        catch (ActionRefusedException exception)
        {
            GD.PushWarning($"État non rafraîchi : {exception.Message}");
            return false;
        }
        catch (Exception exception) when (exception is CloudAuthException or OutdatedClientException || TransportFailure.Matches(exception))
        {
            return false;
        }
        finally
        {
            _syncing = false;
            _clock.Synced();
        }
    }

    private void Apply(long request, StateData state)
    {
        if (request < _appliedRequest || !_keeper.SignedIn)
        {
            return;
        }

        _appliedRequest = request;
        TavernData? previous = Tavern;
        Tavern = state.Tavern;
        Status = state.Tavern is null ? SessionStatus.NeedsFounding : SessionStatus.Ready;
        Changed?.Invoke();
        if (state.Tavern is TavernData current)
        {
            Applied?.Invoke(previous, current);
        }
    }
}
