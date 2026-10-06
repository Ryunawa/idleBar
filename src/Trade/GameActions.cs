using System.Threading.Tasks;

namespace IdleBar.Trade;

public sealed class GameActions
{
    private readonly GameSession _session;
    private readonly GameApi _api;

    public GameActions(GameSession session, GameApi api)
    {
        _session = session;
        _api = api;
    }

    public Task FoundAsync(string name, string craftId, string townId) =>
        _session.PerformAsync(token => _api.FoundAsync(token, name, craftId, townId));

    public Task BuyAsync(string goodId, int quantity) =>
        _session.PerformAsync(token => _api.BuyAsync(token, goodId, quantity));

    public Task SellAsync(string goodId, int quantity) =>
        _session.PerformAsync(token => _api.SellAsync(token, goodId, quantity));

    public Task BuyWagonAsync() => _session.PerformAsync(_api.BuyWagonAsync);

    public Task DepartAsync(string destinationId) =>
        _session.PerformAsync(token => _api.DepartAsync(token, destinationId));

    public Task StartProductionAsync(string recipeId, int batches) =>
        _session.PerformAsync(token => _api.StartProductionAsync(token, recipeId, batches));

    public Task UpgradeWorkshopAsync() => _session.PerformAsync(_api.UpgradeWorkshopAsync);
}
