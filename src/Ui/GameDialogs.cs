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
    private float _scale = 1f;

    public GameDialogs(GameSession session, GameActions actions, Node host)
    {
        _session = session;
        _actions = actions;
        host.AddChild(_login);
        host.AddChild(_founding);
        host.AddChild(_town);

        _login.Submitted += OnLoginSubmitted;
        _founding.Submitted += OnFoundingSubmitted;
        _town.BuyRequested += (goodId, quantity) => RunInTown(() => _actions.BuyAsync(goodId, quantity));
        _town.SellRequested += (goodId, quantity) => RunInTown(() => _actions.SellAsync(goodId, quantity));
        _town.WagonRequested += () => RunInTown(_actions.BuyWagonAsync);
        _town.DepartRequested += destinationId => RunInTown(() => _actions.DepartAsync(destinationId));
        _town.ProductionRequested += (recipeId, batches) => RunInTown(() => _actions.StartProductionAsync(recipeId, batches));
        _town.WorkshopUpgradeRequested += () => RunInTown(_actions.UpgradeWorkshopAsync);
        _session.Changed += RefreshTown;
    }

    public void OpenFor(float scale)
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
            case SessionStatus.Ready when !_session.IsTravelling:
                OpenTown();
                break;
        }
    }

    private void OpenFounding()
    {
        if (_session.World is WorldData world)
        {
            _founding.Open(_scale, world);
        }
    }

    private void OpenTown()
    {
        _town.Open(_scale, _session.Caravan is not null);
        RefreshTown();
    }

    private void RefreshTown()
    {
        if (!_town.Visible || _session.Status == SessionStatus.Offline)
        {
            return;
        }

        if (_session is { Status: SessionStatus.Ready, World: WorldData world, Snapshot: GameSnapshot snapshot } && !_session.IsTravelling)
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
        OpenTown();
    }
}
