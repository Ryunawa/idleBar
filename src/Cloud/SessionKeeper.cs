using System.Threading.Tasks;
using Godot;

namespace IdleBar.Cloud;

public sealed class SessionKeeper
{
    private const double RefreshMarginSeconds = 60;

    private readonly SupabaseAuth _auth;
    private readonly SessionStore _store;
    private AuthSession? _session;

    public SessionKeeper(SupabaseAuth auth, SessionStore store)
    {
        _auth = auth;
        _store = store;
    }

    public bool SignedIn => _session is not null;

    public string? LastEmail { get; private set; }

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

        AuthSession refreshed = await _auth.RefreshAsync(session.RefreshToken);
        Remember(refreshed);
        return refreshed.AccessToken;
    }

    private void Remember(AuthSession session)
    {
        _store.Save(session);
        _session = session;
        LastEmail = session.Email;
    }
}
