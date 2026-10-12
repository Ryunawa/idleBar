using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed class GameDialogs
{
    private const string ConfirmEmailMessage = "Compte créé. Confirme ton adresse depuis l'email reçu, puis connecte-toi.";

    private readonly FrontierSession _session;
    private readonly Account _account;
    private readonly Journal _journal;
    private readonly LoginWindow _login = new();
    private readonly FrontierWindow _window = new();

    public GameDialogs(FrontierSession session, Account account, Journal journal, Node host)
    {
        _session = session;
        _account = account;
        _journal = journal;
        host.AddChild(_login);
        host.AddChild(_window);
        _login.Submitted += OnLoginSubmitted;
        journal.Changed += () =>
        {
            if (_window.Visible)
            {
                _window.Journal.Refresh(journal);
            }
        };
    }

    public FrontierGame? Game
    {
        get => _window.Game;
        set => _window.Game = value;
    }

    public void Open(float scale)
    {
        if (_window.Visible)
        {
            _window.Hide();
            return;
        }

        Open(scale, WindowTab.Fighters);
    }

    public void Open(float scale, WindowTab tab)
    {
        if (_session.Status == SessionStatus.SignedOut)
        {
            _login.Open(scale, _account.LastEmail);
            return;
        }

        if (_window.Game is null)
        {
            return;
        }

        _window.Open(scale, tab);
        _window.Journal.Refresh(_journal);
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
