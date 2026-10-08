using System.Threading.Tasks;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class Account
{
    private readonly SessionKeeper _keeper;
    private readonly OnlineSession _session;

    public Account(SessionKeeper keeper, OnlineSession session)
    {
        _keeper = keeper;
        _session = session;
    }

    public string? LastEmail => _keeper.LastEmail;

    public async Task SignInAsync(string email, string password)
    {
        await _keeper.SignInAsync(email, password);
        await _session.RefreshAsync();
    }

    public async Task<bool> SignUpAsync(string email, string password)
    {
        bool signedIn = await _keeper.SignUpAsync(email, password);
        if (signedIn)
        {
            await _session.RefreshAsync();
        }

        return signedIn;
    }

    public void SignOut()
    {
        _keeper.SignOut();
        _session.Reset();
    }
}
