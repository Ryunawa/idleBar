using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Pixel;
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
    private readonly BuildingPanel _building = new();
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
        host.AddChild(_building);

        _login.Submitted += OnLoginSubmitted;
        _founding.Submitted += OnFoundingSubmitted;
        _town.Requested += command => RunInTown(() => command(_actions));
        _town.TabViewed += _seen.MarkTab;
        _session.Changed += RefreshTown;
        _building.Requested += command => RunInBuilding(() => command(_actions));
        _building.MoreRequested += tab => OpenFor(_scale, tab);
        _session.Changed += RefreshBuilding;
    }

    public Func<RecipeInfo?>? Relaunch { get; set; }

    public bool BuildingOpen => _building.Visible;

    public void OpenBuilding(StreetBuilding building, Vector2I anchor, float scale)
    {
        _scale = scale;
        if (_building.Visible && _building.Building == building)
        {
            _building.Hide();
            return;
        }

        if (_session.Status != SessionStatus.Ready)
        {
            OpenFor(scale);
            return;
        }

        if (BuildingSheets.For(building, _session, Relaunch?.Invoke()) is BuildingSheet sheet && _session.Snapshot is GameSnapshot snapshot)
        {
            string townId = snapshot.Caravan?.TownId ?? snapshot.Workshop?.TownId ?? snapshot.Player!.HomeTownId;
            _building.Open(building, sheet, WindowSkin.For(townId), anchor, scale);
        }
    }

    public void CloseBuilding() => _building.Hide();

    public void RefreshBuilding()
    {
        if (_building is { Visible: true, Building: StreetBuilding building } && BuildingSheets.For(building, _session, Relaunch?.Invoke()) is BuildingSheet sheet)
        {
            _building.Refresh(sheet);
        }
    }

    public void OpenFor(float scale, TownTab? tab = null)
    {
        _scale = scale;
        _building.Hide();
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

    private async void RunInBuilding(Func<Task> action)
    {
        _building.SetBusy(true);
        string? error = await ActionFeedback.CaptureAsync(action);
        _building.SetBusy(false);
        RefreshBuilding();
        if (error is not null)
        {
            _building.ShowError(error);
        }
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
            _login.ShowSignedUp(ConfirmEmailMessage);
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
