using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;

namespace IdleBar.Trade;

public sealed class GameSession
{
    private const double PollIntervalSeconds = 60;
    private const double RetryIntervalSeconds = 20;

    private readonly SessionKeeper _keeper;
    private readonly GameApi _api;
    private long _lastRequest;
    private long _appliedRequest;
    private bool _refreshing;
    private double _sinceRefresh;

    public GameSession(SessionKeeper keeper, GameApi api, ServerClock clock)
    {
        _keeper = keeper;
        _api = api;
        Clock = clock;
    }

    public event Action? Changed;

    public event Action<TownInfo>? Arrived;

    public event Action<RecipeInfo, int>? ProductionDelivered;

    public SessionStatus Status { get; private set; } = SessionStatus.SignedOut;

    public WorldData? World { get; private set; }

    public GameSnapshot? Snapshot { get; private set; }

    public ServerClock Clock { get; }

    public PlayerState? Player => Snapshot?.Player;

    public CaravanState? Caravan => Snapshot?.Caravan;

    public WorkshopState? Workshop => Snapshot?.Workshop;

    public string? LastEmail => _keeper.LastEmail;

    public bool IsTravelling => Caravan?.IsTravelling(Clock.Now) == true;

    public void Start()
    {
        _keeper.Restore();
        if (_keeper.SignedIn)
        {
            _ = RefreshAsync();
        }
    }

    public async Task SignInAsync(string email, string password)
    {
        await _keeper.SignInAsync(email, password);
        await RefreshAsync();
    }

    public async Task<bool> SignUpAsync(string email, string password)
    {
        bool signedIn = await _keeper.SignUpAsync(email, password);
        if (signedIn)
        {
            await RefreshAsync();
        }

        return signedIn;
    }

    public void SignOut()
    {
        _keeper.SignOut();
        World = null;
        Snapshot = null;
        Status = SessionStatus.SignedOut;
        Changed?.Invoke();
    }

    public void Retry()
    {
        if (_keeper.SignedIn && !_refreshing)
        {
            _ = RefreshAsync();
        }
    }

    public void Tick(double delta)
    {
        if (!_keeper.SignedIn || _refreshing)
        {
            return;
        }

        _sinceRefresh += delta;
        bool changeDue = Status == SessionStatus.Ready && Snapshot?.NextChangeAt is DateTimeOffset next
            && Snapshot.ServerTime < next && Clock.Now >= next;
        double interval = Status == SessionStatus.Offline ? RetryIntervalSeconds : PollIntervalSeconds;
        if (changeDue || _sinceRefresh >= interval)
        {
            _ = RefreshAsync();
        }
    }

    public async Task PerformAsync(Func<string, Task<GameSnapshot>> action)
    {
        try
        {
            string token = await _keeper.GetAccessTokenAsync();
            long request = ++_lastRequest;
            Apply(request, await action(token));
        }
        catch (CloudAuthException)
        {
            SignOut();
            throw;
        }
        catch (Exception exception) when (TransportFailure.Matches(exception))
        {
            Status = SessionStatus.Offline;
            Changed?.Invoke();
            throw;
        }
    }

    private async Task RefreshAsync()
    {
        _refreshing = true;
        if (Status is SessionStatus.SignedOut or SessionStatus.Offline)
        {
            Status = SessionStatus.Connecting;
            Changed?.Invoke();
        }

        try
        {
            string token = await _keeper.GetAccessTokenAsync();
            World ??= await _api.GetWorldAsync(token);
            long request = ++_lastRequest;
            Apply(request, await _api.GetStateAsync(token));
        }
        catch (CloudAuthException exception)
        {
            GD.PushWarning($"Session refusée : {exception.Message}");
            SignOut();
        }
        catch (Exception exception) when (TransportFailure.Matches(exception))
        {
            GD.PushWarning($"Serveur injoignable : {exception.Message}");
            Status = SessionStatus.Offline;
            Changed?.Invoke();
        }
        finally
        {
            _refreshing = false;
            _sinceRefresh = 0;
        }
    }

    private void Apply(long request, GameSnapshot snapshot)
    {
        if (request < _appliedRequest || !_keeper.SignedIn)
        {
            return;
        }

        _appliedRequest = request;
        GameSnapshot? previous = Snapshot;
        Clock.Synchronize(snapshot.ServerTime);
        Snapshot = snapshot;
        Status = snapshot.Player is null ? SessionStatus.NeedsFounding : SessionStatus.Ready;
        Changed?.Invoke();

        if (snapshot.EndsTripOf(previous) && World?.FindTown(snapshot.Caravan!.TownId) is TownInfo town)
        {
            Arrived?.Invoke(town);
        }

        int delivered = snapshot.BatchesDeliveredSince(previous);
        if (delivered > 0 && World?.FindRecipe(previous!.Workshop!.RecipeId!) is RecipeInfo recipe)
        {
            ProductionDelivered?.Invoke(recipe, delivered);
        }
    }
}
