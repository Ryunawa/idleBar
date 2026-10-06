namespace IdleBar.Cloud;

internal sealed record TokenResponse(string AccessToken, string RefreshToken, long ExpiresIn, TokenUser User);
