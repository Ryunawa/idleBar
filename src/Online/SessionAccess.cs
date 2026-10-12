using System;
using System.Threading.Tasks;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class SessionAccess
{
    private readonly SessionKeeper _keeper;
    private readonly Func<string, Task<RequirementsData>> _requirements;
    private bool _versionChecked;

    public SessionAccess(SessionKeeper keeper, Func<string, Task<RequirementsData>> requirements)
    {
        _keeper = keeper;
        _requirements = requirements;
    }

    public void Recheck() => _versionChecked = false;

    public async Task<string> TokenAsync()
    {
        string token = await _keeper.GetAccessTokenAsync();
        if (_versionChecked)
        {
            return token;
        }

        RequirementsData requirements = await _requirements(token);
        if (Version.TryParse(requirements.MinClientVersion, out Version? minimum) && GameVersion.Current < minimum)
        {
            throw new OutdatedClientException(requirements.MinClientVersion);
        }

        _versionChecked = true;
        return token;
    }
}
