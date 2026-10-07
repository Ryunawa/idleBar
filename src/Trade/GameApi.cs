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

    public Task<GameSnapshot> GetStateAsync(string accessToken) => Call(accessToken, "get_state", new NoArguments());

    public Task<GameSnapshot> FoundAsync(string accessToken, string name, string craftId, string townId) =>
        Call(accessToken, "found_player", new FoundingArguments(name, craftId, townId));

    public Task<GameSnapshot> BuyAsync(string accessToken, string goodId, int quantity, string townId) =>
        Call(accessToken, "buy_goods", new MarketArguments(goodId, quantity, townId));

    public Task<GameSnapshot> SellAsync(string accessToken, string goodId, int quantity, string townId) =>
        Call(accessToken, "sell_goods", new MarketArguments(goodId, quantity, townId));

    public Task<GameSnapshot> BuyWagonAsync(string accessToken) => Call(accessToken, "buy_wagon", new NoArguments());

    public Task<GameSnapshot> DepartAsync(string accessToken, string destinationId, string? directiveId) =>
        Call(accessToken, "depart", new DestinationArguments(destinationId, directiveId));

    public Task<GameSnapshot> AnswerEventAsync(string accessToken, string choiceId) =>
        Call(accessToken, "answer_event", new AnswerArguments(choiceId));

    public Task<GameSnapshot> StartProductionAsync(string accessToken, string recipeId, int batches) =>
        Call(accessToken, "start_production", new ProductionArguments(recipeId, batches));

    public Task<GameSnapshot> UpgradeWorkshopAsync(string accessToken) => Call(accessToken, "upgrade_workshop", new NoArguments());

    public Task<GameSnapshot> DepositAsync(string accessToken, string goodId, int quantity) =>
        Call(accessToken, "deposit_goods", new TradeArguments(goodId, quantity));

    public Task<GameSnapshot> WithdrawAsync(string accessToken, string goodId, int quantity) =>
        Call(accessToken, "withdraw_goods", new TradeArguments(goodId, quantity));

    public Task<GameSnapshot> OpenBranchAsync(string accessToken, string townId) =>
        Call(accessToken, "open_branch", new BranchArguments(townId));

    public Task<GameSnapshot> PostOfferAsync(string accessToken, OfferDraft draft, string townId) =>
        Call(accessToken, "post_offer", new OfferArguments(draft.GiveGoodId, draft.GiveQuantity, draft.WantGoodId, draft.WantQuantity, townId, draft.FromWarehouse));

    public Task<GameSnapshot> CancelOfferAsync(string accessToken, long offerId) =>
        Call(accessToken, "cancel_offer", new OfferIdArguments(offerId));

    public Task<GameSnapshot> AcceptOfferAsync(string accessToken, long offerId) =>
        Call(accessToken, "accept_offer", new OfferIdArguments(offerId));

    public Task<GameSnapshot> PostContractAsync(string accessToken, ContractDraft draft, string townId) =>
        Call(accessToken, "post_contract", new ContractArguments(draft.GoodId, draft.Quantity, draft.DestinationId, draft.Reward, townId));

    public Task<GameSnapshot> CancelContractAsync(string accessToken, long contractId) =>
        Call(accessToken, "cancel_contract", new ContractIdArguments(contractId));

    public Task<GameSnapshot> AcceptContractAsync(string accessToken, long contractId) =>
        Call(accessToken, "accept_contract", new ContractIdArguments(contractId));

    public Task<GameSnapshot> SetStandingOrderAsync(string accessToken, string kindId, string choiceId) =>
        Call(accessToken, "set_standing_order", new StandingOrderArguments(kindId, choiceId));

    public Task<GameSnapshot> FulfillSpecialOrderAsync(string accessToken, long orderId) =>
        Call(accessToken, "fulfill_special_order", new SpecialOrderArguments(orderId));

    public Task<GameSnapshot> DeclineSpecialOrderAsync(string accessToken, long orderId) =>
        Call(accessToken, "decline_special_order", new SpecialOrderArguments(orderId));

    public Task<GameSnapshot> MarkJournalSeenAsync(string accessToken) =>
        Call(accessToken, "mark_journal_seen", new NoArguments());

    public Task<GameSnapshot> ChooseTalentAsync(string accessToken, string talentId) =>
        Call(accessToken, "choose_talent", new TalentArguments(talentId));

    public Task<GameSnapshot> SellMasterpieceAsync(string accessToken, long masterpieceId) =>
        Call(accessToken, "sell_masterpiece", new MasterpieceArguments(masterpieceId));

    public Task<GameSnapshot> InstallFittingAsync(string accessToken, string fittingId) =>
        Call(accessToken, "install_fitting", new FittingArguments(fittingId));

    public Task<GameSnapshot> AttachWagonAsync(string accessToken) => Call(accessToken, "attach_wagon", new NoArguments());

    public Task<GameSnapshot> MarkExchangesSeenAsync(string accessToken) =>
        Call(accessToken, "mark_exchanges_seen", new NoArguments());

    public Task<GameSnapshot> CollectOddJobsAsync(string accessToken) => Call(accessToken, "collect_odd_jobs", new NoArguments());

    private Task<GameSnapshot> Call<TArguments>(string accessToken, string function, TArguments arguments) =>
        _rpc.CallAsync<TArguments, GameSnapshot>(accessToken, function, arguments);
}
