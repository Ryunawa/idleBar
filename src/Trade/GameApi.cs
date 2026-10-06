using System.Threading.Tasks;
using IdleBar.Cloud;

namespace IdleBar.Trade;

public sealed class GameApi
{
    private readonly SupabaseRpc _rpc;

    public GameApi(SupabaseRpc rpc)
    {
        _rpc = rpc;
    }

    public Task<WorldData> GetWorldAsync(string accessToken) =>
        _rpc.CallAsync<NoArguments, WorldData>(accessToken, "get_world", new NoArguments());

    public Task<GameSnapshot> GetStateAsync(string accessToken) =>
        _rpc.CallAsync<NoArguments, GameSnapshot>(accessToken, "get_state", new NoArguments());

    public Task<GameSnapshot> FoundAsync(string accessToken, string name, string craftId, string townId) =>
        _rpc.CallAsync<FoundingArguments, GameSnapshot>(accessToken, "found_player", new FoundingArguments(name, craftId, townId));

    public Task<GameSnapshot> BuyAsync(string accessToken, string goodId, int quantity) =>
        _rpc.CallAsync<TradeArguments, GameSnapshot>(accessToken, "buy_goods", new TradeArguments(goodId, quantity));

    public Task<GameSnapshot> SellAsync(string accessToken, string goodId, int quantity) =>
        _rpc.CallAsync<TradeArguments, GameSnapshot>(accessToken, "sell_goods", new TradeArguments(goodId, quantity));

    public Task<GameSnapshot> BuyWagonAsync(string accessToken) =>
        _rpc.CallAsync<NoArguments, GameSnapshot>(accessToken, "buy_wagon", new NoArguments());

    public Task<GameSnapshot> DepartAsync(string accessToken, string destinationId) =>
        _rpc.CallAsync<DestinationArguments, GameSnapshot>(accessToken, "depart", new DestinationArguments(destinationId));

    public Task<GameSnapshot> StartProductionAsync(string accessToken, string recipeId, int batches) =>
        _rpc.CallAsync<ProductionArguments, GameSnapshot>(accessToken, "start_production", new ProductionArguments(recipeId, batches));

    public Task<GameSnapshot> UpgradeWorkshopAsync(string accessToken) =>
        _rpc.CallAsync<NoArguments, GameSnapshot>(accessToken, "upgrade_workshop", new NoArguments());
}
