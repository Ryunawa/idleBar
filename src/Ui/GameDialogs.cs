using System;
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
    private readonly StartWindow _start = new();
    private readonly FrontierWindow _window = new();
    private float _scale = 1;

    public GameDialogs(FrontierSession session, Account account, Journal journal, Node host)
    {
        _session = session;
        _account = account;
        _journal = journal;
        host.AddChild(_login);
        host.AddChild(_start);
        host.AddChild(_window);
        _login.Submitted += OnLoginSubmitted;
        _start.Submitted += OnStartSubmitted;
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
        set
        {
            _window.Game = value;
            if (value is null)
            {
                _window.Hide();
            }
        }
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
        _scale = scale;
        switch (_session.Status)
        {
            case SessionStatus.SignedOut:
                _login.Open(scale, _account.LastEmail);
                return;
            case SessionStatus.NeedsStart:
                _start.Open(scale);
                return;
            case SessionStatus.Elsewhere:
                _ = _session.ResumeAsync();
                return;
        }

        if (_window.Game is not null)
        {
            _window.Open(scale, tab);
            _window.Journal.Refresh(_journal);
        }
    }

    private async void OnStartSubmitted()
    {
        _start.SetBusy(true);
        string? error = await ActionFeedback.CaptureAsync(_session.BeginAsync);
        _start.SetBusy(false);
        if (error is not null)
        {
            _start.ShowError(error);
            return;
        }

        _start.Hide();
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
        if (_session.Status == SessionStatus.NeedsStart)
        {
            _start.Open(_scale);
        }
    }
}
