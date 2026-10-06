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

    private static readonly HashSet<HttpStatusCode> RefusalStatuses =
    [
        HttpStatusCode.BadRequest,
        HttpStatusCode.Unauthorized,
        HttpStatusCode.Forbidden,
        HttpStatusCode.UnprocessableEntity,
        HttpStatusCode.TooManyRequests,
    ];

    private readonly HttpClient _http;
    private readonly SupabaseSettings _settings;

    public SupabaseAuth(HttpClient http, SupabaseSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<AuthSession> SignInAsync(string email, string password)
    {
        TokenResponse token = await PostAsync<PasswordGrant, TokenResponse>("token?grant_type=password", new PasswordGrant(email, password));
        return ToSession(token.AccessToken, token.RefreshToken, token.ExpiresIn, token.User);
    }

    public async Task<AuthSession?> SignUpAsync(string email, string password)
    {
        SignUpResponse response = await PostAsync<PasswordGrant, SignUpResponse>("signup", new PasswordGrant(email, password));
        if (response is not { AccessToken: string accessToken, RefreshToken: string refreshToken, ExpiresIn: long expiresIn, User: TokenUser user })
        {
            return null;
        }

        return ToSession(accessToken, refreshToken, expiresIn, user);
    }

    public async Task<AuthSession> RefreshAsync(string refreshToken)
    {
        try
        {
            TokenResponse token = await PostAsync<RefreshGrant, TokenResponse>("token?grant_type=refresh_token", new RefreshGrant(refreshToken));
            return ToSession(token.AccessToken, token.RefreshToken, token.ExpiresIn, token.User);
        }
        catch (CloudAuthException exception) when (!RevokedSessionCodes.Contains(exception.ErrorCode))
        {
            throw new CloudRequestException($"Renouvellement de session refusé temporairement ({exception.ErrorCode}).");
        }
    }

    private static AuthSession ToSession(string accessToken, string refreshToken, long expiresIn, TokenUser user) =>
        new(accessToken, refreshToken, Time.GetUnixTimeFromSystem() + expiresIn, user.Id, user.Email ?? string.Empty);

    private static string DescribeError(string errorCode) => errorCode switch
    {
        "invalid_credentials" or "invalid_grant" => "Email ou mot de passe incorrect.",
        "email_not_confirmed" => "Cet email n'a pas encore été confirmé.",
        "user_already_exists" or "email_exists" => "Un compte existe déjà avec cet email.",
        "weak_password" => "Mot de passe trop faible (6 caractères minimum).",
        "email_address_invalid" or "validation_failed" => "Adresse email invalide.",
        "signup_disabled" => "Les inscriptions sont fermées.",
        "over_email_send_rate_limit" or "over_request_rate_limit" => "Trop de tentatives, réessaie dans quelques minutes.",
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

    private async Task<TResponse> PostAsync<TBody, TResponse>(string endpoint, TBody body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_settings.Url}/auth/v1/{endpoint}");
        request.Headers.Add("apikey", _settings.PublishableKey);
        request.Content = JsonContent.Create(body, options: SupabaseJson.Options);

        using HttpResponseMessage response = await _http.SendAsync(request);
        if (RefusalStatuses.Contains(response.StatusCode))
        {
            string errorCode = ReadErrorCode(await response.Content.ReadAsStringAsync());
            throw new CloudAuthException(DescribeError(errorCode), errorCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CloudRequestException($"Authentification Supabase : HTTP {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(SupabaseJson.Options)
            ?? throw new CloudRequestException("Réponse d'authentification Supabase vide.");
    }
}
