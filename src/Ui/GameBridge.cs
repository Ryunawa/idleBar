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
    private const float RefusalSeconds = 5;
    private const string SessionPath = "user://session.dat";

    private readonly HttpClient? _http;
    private readonly Doorbell? _doorbell;
    private readonly OnlineSession? _session;
    private readonly GameActions? _actions;
    private readonly Account? _account;
    private readonly GameDialogs? _dialogs;
    private readonly RegularBook _book = new();
    private bool _festivalAnnounced;
    private SessionKeeper? _keeper;
    private DecorSet _decor = DecorSet.Bare;
    private IReadOnlyList<string> _souvenirs = [];

    public GameBridge(Node host)
    {
        Tavern.Open = false;
        Tavern.Book = _book;
        Announced += (message, _) => Journal.Add(message);
        SupabaseSettings? settings = SupabaseSettings.FromProjectSettings();
        if (settings is null)
        {
            return;
        }

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
        SessionKeeper keeper = new(new SupabaseAuth(_http, settings), new SessionStore(ProjectSettings.GlobalizePath(SessionPath)));
        TavernApi api = new(new SupabaseRpc(_http, settings));
        _doorbell = new Doorbell(settings);
        _session = new OnlineSession(keeper, api, _doorbell);
        _actions = new GameActions(_session, api);
        _account = new Account(keeper, _session);
        _keeper = keeper;
        Visits = new VisitDesk(Tavern, _actions, _doorbell);
        Visits.Announced += (message, seconds) => Announced?.Invoke(message, seconds);
        Streets = new StreetDesk(Tavern, _actions, _doorbell);
        Streets.Announced += (message, seconds) => Announced?.Invoke(message, seconds);
        Rooms = new RoomDesk(_actions, _doorbell);
        Rooms.Announced += (message, seconds) => Announced?.Invoke(message, seconds);
        Rooms.ViewChanged += () => ViewChanged?.Invoke();
        Rooms.Spoken += line =>
        {
            Journal.Add($"{line.Name} : {line.Text}");
            Spoken?.Invoke(line);
        };
        _session.Changed += Synchronize;
        _session.Applied += (previous, current) =>
        {
            foreach (Announcement announcement in SessionBanners.Describe(_session.World, previous, current))
            {
                Announced?.Invoke(announcement.Text, announcement.Seconds);
            }

            if (!_festivalAnnounced && SessionBanners.Feast(DateTime.Now) is Announcement festival)
            {
                _festivalAnnounced = true;
                Announced?.Invoke(festival.Text, festival.Seconds);
            }
        };
        Tavern.Paid += _session.Record;
        _dialogs = new GameDialogs(_session, _account, _actions, Journal, host);
    }

    public event Action<string, float>? Announced;

    public event Action? ViewChanged;

    public event Action<ChatLine>? Spoken;

    public Tavern Tavern { get; } = new(new Random());

    public Journal Journal { get; } = new();

    public VisitDesk? Visits { get; }

    public StreetDesk? Streets { get; }

    public RoomDesk? Rooms { get; }

    public Tavern Shown => Rooms?.Away ?? Tavern;

    public bool Away => Rooms?.Away is not null;

    public TavernData? Data => _session?.Tavern;

    public DecorSet Decor => Away ? Rooms!.Decor : _decor;

    public IReadOnlyList<string> Souvenirs => Away ? Rooms!.Souvenirs : _souvenirs;

    public Guid? Me => Guid.TryParse(_keeper?.UserId, out Guid me) ? me : null;

    public BarStatus Status => StatusBuilder.Describe(_session, Tavern, Rooms);

    public bool SignedIn => _session?.Status is not (null or SessionStatus.SignedOut);

    public void Start() => _session?.Start();

    public void Tick(double delta)
    {
        Tavern.Update((float)Math.Min(delta, LongestStep));
        Rooms?.Tick((float)Math.Min(delta, LongestStep));
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

    public async void CollectTipJar()
    {
        if (_actions is null)
        {
            return;
        }

        string? error = await ActionFeedback.CaptureAsync(_actions.CollectTipJarAsync);
        if (error is not null)
        {
            Announced?.Invoke(error, RefusalSeconds);
        }
    }

    public PatronLook? LookOf(Patron patron) =>
        (Visits?.LookOf(patron) ?? RegularLooks.For(patron.Regular)) ?? Costumes.For(patron.Look, Calendar.FestivalOf(DateTime.Now));

    public NameTag? TagOf(Patron patron) => patron.Guest is string guest ? new NameTag(guest, BarPalette.Gold, false) : _book.Tag(patron.Regular);

    public string? Describe(Patron patron) => Visits?.Describe(patron, !Away) ?? _book.Describe(patron.Regular);

    public Guid? PlayerOf(Patron patron) => Visits?.PlayerOf(patron);

    public void OpenProfile(Guid friend) => _dialogs?.OpenProfile(friend);

    public void Dispose()
    {
        _doorbell?.Dispose();
        _http?.Dispose();
    }

    private void Synchronize()
    {
        Tavern.Open = _session!.Playing;
        _book.Update(_session.World, _session.Tavern);
        Visits?.Sync(_session.Tavern);
        Streets?.Sync(_session.Tavern);
        Rooms?.Sync(_session.Tavern, _session.World);
        if (_session.Tavern is not TavernData data)
        {
            Tavern.Configure(4, DrinkMenu.Starters, 0);
            _decor = DecorSet.Bare;
            _souvenirs = [];
            return;
        }

        _souvenirs = data.Regulars.Where(progress => progress.Chapter >= 5).Select(progress => progress.Id).ToList();
        TavernSetup.Configure(Tavern, data.Stools, data.Menu, data.Helper);
        _decor = TavernSetup.Decor(_session.World, data.Upgrades);
    }
}
