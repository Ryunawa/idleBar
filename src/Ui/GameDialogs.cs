using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed class GameDialogs
{
    private const string ConfirmEmailMessage = "Compte créé. Confirme ton adresse depuis l'email reçu, puis connecte-toi.";

    private readonly OnlineSession _session;
    private readonly Account _account;
    private readonly GameActions _actions;
    private readonly LoginWindow _login = new();
    private readonly FoundingWindow _founding = new();
    private readonly TavernWindow _tavern = new();
    private float _scale = 1f;

    public GameDialogs(OnlineSession session, Account account, GameActions actions, Node host)
    {
        _session = session;
        _account = account;
        _actions = actions;
        host.AddChild(_login);
        host.AddChild(_founding);
        host.AddChild(_tavern);
        _login.Submitted += OnLoginSubmitted;
        _founding.Submitted += OnFoundingSubmitted;
        _tavern.BuyRequested += id => RunInTavern(() => _actions.BuyAsync(id));
        _tavern.Friends.FriendRequested += code => RunInTavern(() => _actions.RequestFriendAsync(code));
        _tavern.Friends.Answered += (from, accept) => RunInTavern(() => _actions.AnswerFriendAsync(from, accept));
        _tavern.Friends.Removed += friend => RunInTavern(() => _actions.RemoveFriendAsync(friend));
        _tavern.Friends.VisitRequested += (host, stamp) => RunInTavern(() => _actions.StartVisitAsync(host, stamp));
        _tavern.Profile.AvatarSaved += avatar => RunInTavern(() => _actions.SetAvatarAsync(avatar));
        _tavern.Profile.SpecialtySaved += specialty => RunInTavern(() => _actions.SetSpecialtyAsync(specialty));
        _session.Changed += RefreshTavern;
    }

    public void Open(float scale)
    {
        _scale = scale;
        switch (_session.Status)
        {
            case SessionStatus.SignedOut:
                _login.Open(scale, _account.LastEmail);
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

    private async void RunInTavern(Func<Task> action)
    {
        _tavern.ClearError();
        string? error = await ActionFeedback.CaptureAsync(action);
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
        if (_session.Status == SessionStatus.NeedsFounding)
        {
            _founding.Open(_scale);
        }
    }

    private async void OnFoundingSubmitted(string name)
    {
        _founding.SetBusy(true);
        string? error = await ActionFeedback.CaptureAsync(() => _actions.FoundAsync(name));
        _founding.SetBusy(false);
        if (error is not null)
        {
            _founding.ShowError(error);
            return;
        }

        _founding.Hide();
    }
}
