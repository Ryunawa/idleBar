using System.Threading.Tasks;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class SocialApi
{
    public const string HeroHelp = "hero";
    public const string ConvoyHelp = "convoy";

    private readonly SupabaseRpc _rpc;

    public SocialApi(SupabaseRpc rpc)
    {
        _rpc = rpc;
    }

    public Task<SocialData> GetAsync(string token) => _rpc.CallAsync<NoArguments, SocialData>(token, "get_social", new NoArguments());

    public Task<SocialData> CreateGuildAsync(string token, string name) =>
        _rpc.CallAsync<GuildNameArguments, SocialData>(token, "create_guild", new GuildNameArguments(name));

    public Task<SocialData> JoinGuildAsync(string token, string code) =>
        _rpc.CallAsync<GuildCodeArguments, SocialData>(token, "join_guild", new GuildCodeArguments(code));

    public Task<SocialData> LeaveGuildAsync(string token) => _rpc.CallAsync<NoArguments, SocialData>(token, "leave_guild", new NoArguments());

    public Task<SocialData> KickAsync(string token, long member) =>
        _rpc.CallAsync<MemberArguments, SocialData>(token, "kick_member", new MemberArguments(member));

    public Task<SocialData> RenameGuildAsync(string token, string name) =>
        _rpc.CallAsync<GuildNameArguments, SocialData>(token, "rename_guild", new GuildNameArguments(name));

    public Task<SocialData> NewCodeAsync(string token) => _rpc.CallAsync<NoArguments, SocialData>(token, "new_guild_code", new NoArguments());

    public Task<SocialData> SendHelpAsync(string token, long member, string kind) =>
        _rpc.CallAsync<HelpArguments, SocialData>(token, "send_help", new HelpArguments(member, kind));

    public Task<SocialData> ClaimTitanAsync(string token) => _rpc.CallAsync<NoArguments, SocialData>(token, "claim_titan", new NoArguments());

    public Task<SocialData> EquipAsync(string token, long item, string fighter) =>
        _rpc.CallAsync<EquipArguments, SocialData>(token, "equip_item", new EquipArguments(item, fighter));

    public Task<SocialData> UnequipAsync(string token, string slot, string fighter) =>
        _rpc.CallAsync<UnequipArguments, SocialData>(token, "unequip_item", new UnequipArguments(slot, fighter));

    public Task<SocialData> BuyAsync(string token, OfferArguments arguments) =>
        _rpc.CallAsync<OfferArguments, SocialData>(token, "buy_offer", arguments);

    public Task<SocialData> UpgradeAsync(string token, UpgradeArguments arguments) =>
        _rpc.CallAsync<UpgradeArguments, SocialData>(token, "upgrade_item", arguments);

    public Task<SocialData> SalvageAsync(string token, long item) =>
        _rpc.CallAsync<ItemArguments, SocialData>(token, "salvage_item", new ItemArguments(item));
}
