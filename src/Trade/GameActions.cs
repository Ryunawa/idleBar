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

    public Task BuyAsync(string goodId, int quantity, string townId) =>
        _session.PerformAsync(token => _api.BuyAsync(token, goodId, quantity, townId));

    public Task SellAsync(string goodId, int quantity, string townId) =>
        _session.PerformAsync(token => _api.SellAsync(token, goodId, quantity, townId));

    public Task BuyWagonAsync() => _session.PerformAsync(_api.BuyWagonAsync);

    public Task DepartAsync(string destinationId, string? directiveId) =>
        _session.PerformAsync(token => _api.DepartAsync(token, destinationId, directiveId));

    public Task AnswerEventAsync(string choiceId) =>
        _session.PerformAsync(token => _api.AnswerEventAsync(token, choiceId));

    public Task StartProductionAsync(string recipeId, int batches) =>
        _session.PerformAsync(token => _api.StartProductionAsync(token, recipeId, batches));

    public Task UpgradeWorkshopAsync() => _session.PerformAsync(_api.UpgradeWorkshopAsync);

    public Task DepositAsync(string goodId, int quantity) =>
        _session.PerformAsync(token => _api.DepositAsync(token, goodId, quantity));

    public Task WithdrawAsync(string goodId, int quantity) =>
        _session.PerformAsync(token => _api.WithdrawAsync(token, goodId, quantity));

    public Task OpenBranchAsync(string townId) =>
        _session.PerformAsync(token => _api.OpenBranchAsync(token, townId));

    public Task PostOfferAsync(OfferDraft draft, string townId) =>
        _session.PerformAsync(token => _api.PostOfferAsync(token, draft, townId));

    public Task CancelOfferAsync(long offerId) =>
        _session.PerformAsync(token => _api.CancelOfferAsync(token, offerId));

    public Task AcceptOfferAsync(long offerId) =>
        _session.PerformAsync(token => _api.AcceptOfferAsync(token, offerId));

    public Task PostContractAsync(ContractDraft draft, string townId) =>
        _session.PerformAsync(token => _api.PostContractAsync(token, draft, townId));

    public Task CancelContractAsync(long contractId) =>
        _session.PerformAsync(token => _api.CancelContractAsync(token, contractId));

    public Task AcceptContractAsync(long contractId) =>
        _session.PerformAsync(token => _api.AcceptContractAsync(token, contractId));

    public Task MarkExchangesSeenAsync() => _session.PerformAsync(_api.MarkExchangesSeenAsync);

    public Task SetStandingOrderAsync(string kindId, string choiceId) =>
        _session.PerformAsync(token => _api.SetStandingOrderAsync(token, kindId, choiceId));

    public Task FulfillSpecialOrderAsync(long orderId) =>
        _session.PerformAsync(token => _api.FulfillSpecialOrderAsync(token, orderId));

    public Task DeclineSpecialOrderAsync(long orderId) =>
        _session.PerformAsync(token => _api.DeclineSpecialOrderAsync(token, orderId));

    public Task MarkJournalSeenAsync() => _session.PerformAsync(_api.MarkJournalSeenAsync);

    public Task ChooseTalentAsync(string talentId) =>
        _session.PerformAsync(token => _api.ChooseTalentAsync(token, talentId));

    public Task SellMasterpieceAsync(long masterpieceId) =>
        _session.PerformAsync(token => _api.SellMasterpieceAsync(token, masterpieceId));

    public Task InstallFittingAsync(string fittingId) =>
        _session.PerformAsync(token => _api.InstallFittingAsync(token, fittingId));

    public Task AttachWagonAsync() => _session.PerformAsync(_api.AttachWagonAsync);
}
