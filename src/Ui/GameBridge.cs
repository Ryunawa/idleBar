using System;
using Godot;
using IdleBar.Cloud;
using IdleBar.Trade;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Ui;

public sealed class GameBridge : IDisposable
{
    private const float HintBannerSeconds = 4;
    private const float RefusalBannerSeconds = 6;
    private const int HttpTimeoutSeconds = 10;
    private const string SessionPath = "user://session.dat";
    private const string RecipeMemoryPath = "user://workshop.cfg";
    private const string MissingConfiguration = "Supabase n'est pas configuré";

    private readonly HttpClient? _http;
    private readonly GameSession? _session;
    private readonly GameDialogs? _dialogs;
    private readonly QuickRelaunch? _relaunch;
    private bool _relaunching;

    public GameBridge(Node host)
    {
        SupabaseSettings? settings = SupabaseSettings.FromProjectSettings();
        if (settings is null || !OperatingSystem.IsWindows())
        {
            return;
        }

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
        SessionKeeper keeper = new(new SupabaseAuth(_http, settings), new SessionStore(ProjectSettings.GlobalizePath(SessionPath)));
        GameApi api = new(new SupabaseRpc(_http, settings));
        GameSession session = new(keeper, api, new ServerClock());
        SessionBanners banners = new(session);
        banners.Announced += (message, seconds) => Announced?.Invoke(message, seconds);

        GameActions actions = new(session, api);
        _dialogs = new GameDialogs(session, actions, host);
        _relaunch = new QuickRelaunch(session, actions, RecipeMemory.Load(RecipeMemoryPath));
        _session = session;
    }

    public event Action<string, float>? Announced;

    public BarStatus Status { get; private set; } = BarStatusBuilder.Unavailable(MissingConfiguration);

    public bool SignedIn => _session?.Status is not (null or SessionStatus.SignedOut);

    public void Start() => _session?.Start();

    public void Tick(double delta)
    {
        if (_session is null)
        {
            return;
        }

        _session.Tick(delta);
        Status = BarStatusBuilder.Describe(_session, _relaunch?.Candidate);
    }

    public void SignOut() => _session?.SignOut();

    public void OpenFromLane(float dialogScale) => _dialogs?.OpenFor(dialogScale);

    public async void PressSlot(float dialogScale)
    {
        if (_relaunch?.Candidate is not RecipeInfo recipe)
        {
            OpenFromLane(dialogScale);
            return;
        }

        if (_relaunching)
        {
            return;
        }

        _relaunching = true;
        RelaunchOutcome outcome = await _relaunch.RelaunchAsync(recipe);
        _relaunching = false;
        Announced?.Invoke(outcome.Message, outcome.Started ? HintBannerSeconds : RefusalBannerSeconds);
        if (!outcome.Started)
        {
            _dialogs?.OpenFor(dialogScale);
        }
    }

    public void PressNews(float dialogScale)
    {
        if (_session?.Snapshot is not GameSnapshot snapshot)
        {
            return;
        }

        if (snapshot.TripEvent is not null || snapshot.News.Total == 0)
        {
            _dialogs?.OpenFor(dialogScale, TownTab.Journal);
            return;
        }

        if (_session.IsTravelling)
        {
            Announced?.Invoke(ExchangeText.News(snapshot.News), HintBannerSeconds);
            _dialogs?.MarkNewsSeen();
            return;
        }

        _dialogs?.OpenFor(dialogScale, snapshot.News.Offers > 0 ? TownTab.Counter : TownTab.Contracts);
    }

    public void Dispose() => _http?.Dispose();
}
