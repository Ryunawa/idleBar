using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Godot;
using IdleBar.Game;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Cloud;

public sealed class SupabaseSaves
{
    private const string Columns = "data,revision,active_device,updated_at";

    private readonly HttpClient _http;
    private readonly SupabaseSettings _settings;

    public SupabaseSaves(HttpClient http, SupabaseSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<CloudSnapshot?> FetchAsync(string accessToken)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"saves?select={Columns}", accessToken);
        return await SendAsync(request);
    }

    public async Task<CloudSnapshot> CreateAsync(string accessToken, ProgressData progress, string device)
    {
        using HttpRequestMessage request = CreateWriteRequest(HttpMethod.Post, $"saves?select={Columns}", accessToken, progress, device);
        return await SendAsync(request)
            ?? throw new CloudRequestException("Supabase n'a pas renvoyé la sauvegarde créée.");
    }

    public async Task<CloudSnapshot?> TryUpdateAsync(string accessToken, long expectedRevision, ProgressData progress, string device)
    {
        using HttpRequestMessage request = CreateWriteRequest(
            HttpMethod.Patch,
            $"saves?revision=eq.{expectedRevision}&select={Columns}",
            accessToken,
            progress,
            device);
        return await SendAsync(request);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string accessToken)
    {
        HttpRequestMessage request = new(method, $"{_settings.Url}/rest/v1/{path}");
        request.Headers.Add("apikey", _settings.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private HttpRequestMessage CreateWriteRequest(HttpMethod method, string path, string accessToken, ProgressData progress, string device)
    {
        HttpRequestMessage request = CreateRequest(method, path, accessToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = JsonContent.Create(new CloudSaveWrite(progress, device), options: SupabaseJson.Options);
        return request;
    }

    private async Task<CloudSnapshot?> SendAsync(HttpRequestMessage request)
    {
        using HttpResponseMessage response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new CloudRequestException($"Supabase a répondu HTTP {(int)response.StatusCode} : {body}");
        }

        List<CloudSaveRow> rows = await response.Content.ReadFromJsonAsync<List<CloudSaveRow>>(SupabaseJson.Options) ?? [];
        if (rows.Count == 0)
        {
            return null;
        }

        CloudSaveRow row = rows[0];
        DateTimeOffset serverTime = response.Headers.Date
            ?? DateTimeOffset.FromUnixTimeMilliseconds((long)(Time.GetUnixTimeFromSystem() * 1000));
        return new CloudSnapshot(row.Data, row.Revision, row.ActiveDevice, row.UpdatedAt, serverTime);
    }
}
