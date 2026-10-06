using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using IdleBar.Game;

namespace IdleBar.Cloud;

public sealed class CloudSync
{
    private const double PushIntervalSeconds = 60;
    private const double PullIntervalSeconds = 30;
    private const double RetryIntervalSeconds = 30;
    private const double TokenRefreshMarginSeconds = 60;

    private readonly GameState _game;
    private readonly SupabaseAuth _auth;
    private readonly SupabaseSaves _saves;
    private readonly SessionStore _sessions;
    private readonly string _device;
    private readonly double _maxOfflineSeconds;
    private AuthSession? _session;
    private bool _busy;
    private bool _claimPending;
    private double _sinceLastSync;

    public CloudSync(
        GameState game,
        SupabaseAuth auth,
        SupabaseSaves saves,
        SessionStore sessions,
        string device,
        long? knownRevision,
        double maxOfflineSeconds)
    {
        _game = game;
        _auth = auth;
        _saves = saves;
        _sessions = sessions;
        _device = device;
        _maxOfflineSeconds = maxOfflineSeconds;
        KnownRevision = knownRevision;
    }

    public event Action<double, double>? ProgressAdopted;

    public CloudStatus Status { get; private set; } = CloudStatus.SignedOut;

    public string? ActiveDevice { get; private set; }

    public long? KnownRevision { get; private set; }

    public string? Email => _session?.Email;

    public void Start()
    {
        _session = _sessions.Load();
        if (_session is null)
        {
            return;
        }

        _claimPending = true;
        _ = RunAsync(ConnectAsync);
    }

    public async Task SignInAsync(string email, string password)
    {
        AuthSession session = await _auth.SignInAsync(email, password);
        _sessions.Save(session);
        _session = session;
        _claimPending = true;
        _ = RunAsync(ConnectAsync);
    }

    public void SignOut()
    {
        _sessions.Clear();
        _session = null;
        ActiveDevice = null;
        Status = CloudStatus.SignedOut;
    }

    public void RequestTakeOver()
    {
        if (_session is null || Status is CloudStatus.Active or CloudStatus.Connecting)
        {
            return;
        }

        _claimPending = true;
        _ = RunAsync(ConnectAsync);
    }

    public void Tick(double delta)
    {
        if (_session is null || _busy)
        {
            return;
        }

        if (_claimPending && Status != CloudStatus.Offline)
        {
            _ = RunAsync(ConnectAsync);
            return;
        }

        _sinceLastSync += delta;
        double interval = Status switch
        {
            CloudStatus.Active => PushIntervalSeconds,
            CloudStatus.Passive => PullIntervalSeconds,
            CloudStatus.Offline => RetryIntervalSeconds,
            _ => double.PositiveInfinity,
        };
        if (_sinceLastSync < interval)
        {
            return;
        }

        Func<Task> operation = Status switch
        {
            CloudStatus.Active => PushAsync,
            CloudStatus.Passive => PullAsync,
            _ => ConnectAsync,
        };
        _ = RunAsync(operation);
    }

    public Task FlushAsync() =>
        Status == CloudStatus.Active && !_busy ? RunAsync(PushAsync) : Task.CompletedTask;

    private async Task RunAsync(Func<Task> operation)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await operation();
        }
        catch (CloudAuthException exception)
        {
            GD.PushWarning($"Synchronisation : {exception.Message}");
            SignOut();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or CloudRequestException or JsonException)
        {
            GD.PushWarning($"Synchronisation indisponible : {exception.Message}");
            Status = CloudStatus.Offline;
        }
        finally
        {
            _busy = false;
            _sinceLastSync = 0;
            if (_session is null)
            {
                Status = CloudStatus.SignedOut;
            }
        }
    }

    private async Task ConnectAsync()
    {
        Status = CloudStatus.Connecting;
        string token = await GetAccessTokenAsync();
        CloudSnapshot? remote = await _saves.FetchAsync(token);
        if (remote is null)
        {
            BecomeActive(await _saves.CreateAsync(token, _game.Capture(), _device));
            return;
        }

        if (remote.Revision != KnownRevision)
        {
            Adopt(remote);
            if (remote.ActiveDevice != _device && !_claimPending)
            {
                BecomePassive(remote);
                return;
            }
        }

        await WriteAsync(token, remote.Revision);
    }

    private async Task PushAsync()
    {
        string token = await GetAccessTokenAsync();
        await WriteAsync(token, KnownRevision ?? 0);
    }

    private async Task PullAsync()
    {
        string token = await GetAccessTokenAsync();
        CloudSnapshot? latest = await _saves.FetchAsync(token);
        if (latest is null)
        {
            BecomeActive(await _saves.CreateAsync(token, _game.Capture(), _device));
            return;
        }

        if (latest.Revision != KnownRevision)
        {
            Adopt(latest);
        }

        if (latest.ActiveDevice == _device)
        {
            BecomeActive(latest);
            return;
        }

        BecomePassive(latest);
    }

    private async Task WriteAsync(string token, long expectedRevision)
    {
        CloudSnapshot? written = await _saves.TryUpdateAsync(token, expectedRevision, _game.Capture(), _device);
        if (written is not null)
        {
            BecomeActive(written);
            return;
        }

        CloudSnapshot latest = await _saves.FetchAsync(token)
            ?? await _saves.CreateAsync(token, _game.Capture(), _device);
        Adopt(latest);
        if (latest.ActiveDevice == _device)
        {
            BecomeActive(latest);
            return;
        }

        BecomePassive(latest);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        AuthSession session = _session ?? throw new CloudAuthException("Non connecté.", "not_signed_in");
        if (session.ExpiresAtUnixSeconds - Time.GetUnixTimeFromSystem() > TokenRefreshMarginSeconds)
        {
            return session.AccessToken;
        }

        AuthSession refreshed = await _auth.RefreshAsync(session.RefreshToken);
        _sessions.Save(refreshed);
        _session = refreshed;
        return refreshed.AccessToken;
    }

    private void Adopt(CloudSnapshot snapshot)
    {
        double elapsed = Math.Min(snapshot.SecondsSinceUpdate, _maxOfflineSeconds);
        _game.Restore(snapshot.Progress);
        double gain = _game.Advance(elapsed);
        KnownRevision = snapshot.Revision;
        ProgressAdopted?.Invoke(elapsed, gain);
    }

    private void BecomeActive(CloudSnapshot snapshot)
    {
        KnownRevision = snapshot.Revision;
        ActiveDevice = snapshot.ActiveDevice;
        Status = CloudStatus.Active;
        _claimPending = false;
    }

    private void BecomePassive(CloudSnapshot snapshot)
    {
        KnownRevision = snapshot.Revision;
        ActiveDevice = snapshot.ActiveDevice;
        Status = CloudStatus.Passive;
        _claimPending = false;
    }
}
