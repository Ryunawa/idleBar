using System;
using System.Threading.Tasks;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class SocialSession
{
    public Task<string> EquipAsync(long item, FighterKind kind) => RunAsync(token => _api.EquipAsync(token, item, Fighters.Of(kind).Id));

    public Task<string> UnequipAsync(string slot, FighterKind kind) => RunAsync(token => _api.UnequipAsync(token, slot, Fighters.Of(kind).Id));

    public Task<string> BuyAsync(FighterKind kind, string slot, int rarity) => SpendAsync((token, session, revision) =>
        _api.BuyAsync(token, new BuyArguments(session, revision, Fighters.Of(kind).Id, slot, Items.RarityId(rarity))));

    public Task<string> UpgradeAsync(ItemData item, int count) => SpendAsync((token, session, revision) =>
        _api.UpgradeAsync(token, new UpgradeArguments(session, revision, item.Id, count)));

    public async Task<string> SalvageAsync(ItemData item)
    {
        if (_frontier.Game is not { Frozen: false })
        {
            return SocialData.Paused;
        }

        string status = await RunAsync(token => _api.SalvageAsync(token, item.Id));
        if (status == SocialData.Ok)
        {
            _frontier.Game?.Recycle(item.Rank, item.Source == Items.ShopSource, item.Level);
        }

        return status;
    }

    private async Task<string> SpendAsync(Func<string, Guid, int, Task<SocialData>> call)
    {
        int generation = _generation;
        int ticket = 0;
        SocialData? data = await _frontier.SpendAsync((token, session, revision) =>
        {
            ticket = ++_sent;
            return call(token, session, revision);
        });
        if (data is null || generation != _generation)
        {
            return _frontier.Game is { Frozen: true } ? SocialData.Paused : Unavailable;
        }

        if (ticket > _accepted)
        {
            _accepted = ticket;
            Accept(data);
        }

        return data.Status;
    }
}
