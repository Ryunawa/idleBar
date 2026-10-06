using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed class GameDialogs
{
    private const string ConfirmEmailMessage = "Compte créé. Confirme ton adresse depuis l'email reçu, puis connecte-toi.";

    private readonly GameSession _session;
    private readonly GameActions _actions;
    private readonly LoginWindow _login = new();
    private readonly FoundingWindow _founding = new();
    private readonly TownWindow _town = new();
    private readonly SeenMarker _seen;
    private float _scale = 1f;

    public GameDialogs(GameSession session, GameActions actions, Node host)
    {
        _session = session;
        _actions = actions;
        _seen = new SeenMarker(session, actions);
        host.AddChild(_login);
        host.AddChild(_founding);
        host.AddChild(_town);

        _login.Submitted += OnLoginSubmitted;
        _founding.Submitted += OnFoundingSubmitted;
        _town.Requested += command => RunInTown(() => command(_actions));
        _town.TabViewed += _seen.MarkTab;
        _session.Changed += RefreshTown;
    }

    public void OpenFor(float scale, TownTab? tab = null)
    {
        _scale = scale;
        switch (_session.Status)
        {
            case SessionStatus.SignedOut:
                _login.Open(scale, _session.LastEmail);
                break;
            case SessionStatus.NeedsFounding:
                OpenFounding();
                break;
            case SessionStatus.Offline:
                _session.Retry();
                break;
            case SessionStatus.Ready:
                OpenTown(tab);
                break;
        }
    }

    public void MarkNewsSeen() => _seen.MarkExchanges();

    private void OpenFounding()
    {
        if (_session.World is WorldData world)
        {
            _founding.Open(_scale, world);
        }
    }

    private void OpenTown(TownTab? tab)
    {
        if (_session is not { World: WorldData world, Snapshot: GameSnapshot snapshot })
        {
            return;
        }

        _town.Open(_scale, TownAccess.For(world, snapshot, _session.Clock.Now), tab);
        RefreshTown();
    }

    private void RefreshTown()
    {
        if (!_town.Visible || _session.Status == SessionStatus.Offline)
        {
            return;
        }

        if (_session is { Status: SessionStatus.Ready, World: WorldData world, Snapshot: GameSnapshot snapshot })
        {
            _town.Refresh(world, snapshot, _session.Clock);
            return;
        }

        _town.Hide();
    }

    private async void RunInTown(Func<Task> action)
    {
        _town.SetBusy(true);
        string? error = await ActionFeedback.CaptureAsync(action);
        _town.SetBusy(false);
        if (error is not null)
        {
            _town.ShowError(error);
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
            _login.ShowInfo(ConfirmEmailMessage);
            return;
        }

        _login.Hide();
        if (_session.Status == SessionStatus.NeedsFounding)
        {
            OpenFounding();
        }
    }

    private async void OnFoundingSubmitted(string name, string craftId, string townId)
    {
        _founding.SetBusy(true);
        string? error = await ActionFeedback.CaptureAsync(() => _actions.FoundAsync(name, craftId, townId));
        _founding.SetBusy(false);

        if (error is not null)
        {
            _founding.ShowError(error);
            return;
        }

        _founding.Hide();
        OpenTown(null);
    }
}
