using System;
using System.Text.Json;
using Godot;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class Doorbell : IDisposable
{
    private const string Refresh = "refresh";
    private const string Emote = "emote";

    private readonly SupabaseSettings _settings;
    private RealtimeClient? _client;
    private string? _userId;

    public Doorbell(SupabaseSettings settings)
    {
        _settings = settings;
    }

    public event Action? Rang;

    public event Action<EmoteData>? Emoted;

    public bool Connected => _client?.Connected == true;

    public void Listen(string userId, string token)
    {
        if (_client is not null && _userId == userId)
        {
            _client.UpdateToken(token);
            return;
        }

        Stop();
        _userId = userId;
        _client = new RealtimeClient(_settings.Url, _settings.PublishableKey, userId, token);
        _client.Start();
    }

    public void Stop()
    {
        _client?.Dispose();
        _client = null;
        _userId = null;
    }

    public void Drain()
    {
        while (_client is not null && _client.TryNext(out RealtimeEvent? next) && next is not null)
        {
            switch (next.Name)
            {
                case Refresh:
                    Rang?.Invoke();
                    break;
                case Emote when Parse(next.Payload) is EmoteData emote:
                    Emoted?.Invoke(emote);
                    break;
                case RealtimeEvent.Lost:
                    GD.PushWarning($"Sonnette coupée, nouvel essai bientôt : {next.Payload}");
                    break;
            }
        }
    }

    public void Dispose() => Stop();

    private static EmoteData? Parse(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<EmoteData>(payload, SupabaseJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
