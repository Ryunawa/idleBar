using System.Threading.Tasks;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class FrontierApi
{
    private readonly SupabaseRpc _rpc;

    public FrontierApi(SupabaseRpc rpc)
    {
        _rpc = rpc;
    }

    public Task<RequirementsData> GetRequirementsAsync(string token) =>
        _rpc.CallAsync<NoArguments, RequirementsData>(token, "get_requirements", new NoArguments());
}
