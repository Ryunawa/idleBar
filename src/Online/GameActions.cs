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

    public Task SendEmoteAsync(long visit, string emote) => _session.SendAsync(token => _api.SendEmoteAsync(token, visit, emote));
}
