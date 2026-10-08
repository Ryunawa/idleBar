using System.Threading.Tasks;
using Godot;

namespace IdleBar.Cloud;

public sealed class SessionKeeper
{
    private const double RefreshMarginSeconds = 60;

    private readonly SupabaseAuth _auth;
    private readonly SessionStore _store;
    private AuthSession? _session;
    private Task<AuthSession>? _refreshing;

    public SessionKeeper(SupabaseAuth auth, SessionStore store)
    {
        _auth = auth;
        _store = store;
    }

    public bool SignedIn => _session is not null;

    public string? LastEmail { get; private set; }

    public string? UserId => _session?.UserId;

    public void Restore()
    {
        _session = _store.Load();
        LastEmail = _session?.Email;
    }

    public async Task SignInAsync(string email, string password) =>
        Remember(await _auth.SignInAsync(email, password));

    public async Task<bool> SignUpAsync(string email, string password)
    {
        LastEmail = email;
        AuthSession? session = await _auth.SignUpAsync(email, password);
        if (session is null)
        {
            return false;
        }

        Remember(session);
        return true;
    }

    public void SignOut()
    {
        _store.Clear();
        _session = null;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        AuthSession session = _session ?? throw new CloudAuthException("Connecte-toi pour jouer.", "not_signed_in");
        if (session.ExpiresAtUnixSeconds - Time.GetUnixTimeFromSystem() > RefreshMarginSeconds)
        {
            return session.AccessToken;
        }

        _refreshing ??= RefreshAsync(session.RefreshToken);
        try
        {
            return (await _refreshing).AccessToken;
        }
        finally
        {
            _refreshing = null;
        }
    }

    private async Task<AuthSession> RefreshAsync(string refreshToken)
    {
        AuthSession refreshed = await _auth.RefreshAsync(refreshToken);
        if (_session is null)
        {
            throw new CloudAuthException("Connecte-toi pour jouer.", "not_signed_in");
        }

        Remember(refreshed);
        return refreshed;
    }

    private void Remember(AuthSession session)
    {
        _store.Save(session);
        _session = session;
        LastEmail = session.Email;
    }
}
