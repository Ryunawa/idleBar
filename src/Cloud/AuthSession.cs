namespace IdleBar.Cloud;

public sealed record AuthSession(
    string AccessToken,
    string RefreshToken,
    double ExpiresAtUnixSeconds,
    string UserId,
    string Email);
