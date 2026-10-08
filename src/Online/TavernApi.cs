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
}
