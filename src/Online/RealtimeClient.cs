using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace IdleBar.Online;

public sealed class RealtimeClient : IDisposable
{
    private const int HeartbeatSeconds = 25;
    private const int FirstRetrySeconds = 2;
    private const int LastRetrySeconds = 60;
    private const int BufferSize = 16384;

    private readonly Uri _endpoint;
    private readonly string _topic;
    private readonly ConcurrentQueue<RealtimeEvent> _events = new();
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private string _token;
    private bool _joined;
    private bool _wasJoined;
    private int _reference;

    public RealtimeClient(string supabaseUrl, string apiKey, string userId, string token)
    {
        string socketUrl = supabaseUrl.Replace("https://", "wss://").Replace("http://", "ws://");
        _endpoint = new Uri($"{socketUrl}/realtime/v1/websocket?apikey={Uri.EscapeDataString(apiKey)}&vsn=1.0.0");
        _topic = $"realtime:taverne:{userId}";
        _token = token;
    }

    public bool Connected
    {
        get
        {
            lock (_gate)
            {
                return _joined;
            }
        }
    }

    public void Start() => _ = RunAsync(_stop.Token);

    public void UpdateToken(string token)
    {
        lock (_gate)
        {
            _token = token;
        }
    }

    public bool TryNext(out RealtimeEvent? next) => _events.TryDequeue(out next);

    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
    }

    private async Task RunAsync(CancellationToken stop)
    {
        int retry = FirstRetrySeconds;
        while (!stop.IsCancellationRequested)
        {
            _wasJoined = false;
            try
            {
                await ConnectAsync(stop);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _events.Enqueue(new RealtimeEvent(RealtimeEvent.Lost, exception.Message));
            }

            if (_wasJoined)
            {
                retry = FirstRetrySeconds;
            }

            SetJoined(false);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(retry), stop);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            retry = Math.Min(retry * 2, LastRetrySeconds);
        }
    }

    private async Task ConnectAsync(CancellationToken stop)
    {
        using ClientWebSocket socket = new();
        await socket.ConnectAsync(_endpoint, stop);
        string sentToken = CurrentToken();
        string joinReference = NextReference();
        await SendAsync(socket, RealtimeMessages.Join(_topic, sentToken, joinReference), stop);
        Task receiving = ReceiveAsync(socket, stop);
        while (socket.State == WebSocketState.Open)
        {
            Task finished = await Task.WhenAny(receiving, Task.Delay(TimeSpan.FromSeconds(HeartbeatSeconds), stop));
            if (finished == receiving)
            {
                await receiving;
                return;
            }

            string token = CurrentToken();
            if (token != sentToken)
            {
                await SendAsync(socket, RealtimeMessages.AccessToken(_topic, token, NextReference(), joinReference), stop);
                sentToken = token;
            }

            await SendAsync(socket, RealtimeMessages.Heartbeat(NextReference()), stop);
        }
    }

    private async Task ReceiveAsync(ClientWebSocket socket, CancellationToken stop)
    {
        byte[] buffer = new byte[BufferSize];
        using MemoryStream message = new();
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, stop);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            bool keep = Handle(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
            message.SetLength(0);
            if (!keep)
            {
                return;
            }
        }
    }

    private bool Handle(string text)
    {
        (RealtimeSignal signal, RealtimeEvent? received) = RealtimeMessages.Read(text, _topic);
        if (signal == RealtimeSignal.Joined)
        {
            SetJoined(true);
            _wasJoined = true;
        }
        else if (received is not null)
        {
            _events.Enqueue(received);
        }

        return signal != RealtimeSignal.Dropped;
    }

    private static Task SendAsync(ClientWebSocket socket, string message, CancellationToken stop) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, stop);

    private string NextReference() => Interlocked.Increment(ref _reference).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private string CurrentToken()
    {
        lock (_gate)
        {
            return _token;
        }
    }

    private void SetJoined(bool joined)
    {
        lock (_gate)
        {
            _joined = joined;
        }
    }
}
