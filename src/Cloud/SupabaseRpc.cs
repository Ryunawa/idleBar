using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Cloud;

public sealed class SupabaseRpc
{
    private const string RaisedExceptionCode = "P0001";
    private const string UnknownFunctionCode = "PGRST202";

    private readonly HttpClient _http;
    private readonly SupabaseSettings _settings;

    public SupabaseRpc(HttpClient http, SupabaseSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<TResult> CallAsync<TArguments, TResult>(string accessToken, string function, TArguments arguments)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_settings.Url}/rest/v1/rpc/{function}");
        request.Headers.Add("apikey", _settings.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(arguments, options: SupabaseJson.Options);

        using HttpResponseMessage response = await _http.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw CreateFailure(function, (int)response.StatusCode, body);
        }

        return JsonSerializer.Deserialize<TResult>(body, SupabaseJson.Options)
            ?? throw new CloudRequestException($"Supabase n'a rien renvoyé pour {function}.");
    }

    private static Exception CreateFailure(string function, int status, string body)
    {
        RpcError? error = TryReadError(body);
        return error switch
        {
            { Code: RaisedExceptionCode, Message: not null } => new ActionRefusedException(error.Message),
            { Code: UnknownFunctionCode } => new CloudRequestException(
                $"La fonction {function} n'existe pas sur Supabase : exécute les scripts du dossier supabase."),
            _ => new CloudRequestException($"{function} : HTTP {status} {error?.Message ?? body}"),
        };
    }

    private static RpcError? TryReadError(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<RpcError>(body, SupabaseJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
