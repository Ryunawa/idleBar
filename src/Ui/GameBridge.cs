using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Ui;

public sealed class GameBridge : IDisposable
{
    private const int HttpTimeoutSeconds = 10;
    private const double LongestStep = 0.25;
    private const float BannerSeconds = 5;
    private const string SessionPath = "user://session.dat";

    private readonly HttpClient? _http;
    private readonly OnlineSession? _session;
    private readonly GameDialogs? _dialogs;

    public GameBridge(Node host)
    {
        Tavern.Open = false;
        SupabaseSettings? settings = SupabaseSettings.FromProjectSettings();
        if (settings is null)
        {
            return;
        }

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
        SessionKeeper keeper = new(new SupabaseAuth(_http, settings), new SessionStore(ProjectSettings.GlobalizePath(SessionPath)));
        _session = new OnlineSession(keeper, new TavernApi(new SupabaseRpc(_http, settings)));
        _session.Changed += Synchronize;
        _session.Applied += (previous, current) =>
        {
            foreach (string message in SessionBanners.Describe(_session.World, previous, current))
            {
                Announced?.Invoke(message, BannerSeconds);
            }
        };
        Tavern.Paid += _session.Record;
        _dialogs = new GameDialogs(_session, host);
    }

    public event Action<string, float>? Announced;

    public Tavern Tavern { get; } = new(new Random());

    public DecorSet Decor { get; private set; } = DecorSet.Bare;

    public BarStatus Status => StatusBuilder.Describe(_session, Tavern);

    public bool SignedIn => _session?.Status is not (null or SessionStatus.SignedOut);

    public void Start() => _session?.Start();

    public void Tick(double delta)
    {
        Tavern.Update((float)Math.Min(delta, LongestStep));
        _session?.Tick(delta);
    }

    public Task FlushAsync() => _session?.FlushAsync() ?? Task.CompletedTask;

    public void OpenMenu(float scale) => _dialogs?.Open(scale);

    public void SignOut() => _session?.SignOut();

    public async void CollectTipJar()
    {
        if (_session is null)
        {
            return;
        }

        string? error = await ActionFeedback.CaptureAsync(_session.CollectTipJarAsync);
        if (error is not null)
        {
            Announced?.Invoke(error, BannerSeconds);
        }
    }

    public void Dispose() => _http?.Dispose();

    private void Synchronize()
    {
        Tavern.Open = _session!.Playing;
        if (_session.Tavern is not TavernData data)
        {
            Tavern.Configure(4, DrinkMenu.Starters, 0);
            Decor = DecorSet.Bare;
            return;
        }

        List<Drink> menu = data.Menu.Select(DrinkMenu.FromId).OfType<Drink>().ToList();
        Tavern.Configure(data.Stools, menu.Count > 0 ? menu : DrinkMenu.Starters, data.Helper);
        IEnumerable<string> unlocks = _session.World?.Upgrades
            .Where(upgrade => upgrade.Kind == "decor" && data.Upgrades.Contains(upgrade.Id))
            .Select(upgrade => upgrade.Value) ?? [];
        Decor = DecorSet.From(unlocks);
    }
}
