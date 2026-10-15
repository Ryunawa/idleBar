using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Desktop;
using IdleBar.Ui;

namespace IdleBar;

public partial class Main : Control
{
    private const double TrayRefreshIntervalSeconds = 2;
    private const double InterfaceRefreshSeconds = 0.1;
    private const string PreferencesPath = "user://preferences.cfg";
    private const string InstancePath = "user://instance.pid";
    private const int FlushTimeoutMilliseconds = 2000;
    private const double UpdateCleanUpSeconds = 5;

    private SingleInstance _instance = null!;
    private BarPlacement _placement = null!;
    private GameBridge _game = null!;
    private ExpandedBar _expanded = null!;
    private CollapsedBar _collapsedBar = null!;
    private TrayMenu _tray = null!;
    private SettingsWindow _settings = null!;
    private FaqWindow _faq = null!;
    private UpdateNotice _updates = null!;
    private double _sinceTrayRefresh = TrayRefreshIntervalSeconds;
    private double _sinceInterfaceRefresh;
    private Task _installation = Task.CompletedTask;
    private bool _installing;
    private bool _quitting;

    public override void _Ready()
    {
        _instance = SingleInstance.ReplaceRunningInstance(ProjectSettings.GlobalizePath(InstancePath));
        GetTree().AutoAcceptQuit = false;
        Theme = BarTheme.Create();
        _game = new GameBridge(this);
        BuildInterface();
        _game.GameChanged += game => _expanded.Lane.Game = game;
        _expanded.Lane.GoldArrived += _expanded.Glow;
        _game.Happened += _expanded.Lane.Receive;
        _expanded.Lane.ManageRequested += tab => _game.OpenTab(_placement.DialogScale, tab);
        _game.HeroWanted += () => _game.AskHero(_placement.DialogScale);

        BarPreferences preferences = BarPreferences.Load(PreferencesPath);
        _placement = new BarPlacement(GetWindow(), preferences);
        _placement.CollapsedChanged += OnCollapsedChanged;
        _placement.ApplyCollapsed(preferences.Collapsed);

        _game.Start();
        RefreshInterface();
        GetTree().CreateTimer(UpdateCleanUpSeconds).Timeout += () => SelfInstaller.ForThisBuild()?.CleanUpAsync();
    }

    public override void _Process(double delta)
    {
        _placement.Update();
        _game.Tick(delta);
        _sinceInterfaceRefresh += delta;
        if (_sinceInterfaceRefresh >= InterfaceRefreshSeconds)
        {
            _sinceInterfaceRefresh = 0;
            RefreshInterface();
        }

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
        _expanded.UpdateRequested += InstallUpdate;
        _expanded.MenuRequested += OpenMenu;

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
            _expanded.ShowUpdate(release.Name, SelfInstaller.ForThisBuild() is not null && release.WindowsPackage is not null);
            _game.Announce($"Nouvelle version {release.Name} disponible : clique sur « Mise à jour »");
        };

        _settings = new SettingsWindow();
        AddChild(_settings);
        _settings.ScreenChosen += device => _placement.MoveTo(device);

        _faq = new FaqWindow();
        AddChild(_faq);
        _settings.FaqRequested += () => _faq.Open(_placement.DialogScale);
        _settings.SignOutRequested += () =>
        {
            _game.SignOut();
            _settings.Hide();
        };
    }

    private void RefreshInterface()
    {
        BarStatus status = _game.Status;
        _expanded.Refresh(status);
        _collapsedBar.Refresh(status);
    }

    private void OnCollapsedChanged(bool collapsed)
    {
        _expanded.Visible = !collapsed;
        _collapsedBar.Visible = collapsed;
        _tray.SetCollapsed(collapsed);
    }

    private void OpenMenu()
    {
        if (_game.UpdateRequired && _updates.Latest is not null)
        {
            InstallUpdate();
            return;
        }

        _game.OpenMenu(_placement.DialogScale);
    }

    private async void InstallUpdate()
    {
        if (_installing || _updates.Latest is not ReleaseInfo release)
        {
            return;
        }

        SelfInstaller? installer = SelfInstaller.ForThisBuild();
        if (installer is null || release.WindowsPackage is not ReleaseAsset package)
        {
            OpenDownloadPage();
            return;
        }

        _installing = true;
        try
        {
            _game.Announce($"Téléchargement de la version {release.Name}…");
            await installer.PrepareAsync(package, new Progress<double>(share =>
                _expanded.ShowUpdateStep(release.Name, share < 1 ? $"téléchargement {(int)(share * 100)} %" : "installation…")));
            _installation = installer.InstallAsync();
            await _installation;
            _game.Announce($"Version {release.Name} installée : IdleBar redémarre.");
            Shutdown(installer);
        }
        catch (Exception exception) when (UpdateFailure.Matches(exception))
        {
            GD.PushWarning($"Mise à jour automatique impossible : {exception.Message}");
            _installing = false;
            _expanded.ShowUpdate(release.Name, automatic: true);
            _game.Announce("La mise à jour automatique a échoué : la page de téléchargement s'ouvre.");
            OpenDownloadPage();
        }
    }

    private void OpenDownloadPage()
    {
        if (_updates.Latest is { HasSafePage: true } release)
        {
            OS.ShellOpen(release.HtmlUrl);
        }
    }

    private void OpenSettings() =>
        _settings.Open(_placement.DialogScale, _placement.ScreenDevice, _placement.DetectScreens(), _game.SignedIn);

    private void Quit() => Shutdown(null);

    private async void Shutdown(SelfInstaller? relaunchFrom)
    {
        if (_quitting)
        {
            return;
        }

        _quitting = true;
        _expanded.Visible = false;
        _collapsedBar.Visible = false;
        await Task.WhenAny(_game.FlushAsync(), Task.Delay(FlushTimeoutMilliseconds));
        await Task.WhenAny(_installation);
        _placement.Dispose();
        _tray.Dismiss();
        relaunchFrom?.Relaunch();
        GetTree().Quit();
    }
}
