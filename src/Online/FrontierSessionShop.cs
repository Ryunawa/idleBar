using System;
using System.Threading.Tasks;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class FrontierSession
{
    private const int SaveWaitMilliseconds = 50;

    public async Task<SocialData?> SpendAsync(Func<string, Guid, int, Task<SocialData>> call)
    {
        if (Game is not FrontierGame game || game.Frozen || Status != SessionStatus.Ready)
        {
            return null;
        }

        while (_busy)
        {
            await Task.Delay(SaveWaitMilliseconds);
        }

        await SaveAsync();
        if (Game != game || Status != SessionStatus.Ready || _busy)
        {
            return null;
        }

        int generation = _generation;
        _busy = true;
        try
        {
            string token = await _access.TokenAsync();
            SocialData data = await call(token, _session, _revision);
            if (Stale(generation) || Game != game)
            {
                return null;
            }

            if (data.Status == SocialData.Ok && data.Revision is int revision && data.Spent is double spent)
            {
                _revision = revision;
                game.State.Gold = Math.Max(0, game.State.Gold - spent);
                Remember();
            }

            return data;
        }
        catch (Exception exception) when (Stale(generation) || Fails(exception))
        {
            return null;
        }
        finally
        {
            if (!Stale(generation))
            {
                _busy = false;
            }
        }
    }
}
