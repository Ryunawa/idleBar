using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed class GameDialogs
{
    private const string ConfirmEmailMessage = "Compte créé. Confirme ton adresse depuis l'email reçu, puis connecte-toi.";

    private readonly FrontierSession _session;
    private readonly Account _account;
    private readonly LoginWindow _login = new();

    public GameDialogs(FrontierSession session, Account account, Node host)
    {
        _session = session;
        _account = account;
        host.AddChild(_login);
        _login.Submitted += OnLoginSubmitted;
    }

    public void Open(float scale)
    {
        if (_session.Status == SessionStatus.SignedOut)
        {
            _login.Open(scale, _account.LastEmail);
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
                awaitingConfirmation = !await _account.SignUpAsync(email, password);
                return;
            }

            await _account.SignInAsync(email, password);
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
    }
}
