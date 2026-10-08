using System.Threading.Tasks;
using Godot;
using IdleBar.Desktop;
using IdleBar.Ui;

namespace IdleBar;

public partial class Main : Control
{
    private const double TrayRefreshIntervalSeconds = 2;
    private const string PreferencesPath = "user://preferences.cfg";
    private const string InstancePath = "user://instance.pid";
    private const float UpdateBannerSeconds = 10;
    private const int FlushTimeoutMilliseconds = 2000;

    private SingleInstance _instance = null!;
    private BarPlacement _placement = null!;
    private GameBridge _game = null!;
    private ExpandedBar _expanded = null!;
    private CollapsedBar _collapsedBar = null!;
    private TrayMenu _tray = null!;
    private SettingsWindow _settings = null!;
    private UpdateNotice _updates = null!;
    private double _sinceTrayRefresh = TrayRefreshIntervalSeconds;

    public override void _Ready()
    {
        _instance = SingleInstance.ReplaceRunningInstance(ProjectSettings.GlobalizePath(InstancePath));
        GetTree().AutoAcceptQuit = false;
        Theme = BarTheme.Create();
        _game = new GameBridge(this);
        BuildInterface();
        _expanded.Attach(_game.Tavern);
        _expanded.Lane.Describe = _game.Describe;
        _expanded.Lane.LookOf = _game.LookOf;
        if (_game.Visits is VisitDesk visits)
        {
            _expanded.Lane.PatronClicked += visits.Cheer;
            _expanded.EmoteRequested += visits.Emote;
            visits.Popped += (text, x, color) => _expanded.Lane.Pop(text, x, color);
        }
        _game.Announced += (message, seconds) => _expanded.Lane.Announce(message, seconds);

        BarPreferences preferences = BarPreferences.Load(PreferencesPath);
        _placement = new BarPlacement(GetWindow(), preferences);
        _placement.CollapsedChanged += OnCollapsedChanged;
        _placement.ApplyCollapsed(preferences.Collapsed);

        _game.Start();
        RefreshInterface();
    }

    public override void _Process(double delta)
    {
        _placement.Update();
        _game.Tick(delta);
        RefreshInterface();

        _sinceTrayRefresh += delta;
        if (_sinceTrayRefresh >= TrayRefreshIntervalSeconds)
        {
            _sinceTrayRefresh = 0;
            _tray.Refresh($"IdleBar · {_game.Status.Compact}", _game.SignedIn);
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
        _game.Dispose();
        _instance.Dispose();
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
        _expanded.SettingsRequested += OpenSettings;
        _expanded.UpdateRequested += OpenDownloadPage;
        _expanded.MenuRequested += () => _game.OpenMenu(_placement.DialogScale);
        _expanded.TipJarRequested += () => _game.CollectTipJar();

        _collapsedBar = new CollapsedBar();
        AddChild(_collapsedBar);
        _collapsedBar.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _collapsedBar.ToggleRequested += () => _placement.ToggleCollapsed();
        _collapsedBar.QuitRequested += Quit;
        _collapsedBar.SettingsRequested += OpenSettings;

        _tray = new TrayMenu();
        AddChild(_tray);
        _tray.ToggleRequested += () => _placement.ToggleCollapsed();
        _tray.SignOutRequested += () => _game.SignOut();
        _tray.SettingsRequested += OpenSettings;
        _tray.QuitRequested += Quit;

        _updates = new UpdateNotice();
        AddChild(_updates);
        _updates.Found += release =>
        {
            _expanded.ShowUpdate(release.Name);
            _expanded.Lane.Announce($"Nouvelle version {release.Name} disponible : clique sur « Mise à jour »", UpdateBannerSeconds);
        };

        _settings = new SettingsWindow();
        AddChild(_settings);
        _settings.SizeChosen += size => _placement.Resize(size);
        _settings.ScreenChosen += device => _placement.MoveTo(device);
    }

    private void RefreshInterface()
    {
        BarStatus status = _game.Status;
        _expanded.Lane.Decor = _game.Decor;
        _expanded.Lane.Souvenirs = _game.Souvenirs;
        _expanded.Refresh(status);
        _collapsedBar.Refresh(status);
    }

    private void OnCollapsedChanged(bool collapsed)
    {
        _expanded.Visible = !collapsed;
        _collapsedBar.Visible = collapsed;
        _tray.SetCollapsed(collapsed);
    }

    private void OpenDownloadPage()
    {
        if (_updates.Latest is { HasSafePage: true } release)
        {
            OS.ShellOpen(release.HtmlUrl);
        }
    }

    private void OpenSettings() =>
        _settings.Open(_placement.DialogScale, _placement.Size, _placement.ScreenDevice, _placement.DetectScreens());

    private async void Quit()
    {
        _expanded.Visible = false;
        _collapsedBar.Visible = false;
        await Task.WhenAny(_game.FlushAsync(), Task.Delay(FlushTimeoutMilliseconds));
        _placement.Dispose();
        _tray.Dismiss();
        GetTree().Quit();
    }
}
