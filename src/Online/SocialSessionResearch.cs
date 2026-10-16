using System.Threading.Tasks;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class SocialSession
{
    public int LastGiven { get; private set; }

    public async Task<string> FundAsync(ResearchKind kind, int amount)
    {
        SocialData? reply = null;
        string status = await RunAsync(async token => reply = await _api.FundAsync(token, Researches.Of(kind).Id, amount));
        LastGiven = status == SocialData.Ok ? reply?.Given ?? 0 : 0;
        return status;
    }

    public Task<string> MoveAsync(ResearchKind kind, int level, int offset) =>
        RunAsync(token => _api.MoveAsync(token, Researches.Of(kind).Id, level, offset));
}
