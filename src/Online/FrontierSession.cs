using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class FrontierSession
{
    private readonly SessionKeeper _keeper;
    private readonly SessionAccess _access;
    private readonly SyncClock _clock = new();
    private bool _connecting;

    public FrontierSession(SessionKeeper keeper, FrontierApi api)
    {
        _keeper = keeper;
        _access = new SessionAccess(keeper, api.GetRequirementsAsync);
    }

    public event Action? Changed;

    public SessionStatus Status { get; private set; } = SessionStatus.SignedOut;

    public void Start()
    {
        _keeper.Restore();
        if (_keeper.SignedIn)
        {
            _ = ConnectAsync();
        }
    }

    public Task<bool> RefreshAsync() => ConnectAsync();

    public void Reset() => SetStatus(SessionStatus.SignedOut);

    public void Tick(double delta)
    {
        if (!_keeper.SignedIn || _connecting)
        {
            return;
        }

        _clock.Advance(delta);
        if (_clock.RetryDue(Status))
        {
            _ = ConnectAsync();
        }
    }

    public Task FlushAsync() => Task.CompletedTask;

    private async Task<bool> ConnectAsync()
    {
        _connecting = true;
        _clock.Attempted();
        if (Status is SessionStatus.SignedOut or SessionStatus.Offline)
        {
            SetStatus(SessionStatus.Connecting);
        }

        try
        {
            await _access.TokenAsync();
            SetStatus(SessionStatus.Ready);
            return true;
        }
        catch (CloudAuthException exception)
        {
            GD.PushWarning($"Session refusée : {exception.Message}");
            _keeper.SignOut();
            Reset();
            return false;
        }
        catch (OutdatedClientException)
        {
            SetStatus(SessionStatus.UpdateRequired);
            return false;
        }
        catch (Exception exception) when (exception is ActionRefusedException || TransportFailure.Matches(exception))
        {
            GD.PushWarning($"Serveur injoignable : {exception.Message}");
            SetStatus(SessionStatus.Offline);
            return false;
        }
        finally
        {
            _connecting = false;
        }
    }

    private void SetStatus(SessionStatus status)
    {
        Status = status;
        Changed?.Invoke();
    }
}
