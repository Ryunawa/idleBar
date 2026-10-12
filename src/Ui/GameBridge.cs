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
        _session.Loaded += Load;
        _session.Changed += () =>
        {
            if (_session.Status == SessionStatus.SignedOut && Game is not null)
            {
                SetGame(null);
            }
        };
        _session.Noticed += text => Journal.Add(text);
        _account = new Account(keeper, _session);
        _dialogs = new GameDialogs(_session, _account, Journal, host);
    }

    public event Action<string, float>? Announced;

    public event Action<IReadOnlyList<FrontierEvent>>? Happened;

    public event Action<FrontierGame?>? GameChanged;

    public Journal Journal { get; } = new();

    public FrontierGame? Game { get; private set; }

    public BarStatus Status => StatusBuilder.Describe(_session, Game);

    public bool SignedIn => _session?.Status is not (null or SessionStatus.SignedOut);

    public void Start() => _session?.Start();

    public void Tick(double delta)
    {
        if (Game is FrontierGame game && _session?.Status is not (SessionStatus.Elsewhere or SessionStatus.UpdateRequired))
        {
            game.Advance(delta);
            Report(game, game.Events.Drain());
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

    public void OpenTab(float scale, WindowTab tab) => _dialogs?.Open(scale, tab);

    public void SignOut() => _account?.SignOut();

    public void Dispose() => _http?.Dispose();

    private void Report(FrontierGame game, IReadOnlyList<FrontierEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        foreach (FrontierEvent frontierEvent in events)
        {
            if (FrontierJournal.Describe(frontierEvent, game.State) is string line)
            {
                Journal.Add(line);
            }

            if (frontierEvent is Retreated or AchievementUnlocked or PlotConquered or Built { Level: 1 })
            {
                _session?.RequestSave();
            }
        }

        Happened?.Invoke(events);
    }

    private void Load(LoadPlan plan)
    {
        FrontierGame game = new(plan.State, new Random());
        if (plan.Away >= 1)
        {
            game.Absent(plan.Away);
        }

        SetGame(game);
    }

    private void SetGame(FrontierGame? game)
    {
        Game = game;
        if (_session is not null)
        {
            _session.Game = game;
        }

        if (_dialogs is not null)
        {
            _dialogs.Game = game;
        }

        GameChanged?.Invoke(game);
    }
}
