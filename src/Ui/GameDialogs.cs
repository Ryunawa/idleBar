using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed class GameDialogs
{
    private const string ConfirmEmailMessage = "Compte créé. Confirme ton adresse depuis l'email reçu, puis connecte-toi.";

    private readonly FrontierSession _session;
    private readonly SocialSession _social;
    private readonly Account _account;
    private readonly Journal _journal;
    private readonly LoginWindow _login = new();
    private readonly StartWindow _start = new();
    private readonly FrontierWindow _window = new();
    private float _scale = 1;
    private bool _classWanted;

    public GameDialogs(FrontierSession session, SocialSession social, Account account, Journal journal, Node host)
    {
        _session = session;
        _account = account;
        _journal = journal;
        host.AddChild(_login);
        host.AddChild(_start);
        host.AddChild(_window);
        _window.Connect(social);
        _social = social;
        _login.Submitted += OnLoginSubmitted;
        _start.Submitted += OnStartSubmitted;
        _window.Hero.SaveRequested += OnHeroSaveRequested;
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
                _start.Open(scale, null);
                return;
            case SessionStatus.Elsewhere:
                _ = _session.ResumeAsync();
                return;
        }

        if (_window.Game is not null)
        {
            _social.RefreshSoon();
            _window.Open(scale, tab);
            _window.Journal.Refresh(_journal);
        }
    }

    public void AskClassNext() => _classWanted = true;

    public void AskHero(float scale)
    {
        _scale = scale;
        if (!_start.Visible && _window.Game is FrontierGame game && (game.State.Hero is null || _classWanted))
        {
            _start.Open(scale, game.State, game.State.Hero is not null);
            _classWanted = false;
        }
    }

    private async void OnStartSubmitted(HeroLook look, HeroRole role)
    {
        _start.SetBusy(true);
        string status = FrontierSession.Unavailable;
        string? error = await ActionFeedback.CaptureAsync(async () =>
        {
            if (_session.Status == SessionStatus.NeedsStart)
            {
                await _session.BeginAsync();
            }

            status = await _session.SetHeroAsync(look, role);
        });
        _start.SetBusy(false);
        error ??= HeroMessages.For(status);
        if (error is not null)
        {
            _start.ShowError(error);
            return;
        }

        _social.RefreshSoon();
        _start.Hide();
    }

    private async void OnHeroSaveRequested(HeroLook look, HeroRole role)
    {
        _window.Hero.SetBusy(true);
        string status = await _session.SetHeroAsync(look, role);
        _window.Hero.SetBusy(false);
        _window.Hero.ShowResult(HeroMessages.For(status));
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
            _start.Open(_scale, null);
        }
    }
}
