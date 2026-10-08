using System;
using System.Text.Json;
using Godot;
using IdleBar.Cloud;

namespace IdleBar.Online;

public sealed class Doorbell : IDisposable
{
    private const string Refresh = "refresh";
    private const string Emote = "emote";
    private const string Wave = "wave";

    private readonly SupabaseSettings _settings;
    private RealtimeClient? _client;
    private string? _userId;

    public Doorbell(SupabaseSettings settings)
    {
        _settings = settings;
    }

    public event Action? Rang;

    public event Action<EmoteData>? Emoted;

    public event Action<WaveData>? Waved;

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
                case Emote when Parse<EmoteData>(next.Payload) is EmoteData emote:
                    Emoted?.Invoke(emote);
                    break;
                case Wave when Parse<WaveData>(next.Payload) is WaveData wave:
                    Waved?.Invoke(wave);
                    break;
                case RealtimeEvent.Lost:
                    GD.PushWarning($"Sonnette coupée, nouvel essai bientôt : {next.Payload}");
                    break;
            }
        }
    }

    public void Dispose() => Stop();

    private static TData? Parse<TData>(string payload)
        where TData : class
    {
        try
        {
            return JsonSerializer.Deserialize<TData>(payload, SupabaseJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
