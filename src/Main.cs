using Godot;
using IdleBar.Desktop;
using IdleBar.Ui;

namespace IdleBar;

public partial class Main : Control
{
    private const double TrayRefreshIntervalSeconds = 2;
    private const string PreferencesPath = "user://preferences.cfg";
    private const string InstancePath = "user://instance.pid";

    private SingleInstance _instance = null!;
    private BarPlacement _placement = null!;
    private TavernSession _session = null!;
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
        _session = new TavernSession();
        BuildInterface();
        _expanded.Attach(_session.Tavern);

        BarPreferences preferences = BarPreferences.Load(PreferencesPath);
        _placement = new BarPlacement(GetWindow(), preferences);
        _placement.CollapsedChanged += OnCollapsedChanged;
        _placement.ApplyCollapsed(preferences.Collapsed);
        RefreshInterface();
    }

    public override void _Process(double delta)
    {
        _placement.Update();
        _session.Tick(delta);
        RefreshInterface();

        _sinceTrayRefresh += delta;
        if (_sinceTrayRefresh >= TrayRefreshIntervalSeconds)
        {
            _sinceTrayRefresh = 0;
            _tray.Refresh($"IdleBar · {_session.Status.Compact}");
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
        _session.Save();
        _placement.Dispose();
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

        _collapsedBar = new CollapsedBar();
        AddChild(_collapsedBar);
        _collapsedBar.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _collapsedBar.ToggleRequested += () => _placement.ToggleCollapsed();
        _collapsedBar.QuitRequested += Quit;
        _collapsedBar.SettingsRequested += OpenSettings;

        _tray = new TrayMenu();
        AddChild(_tray);
        _tray.ToggleRequested += () => _placement.ToggleCollapsed();
        _tray.SettingsRequested += OpenSettings;
        _tray.QuitRequested += Quit;

        _updates = new UpdateNotice();
        AddChild(_updates);
        _updates.Found += release => _expanded.ShowUpdate(release.Name);

        _settings = new SettingsWindow();
        AddChild(_settings);
        _settings.SizeChosen += size => _placement.Resize(size);
        _settings.ScreenChosen += device => _placement.MoveTo(device);
    }

    private void RefreshInterface()
    {
        BarStatus status = _session.Status;
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

    private void Quit()
    {
        _session.Save();
        _placement.Dispose();
        _tray.Dismiss();
        GetTree().Quit();
    }
}
