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
    private readonly Journal _journal;
    private readonly LoginWindow _login = new();
    private readonly FoundingWindow _founding = new();
    private readonly TavernWindow _tavern = new();
    private readonly FriendProfileWindow _profile = new();
    private float _scale = 1f;

    public GameDialogs(OnlineSession session, Account account, GameActions actions, Journal journal, Node host)
    {
        _session = session;
        _account = account;
        _actions = actions;
        _journal = journal;
        journal.Changed += () =>
        {
            if (_tavern.Visible)
            {
                _tavern.Journal.Refresh(journal);
            }
        };
        host.AddChild(_login);
        host.AddChild(_founding);
        host.AddChild(_tavern);
        _login.Submitted += OnLoginSubmitted;
        _founding.Submitted += OnFoundingSubmitted;
        host.AddChild(_profile);
        _tavern.BuyRequested += id => RunInTavern(async () =>
        {
            await _session.FlushAsync();
            await _actions.BuyAsync(id);
        });
        _tavern.Friends.ProfileRequested += friend => OpenProfile(friend, _scale);
        _tavern.Friends.Unmuted += player => RunInTavern(() => _actions.MuteAsync(player, false));
        _tavern.Profile.StampSaved += stamp => RunInTavern(() => _actions.SetStampAsync(stamp));
        _profile.MuteRequested += (player, muted) => RunInTavern(() => _actions.MuteAsync(player, muted));
        _tavern.Friends.FriendRequested += code => RunInTavern(() => _actions.RequestFriendAsync(code));
        _tavern.Friends.Answered += (from, accept) => RunInTavern(() => _actions.AnswerFriendAsync(from, accept));
        _tavern.Friends.Removed += friend => RunInTavern(() => _actions.RemoveFriendAsync(friend));
        _tavern.Friends.VisitRequested += (host, stamp) => RunInTavern(() => _actions.StartVisitAsync(host, stamp));
        _tavern.Friends.InvitationAnswered += (from, accept) => RunInTavern(() => _actions.AnswerInvitationAsync(from, accept));
        _tavern.Friends.RoundOffered += () => RunInTavern(_actions.OfferRoundAsync);
        _tavern.Profile.AvatarSaved += avatar => RunInTavern(() => _actions.SetAvatarAsync(avatar));
        _tavern.Profile.SpecialtySaved += specialty => RunInTavern(() => _actions.SetSpecialtyAsync(specialty));
        _session.Changed += RefreshTavern;
    }

    public event Action<string>? Refused;

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

    public async void OpenProfile(Guid friend, float scale)
    {
        FriendProfile? profile = null;
        string? error = await ActionFeedback.CaptureAsync(async () => profile = await _actions.GetFriendProfileAsync(friend));
        if (error is not null && _tavern.Visible)
        {
            _tavern.ShowError(error);
        }
        else if (error is not null)
        {
            Refused?.Invoke(error);
        }

        if (profile is not null && _session.World is WorldData world)
        {
            _profile.Open(scale, profile, world);
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
        _tavern.Journal.Refresh(_journal);
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
