using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Inn;

namespace IdleBar.Online;

public sealed class OnlineSession
{
    private const double PollSeconds = 30;
    private const double ReportSeconds = 15;
    private const double RetrySeconds = 20;

    private readonly SessionKeeper _keeper;
    private readonly TavernApi _api;
    private readonly ServiceLedger _ledger = new();
    private long _lastRequest;
    private long _appliedRequest;
    private bool _syncing;
    private double _sincePoll;
    private double _sinceReport;

    public OnlineSession(SessionKeeper keeper, TavernApi api)
    {
        _keeper = keeper;
        _api = api;
    }

    public event Action? Changed;

    public event Action<TavernData?, TavernData>? Applied;

    public SessionStatus Status { get; private set; } = SessionStatus.SignedOut;

    public WorldData? World { get; private set; }

    public TavernData? Tavern { get; private set; }

    public string? LastEmail => _keeper.LastEmail;

    public bool Playing => Tavern is not null && Status is not (SessionStatus.SignedOut or SessionStatus.NeedsFounding);

    public long Coins => (Tavern?.Coins ?? 0) + _ledger.Coins;

    public void Start()
    {
        _keeper.Restore();
        if (_keeper.SignedIn)
        {
            _ = SyncAsync(_api.GetStateAsync);
        }
    }

    public void Record(Payment payment)
    {
        if (Playing)
        {
            _ledger.Record(payment);
        }
    }

    public async Task SignInAsync(string email, string password)
    {
        await _keeper.SignInAsync(email, password);
        await SyncAsync(_api.GetStateAsync);
    }

    public async Task<bool> SignUpAsync(string email, string password)
    {
        bool signedIn = await _keeper.SignUpAsync(email, password);
        if (signedIn)
        {
            await SyncAsync(_api.GetStateAsync);
        }

        return signedIn;
    }

    public void SignOut()
    {
        _keeper.SignOut();
        _ledger.Clear();
        World = null;
        Tavern = null;
        Status = SessionStatus.SignedOut;
        Changed?.Invoke();
    }

    public void Tick(double delta)
    {
        if (!_keeper.SignedIn || _syncing)
        {
            return;
        }

        _sincePoll += delta;
        _sinceReport += delta;
        if (Status == SessionStatus.Ready && !_ledger.Empty && _sinceReport >= ReportSeconds)
        {
            _ = ReportAsync();
        }
        else if (_sincePoll >= (Status == SessionStatus.Offline ? RetrySeconds : PollSeconds))
        {
            _ = SyncAsync(_api.GetStateAsync);
        }
    }

    public Task FlushAsync() => Status == SessionStatus.Ready && !_ledger.Empty && !_syncing ? ReportAsync() : Task.CompletedTask;

    public Task FoundAsync(string name) => PerformAsync(token => _api.FoundAsync(token, name));

    public Task BuyAsync(string upgradeId) => PerformAsync(token => _api.BuyAsync(token, upgradeId));

    public Task CollectTipJarAsync() => PerformAsync(_api.CollectTipJarAsync);

    private async Task ReportAsync()
    {
        ServiceReport report = _ledger.Take();
        _sinceReport = 0;
        bool sent = await SyncAsync(token => _api.ReportAsync(token, report));
        if (!sent)
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
        catch (Exception exception) when (exception is CloudAuthException || TransportFailure.Matches(exception))
        {
            return false;
        }
        finally
        {
            _syncing = false;
            _sincePoll = 0;
        }
    }

    private async Task PerformAsync(Func<string, Task<StateData>> request)
    {
        try
        {
            string token = await _keeper.GetAccessTokenAsync();
            World ??= await _api.GetWorldAsync(token);
            long number = ++_lastRequest;
            Apply(number, await request(token));
        }
        catch (CloudAuthException exception)
        {
            GD.PushWarning($"Session refusée : {exception.Message}");
            SignOut();
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
