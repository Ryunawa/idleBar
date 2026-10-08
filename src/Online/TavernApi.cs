using System;
using System.Threading.Tasks;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class TavernApi
{
    private readonly SupabaseRpc _rpc;

    public TavernApi(SupabaseRpc rpc)
    {
        _rpc = rpc;
    }

    public Task<WorldData> GetWorldAsync(string token) => _rpc.CallAsync<NoArguments, WorldData>(token, "get_world", new NoArguments());

    public Task<StateData> GetStateAsync(string token) => _rpc.CallAsync<NoArguments, StateData>(token, "get_state", new NoArguments());

    public Task<StateData> FoundAsync(string token, string name) =>
        _rpc.CallAsync<NameArguments, StateData>(token, "found_tavern", new NameArguments(name));

    public Task<StateData> ReportAsync(string token, ServiceReport report) =>
        _rpc.CallAsync<ReportArguments, StateData>(token, "report_service", new ReportArguments(report));

    public Task<StateData> BuyAsync(string token, string upgradeId) =>
        _rpc.CallAsync<UpgradeArguments, StateData>(token, "buy_upgrade", new UpgradeArguments(upgradeId));

    public Task<StateData> CollectTipJarAsync(string token) =>
        _rpc.CallAsync<NoArguments, StateData>(token, "collect_tip_jar", new NoArguments());

    public Task<StateData> RequestFriendAsync(string token, string code) =>
        _rpc.CallAsync<CodeArguments, StateData>(token, "request_friend", new CodeArguments(code));

    public Task<StateData> AnswerFriendAsync(string token, Guid from, bool accept) =>
        _rpc.CallAsync<AnswerArguments, StateData>(token, "answer_friend", new AnswerArguments(from, accept));

    public Task<StateData> RemoveFriendAsync(string token, Guid friend) =>
        _rpc.CallAsync<FriendArguments, StateData>(token, "remove_friend", new FriendArguments(friend));

    public Task<StateData> SetAvatarAsync(string token, AvatarData avatar) =>
        _rpc.CallAsync<AvatarArguments, StateData>(token, "set_avatar", new AvatarArguments(avatar));

    public Task<StateData> SetSpecialtyAsync(string token, SpecialtyArguments specialty) =>
        _rpc.CallAsync<SpecialtyArguments, StateData>(token, "set_specialty", specialty);

    public Task<StateData> StartVisitAsync(string token, Guid host, string stamp) =>
        _rpc.CallAsync<VisitArguments, StateData>(token, "start_visit", new VisitArguments(host, stamp));

    public Task<StateData> ServeVisitAsync(string token, long visit, bool perfect) =>
        _rpc.CallAsync<ServeVisitArguments, StateData>(token, "serve_visit", new ServeVisitArguments(visit, perfect));

    public Task<EmptyResult> GreetAsync(string token, Guid player) =>
        _rpc.CallAsync<PlayerArguments, EmptyResult>(token, "greet_passerby", new PlayerArguments(player));

    public Task<StateData> InviteAsync(string token, Guid player) =>
        _rpc.CallAsync<PlayerArguments, StateData>(token, "invite_passerby", new PlayerArguments(player));

    public Task<StateData> AnswerInvitationAsync(string token, Guid from, bool accept) =>
        _rpc.CallAsync<AnswerArguments, StateData>(token, "answer_invitation", new AnswerArguments(from, accept));

    public Task<StateData> OfferRoundAsync(string token) =>
        _rpc.CallAsync<NoArguments, StateData>(token, "offer_round", new NoArguments());

    public Task<EmptyResult> SendEmoteAsync(string token, long visit, string emote) =>
        _rpc.CallAsync<EmoteArguments, EmptyResult>(token, "send_emote", new EmoteArguments(visit, emote));
}
