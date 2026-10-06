using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Cloud;

public sealed class SupabaseAuth
{
    private const string UnknownErrorCode = "unknown";

    private static readonly HashSet<string> RevokedSessionCodes =
    [
        "invalid_grant",
        "refresh_token_not_found",
        "refresh_token_already_used",
        "session_not_found",
        "session_expired",
        "user_not_found",
        "user_banned",
    ];

    private readonly HttpClient _http;
    private readonly SupabaseSettings _settings;

    public SupabaseAuth(HttpClient http, SupabaseSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public Task<AuthSession> SignInAsync(string email, string password) =>
        RequestTokenAsync("password", new PasswordGrant(email, password));

    public async Task<AuthSession> RefreshAsync(string refreshToken)
    {
        try
        {
            return await RequestTokenAsync("refresh_token", new RefreshGrant(refreshToken));
        }
        catch (CloudAuthException exception) when (!RevokedSessionCodes.Contains(exception.ErrorCode))
        {
            throw new CloudRequestException($"Renouvellement de session refusé temporairement ({exception.ErrorCode}).");
        }
    }

    private static string DescribeError(string errorCode) => errorCode switch
    {
        "invalid_credentials" or "invalid_grant" => "Email ou mot de passe incorrect.",
        "email_not_confirmed" => "Cet email n'a pas encore été confirmé.",
        "refresh_token_not_found" or "refresh_token_already_used" or "session_not_found" or "session_expired" =>
            "Session expirée, reconnecte-toi.",
        _ => $"Connexion refusée par Supabase ({errorCode}).",
    };

    private static string ReadErrorCode(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("error_code", out JsonElement errorCode))
            {
                return errorCode.GetString() ?? UnknownErrorCode;
            }

            return root.TryGetProperty("error", out JsonElement legacyError)
                ? legacyError.GetString() ?? UnknownErrorCode
                : UnknownErrorCode;
        }
        catch (JsonException)
        {
            return UnknownErrorCode;
        }
    }

    private async Task<AuthSession> RequestTokenAsync<TGrant>(string grantType, TGrant grant)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_settings.Url}/auth/v1/token?grant_type={grantType}");
        request.Headers.Add("apikey", _settings.PublishableKey);
        request.Content = JsonContent.Create(grant, options: SupabaseJson.Options);

        using HttpResponseMessage response = await _http.SendAsync(request);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            string errorCode = ReadErrorCode(await response.Content.ReadAsStringAsync());
            throw new CloudAuthException(DescribeError(errorCode), errorCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CloudRequestException($"Authentification Supabase : HTTP {(int)response.StatusCode}.");
        }

        TokenResponse token = await response.Content.ReadFromJsonAsync<TokenResponse>(SupabaseJson.Options)
            ?? throw new CloudRequestException("Réponse d'authentification Supabase vide.");

        return new AuthSession(
            token.AccessToken,
            token.RefreshToken,
            Time.GetUnixTimeFromSystem() + token.ExpiresIn,
            token.User.Id,
            token.User.Email ?? string.Empty);
    }
}
