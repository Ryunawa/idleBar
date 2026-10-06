using System;
using System.Globalization;
using Godot;
using IdleBar.Cloud;
using IdleBar.Desktop;
using IdleBar.Trade;
using IdleBar.Ui;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar;

public partial class Main : Control
{
    private const double TrayRefreshIntervalSeconds = 2;
    private const float ArrivalBannerSeconds = 8;
    private const float HintBannerSeconds = 4;
    private const float ProductionBannerSeconds = 6;
    private const int HttpTimeoutSeconds = 10;
    private const string PreferencesPath = "user://preferences.cfg";
    private const string SessionPath = "user://session.dat";
    private const string InstancePath = "user://instance.pid";
    private const string MissingConfiguration = "Supabase n'est pas configuré";

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private SingleInstance _instance = null!;
    private BarPlacement _placement = null!;
    private ExpandedBar _expanded = null!;
    private CollapsedBar _collapsedBar = null!;
    private TrayMenu _tray = null!;
    private SettingsWindow _settings = null!;
    private BarStatus _status = BarStatusBuilder.Unavailable(MissingConfiguration);
    private HttpClient? _http;
    private GameSession? _session;
    private GameDialogs? _dialogs;
    private double _sinceTrayRefresh = TrayRefreshIntervalSeconds;

    public override void _Ready()
    {
        _instance = SingleInstance.ReplaceRunningInstance(ProjectSettings.GlobalizePath(InstancePath));
        GetTree().AutoAcceptQuit = false;
        Theme = BarTheme.Create();
        _session = CreateSession();
        BuildInterface();

        BarPreferences preferences = BarPreferences.Load(PreferencesPath);
        _placement = new BarPlacement(GetWindow(), preferences);
        _placement.CollapsedChanged += OnCollapsedChanged;
        _placement.ApplyCollapsed(preferences.Collapsed);

        _session?.Start();
        RefreshInterface();
    }

    public override void _Process(double delta)
    {
        _placement.Update();
        _session?.Tick(delta);
        RefreshInterface();

        _sinceTrayRefresh += delta;
        if (_sinceTrayRefresh >= TrayRefreshIntervalSeconds)
        {
            _sinceTrayRefresh = 0;
            _tray.Refresh($"IdleBar · {_status.Compact}", _session?.Status is not (null or SessionStatus.SignedOut));
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Quit();
        }
    }

    public override void _ExitTree()
    {
        _placement.Dispose();
        _http?.Dispose();
        _instance.Dispose();
    }

    private GameSession? CreateSession()
    {
        SupabaseSettings? settings = SupabaseSettings.FromProjectSettings();
        if (settings is null || !OperatingSystem.IsWindows())
        {
            return null;
        }

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
        SessionKeeper keeper = new(new SupabaseAuth(_http, settings), new SessionStore(ProjectSettings.GlobalizePath(SessionPath)));
        GameApi api = new(new SupabaseRpc(_http, settings));
        GameSession session = new(keeper, api, new ServerClock());
        session.Arrived += town => _expanded.Lane.ShowBanner($"Ta caravane est arrivée à {town.Name}", ArrivalBannerSeconds);
        session.ProductionDelivered += (recipe, batches) => ShowDelivery(session, recipe, batches);
        _dialogs = new GameDialogs(session, new GameActions(session, api), this);
        return session;
    }

    private void BuildInterface()
    {
        Panel background = new() { MouseFilter = MouseFilterEnum.Ignore };
        background.AddThemeStyleboxOverride("panel", BarTheme.CreateBackground());
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _expanded = new ExpandedBar();
        AddChild(_expanded);
        _expanded.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _expanded.ToggleRequested += () => _placement.ToggleCollapsed();
        _expanded.QuitRequested += Quit;
        _expanded.ActionRequested += OnBarActionRequested;
        _expanded.SettingsRequested += OpenSettings;

        _collapsedBar = new CollapsedBar();
        AddChild(_collapsedBar);
        _collapsedBar.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _collapsedBar.ToggleRequested += () => _placement.ToggleCollapsed();
        _collapsedBar.QuitRequested += Quit;
        _collapsedBar.SettingsRequested += OpenSettings;

        _tray = new TrayMenu();
        AddChild(_tray);
        _tray.ToggleRequested += () => _placement.ToggleCollapsed();
        _tray.SignOutRequested += () => _session?.SignOut();
        _tray.SettingsRequested += OpenSettings;
        _tray.QuitRequested += Quit;

        _settings = new SettingsWindow();
        AddChild(_settings);
        _settings.SizeChosen += size => _placement.Resize(size);
        _settings.ScreenChosen += device => _placement.MoveTo(device);
    }

    private void RefreshInterface()
    {
        _status = _session is null ? BarStatusBuilder.Unavailable(MissingConfiguration) : BarStatusBuilder.Describe(_session);
        _expanded.Refresh(_status);
        _collapsedBar.Refresh(_status);
    }

    private void OnCollapsedChanged(bool collapsed)
    {
        _expanded.Visible = !collapsed;
        _collapsedBar.Visible = collapsed;
        _tray.SetCollapsed(collapsed);
    }

    private void OnBarActionRequested()
    {
        if (_session is { Status: SessionStatus.Ready, IsTravelling: true })
        {
            _expanded.Lane.ShowBanner(_status.Slot.Tooltip, HintBannerSeconds);
            return;
        }

        _dialogs?.OpenFor(_placement.DialogScale);
    }

    private void OpenSettings()
    {
        DisplayScreen[] noScreens = [];
        _settings.Open(
            _placement.DialogScale,
            _placement.Size,
            _placement.ScreenDevice,
            OperatingSystem.IsWindows() ? DisplayScreens.Detect() : noScreens);
    }

    private void ShowDelivery(GameSession session, RecipeInfo recipe, int batches)
    {
        string good = session.World?.GoodName(recipe.OutputGoodId).ToLower(French) ?? recipe.OutputGoodId;
        _expanded.Lane.ShowBanner($"+{NumberFormat.Amount(batches * recipe.OutputQuantity)} {good} à l'entrepôt", ProductionBannerSeconds);
    }

    private void Quit()
    {
        _placement.Dispose();
        _tray.Dismiss();
        GetTree().Quit();
    }
}
