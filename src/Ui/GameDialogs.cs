using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed class GameDialogs
{
    private const string ConfirmEmailMessage = "Compte créé. Confirme ton adresse depuis l'email reçu, puis connecte-toi.";

    private readonly OnlineSession _session;
    private readonly LoginWindow _login = new();
    private readonly FoundingWindow _founding = new();
    private readonly TavernWindow _tavern = new();
    private float _scale = 1f;

    public GameDialogs(OnlineSession session, Node host)
    {
        _session = session;
        host.AddChild(_login);
        host.AddChild(_founding);
        host.AddChild(_tavern);
        _login.Submitted += OnLoginSubmitted;
        _founding.Submitted += OnFoundingSubmitted;
        _tavern.BuyRequested += OnBuyRequested;
        _session.Changed += RefreshTavern;
    }

    public void Open(float scale)
    {
        _scale = scale;
        switch (_session.Status)
        {
            case SessionStatus.SignedOut:
                _login.Open(scale, _session.LastEmail);
                break;
            case SessionStatus.NeedsFounding:
                _founding.Open(scale);
                break;
            case SessionStatus.Ready or SessionStatus.Offline when _session.Tavern is not null:
                ToggleTavern();
                break;
        }
    }

    private void ToggleTavern()
    {
        if (_tavern.Visible)
        {
            _tavern.Hide();
            return;
        }

        _tavern.Open(_scale);
        RefreshTavern();
    }

    private void RefreshTavern()
    {
        if (!_tavern.Visible)
        {
            return;
        }

        if (_session is { World: WorldData world, Tavern: TavernData tavern })
        {
            _tavern.Refresh(world, tavern, _session.Coins);
            return;
        }

        _tavern.Hide();
    }

    private async void OnBuyRequested(string upgradeId)
    {
        string? error = await ActionFeedback.CaptureAsync(() => _session.BuyAsync(upgradeId));
        if (error is not null)
        {
            _tavern.ShowError(error);
        }
    }

    private async void OnLoginSubmitted(string email, string password, bool createAccount)
    {
        _login.SetBusy(true);
        bool awaitingConfirmation = false;
        string? error = await ActionFeedback.CaptureAsync(async () =>
        {
            if (createAccount)
            {
                awaitingConfirmation = !await _session.SignUpAsync(email, password);
                return;
            }

            await _session.SignInAsync(email, password);
        });
        _login.SetBusy(false);
        if (error is not null)
        {
            _login.ShowError(error);
            return;
        }

        if (awaitingConfirmation)
        {
            _login.ShowSignedUp(ConfirmEmailMessage);
            return;
        }

        _login.Hide();
        if (_session.Status == SessionStatus.NeedsFounding)
        {
            _founding.Open(_scale);
        }
    }

    private async void OnFoundingSubmitted(string name)
    {
        _founding.SetBusy(true);
        string? error = await ActionFeedback.CaptureAsync(() => _session.FoundAsync(name));
        _founding.SetBusy(false);
        if (error is not null)
        {
            _founding.ShowError(error);
            return;
        }

        _founding.Hide();
    }
}
