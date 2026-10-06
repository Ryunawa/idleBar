namespace IdleBar.Cloud;

internal sealed record SignUpResponse(string? AccessToken, string? RefreshToken, long? ExpiresIn, TokenUser? User);
