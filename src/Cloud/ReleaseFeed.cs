using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Godot;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Cloud;

public sealed class ReleaseFeed
{
    private const string UrlSetting = "idlebar/updates/latest_release_url";
    private const string UserAgent = "IdleBar";
    private const string GitHubJson = "application/vnd.github+json";

    private readonly HttpClient _http;
    private readonly string _url;

    private ReleaseFeed(HttpClient http, string url)
    {
        _http = http;
        _url = url;
    }

    public static ReleaseFeed? FromProjectSettings(HttpClient http)
    {
        string url = ProjectSettings.GetSetting(UrlSetting, string.Empty).AsString();
        return string.IsNullOrWhiteSpace(url) ? null : new ReleaseFeed(http, url);
    }

    public async Task<ReleaseInfo?> FetchLatestAsync()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, _url);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.ParseAdd(GitHubJson);
        using HttpResponseMessage response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        ReleaseInfo? release = await response.Content.ReadFromJsonAsync<ReleaseInfo>(SupabaseJson.Options);
        return release is { TagName.Length: > 0, HtmlUrl.Length: > 0 } ? release : null;
    }
}