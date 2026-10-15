using System;
using System.Threading.Tasks;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class FrontierSession
{
    public const string Unavailable = "unavailable";

    private const int SaveWaits = 200;

    public async Task SettleAsync()
    {
        await WaitForSaveAsync();
        await FlushAsync();
    }

    public async Task<string> SetHeroAsync(HeroLook look, HeroRole role)
    {
        if (Game is not FrontierGame game || Status != SessionStatus.Ready)
        {
            return Unavailable;
        }

        if (role != game.State.Role || SavedRetreats != game.State.Retreats)
        {
            await WaitForSaveAsync();
        }

        if ((role != game.State.Role || SavedRetreats != game.State.Retreats) && !_busy)
        {
            await SaveAsync();
        }

        int generation = _generation;
        try
        {
            string token = await _access.TokenAsync();
            FrontierReply reply = await _api.SetHeroAsync(token, _session, look, role);
            if (Stale(generation) || Game != game)
            {
                return Unavailable;
            }

            if (reply.Status == FrontierReply.Ok && reply.Frontier is FrontierData data && data.Hero is HeroLook kept)
            {
                game.Adopt(kept, Roles.Parse(data.HeroRole) ?? role, data.RoleRetreats);
                Remember();
            }

            return reply.Status;
        }
        catch (Exception exception) when (Stale(generation) || Fails(exception))
        {
            return Unavailable;
        }
    }

    private async Task WaitForSaveAsync()
    {
        for (int wait = 0; _busy && wait < SaveWaits; wait++)
        {
            await Task.Delay(SaveWaitMilliseconds);
        }
    }
}
