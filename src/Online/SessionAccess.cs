using System;
using System.Threading.Tasks;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class SessionAccess
{
    private readonly SessionKeeper _keeper;
    private readonly TavernApi _api;
    private readonly Doorbell _doorbell;
    private bool _versionChecked;

    public SessionAccess(SessionKeeper keeper, TavernApi api, Doorbell doorbell)
    {
        _keeper = keeper;
        _api = api;
        _doorbell = doorbell;
    }

    public async Task<string> TokenAsync()
    {
        string token = await _keeper.GetAccessTokenAsync();
        if (!_versionChecked)
        {
            RequirementsData requirements = await _api.GetRequirementsAsync(token);
            if (Version.TryParse(requirements.MinClientVersion, out Version? minimum) && GameVersion.Current < minimum)
            {
                throw new OutdatedClientException(requirements.MinClientVersion);
            }

            _versionChecked = true;
        }

        if (_keeper.UserId is string userId)
        {
            _doorbell.Listen(userId, token);
        }

        return token;
    }
}
