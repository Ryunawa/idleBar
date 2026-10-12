using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Frontier;
using IdleBar.Online;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Ui;

public sealed class GameBridge : IDisposable
{
    private const int HttpTimeoutSeconds = 10;
    private const string SessionPath = "user://session.dat";

    private readonly HttpClient? _http;
    private readonly FrontierSession? _session;
    private readonly Account? _account;
    private readonly GameDialogs? _dialogs;

    public GameBridge(Node host)
    {
        Announced += (message, _) => Journal.Add(message);
        SupabaseSettings? settings = SupabaseSettings.FromProjectSettings();
        if (settings is null)
        {
            return;
        }

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
        SessionKeeper keeper = new(new SupabaseAuth(_http, settings), new SessionStore(ProjectSettings.GlobalizePath(SessionPath)));
        _session = new FrontierSession(keeper, new FrontierApi(new SupabaseRpc(_http, settings)));
        _account = new Account(keeper, _session);
        _dialogs = new GameDialogs(_session, _account, host);
    }

    public event Action<string, float>? Announced;

    public event Action<IReadOnlyList<FrontierEvent>>? Happened;

    public Journal Journal { get; } = new();

    public FrontierGame Game { get; } = new(new FrontierState(), new Random());

    public BarStatus Status => StatusBuilder.Describe(_session, Game);

    public bool SignedIn => _session?.Status is not (null or SessionStatus.SignedOut);

    public void Start() => _session?.Start();

    public void Tick(double delta)
    {
        Game.Advance(delta);
        IReadOnlyList<FrontierEvent> events = Game.Events.Drain();
        if (events.Count > 0)
        {
            Happened?.Invoke(events);
        }

        _session?.Tick(delta);
    }

    public Task FlushAsync() => _session?.FlushAsync() ?? Task.CompletedTask;

    public void Announce(string text, float seconds) => Announced?.Invoke(text, seconds);

    public void OpenMenu(float scale)
    {
        if (_session?.Status == SessionStatus.UpdateRequired)
        {
            if (GameVersion.DownloadUrl.StartsWith("https://github.com/", StringComparison.Ordinal))
            {
                OS.ShellOpen(GameVersion.DownloadUrl);
            }

            return;
        }

        _dialogs?.Open(scale);
    }

    public void SignOut() => _account?.SignOut();

    public void Dispose() => _http?.Dispose();
}
