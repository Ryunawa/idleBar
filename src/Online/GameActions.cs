using System;
using System.Threading.Tasks;

namespace IdleBar.Online;

public sealed class GameActions
{
    private readonly OnlineSession _session;
    private readonly TavernApi _api;

    public GameActions(OnlineSession session, TavernApi api)
    {
        _session = session;
        _api = api;
    }

    public Task FoundAsync(string name) => _session.PerformAsync(token => _api.FoundAsync(token, name));

    public Task BuyAsync(string upgradeId) => _session.PerformAsync(token => _api.BuyAsync(token, upgradeId));

    public Task CollectTipJarAsync() => _session.PerformAsync(_api.CollectTipJarAsync);

    public Task RequestFriendAsync(string code) => _session.PerformAsync(token => _api.RequestFriendAsync(token, code));

    public Task AnswerFriendAsync(Guid from, bool accept) => _session.PerformAsync(token => _api.AnswerFriendAsync(token, from, accept));

    public Task RemoveFriendAsync(Guid friend) => _session.PerformAsync(token => _api.RemoveFriendAsync(token, friend));

    public Task SetAvatarAsync(AvatarData avatar) => _session.PerformAsync(token => _api.SetAvatarAsync(token, avatar));

    public Task SetSpecialtyAsync(SpecialtyArguments specialty) => _session.PerformAsync(token => _api.SetSpecialtyAsync(token, specialty));

    public Task StartVisitAsync(Guid host, string stamp) => _session.PerformAsync(token => _api.StartVisitAsync(token, host, stamp));

    public Task ServeVisitAsync(long visit, bool perfect) => _session.PerformAsync(token => _api.ServeVisitAsync(token, visit, perfect));

    public Task GreetAsync(Guid player) => _session.SendAsync(token => _api.GreetAsync(token, player));

    public Task InviteAsync(Guid player) => _session.PerformAsync(token => _api.InviteAsync(token, player));

    public Task AnswerInvitationAsync(Guid from, bool accept) => _session.PerformAsync(token => _api.AnswerInvitationAsync(token, from, accept));

    public Task OfferRoundAsync() => _session.PerformAsync(_api.OfferRoundAsync);

    public Task SendEmoteAsync(long visit, string emote) => _session.SendAsync(token => _api.SendEmoteAsync(token, visit, emote));

    public Task SetStampAsync(string stamp) => _session.PerformAsync(token => _api.SetStampAsync(token, stamp));

    public Task LeaveVisitAsync() => _session.PerformAsync(_api.LeaveVisitAsync);

    public Task ShowDoorAsync(long visit) => _session.PerformAsync(token => _api.ShowDoorAsync(token, visit));

    public Task SayAsync(string text) => _session.PerformAsync(token => _api.SayAsync(token, text));

    public Task MuteAsync(Guid player, bool muted) => _session.PerformAsync(token => _api.MuteAsync(token, player, muted));

    public Task<FriendProfile> GetFriendProfileAsync(Guid friend) => _session.FetchAsync(token => _api.GetFriendProfileAsync(token, friend));
}
