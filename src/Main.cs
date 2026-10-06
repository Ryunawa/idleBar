using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Desktop;
using IdleBar.Game;
using IdleBar.Ui;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar;

public partial class Main : Control
{
    private const int ExpandedHeight = 56;
    private const int CollapsedHeight = 24;
    private const int ActiveFramesPerSecond = 30;
    private const int BackgroundFramesPerSecond = 5;
    private const double AutosaveIntervalSeconds = 15;
    private const double TrayRefreshIntervalSeconds = 2;
    private const double MaxOfflineSeconds = 8 * 3600;
    private const double OfflineBannerMinSeconds = 120;
    private const float OfflineBannerSeconds = 8;
    private const float TakeOverBannerSeconds = 3;
    private const int QuitFlushTimeoutMilliseconds = 3000;
    private const int HttpTimeoutSeconds = 10;
    private const int TrayMenuToggleId = 0;
    private const int TrayMenuQuitId = 1;
    private const int TrayMenuSignOutId = 2;
    private const string SavePath = "user://save.json";
    private const string SessionPath = "user://session.dat";
    private const string InstancePath = "user://instance.pid";

    private readonly List<UpgradeSlot> _upgradeSlots = [];
    private readonly GameState _game = new();
    private SingleInstance _instance = null!;
    private SaveStore _saveStore = null!;
    private AppBar? _appBar;
    private HttpClient? _http;
    private CloudSync? _cloud;
    private SyncSlot? _syncSlot;
    private LoginWindow _loginWindow = null!;
    private Control _expandedView = null!;
    private Control _collapsedView = null!;
    private Label _goldLabel = null!;
    private Label _rateLabel = null!;
    private Label _compactLabel = null!;
    private MineLane _lane = null!;
    private StatusIndicator _tray = null!;
    private PopupMenu _trayMenu = null!;
    private long? _savedCloudRevision;
    private double _sinceAutosave;
    private double _sinceTrayRefresh;
    private bool _collapsed;
    private bool _quitting;

    public override void _Ready()
    {
        _instance = SingleInstance.ReplaceRunningInstance(ProjectSettings.GlobalizePath(InstancePath));
        GetTree().AutoAcceptQuit = false;
        Theme = BarTheme.Create();
        _saveStore = new SaveStore(ProjectSettings.GlobalizePath(SavePath));

        double offlineGain = LoadGame();
        _cloud = CreateCloudSync();
        BuildInterface();
        BuildTray();
        BuildLoginWindow();
        DockToScreen();
        ApplyCollapsed(_collapsed);
        RefreshInterface();
        _cloud?.Start();

        if (offlineGain >= 1)
        {
            ShowOfflineGain(offlineGain);
        }
    }

    public override void _Process(double delta)
    {
        _appBar?.Update();
        _game.Advance(delta);
        _cloud?.Tick(delta);
        RefreshInterface();

        _sinceAutosave += delta;
        if (_sinceAutosave >= AutosaveIntervalSeconds)
        {
            _sinceAutosave = 0;
            SaveGame();
        }

        _sinceTrayRefresh += delta;
        if (_sinceTrayRefresh >= TrayRefreshIntervalSeconds)
        {
            _sinceTrayRefresh = 0;
            RefreshTray();
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
        _appBar?.Dispose();
        _http?.Dispose();
        _instance.Dispose();
    }

    private double LoadGame()
    {
        SaveData? save = _saveStore.Load();
        if (save is null)
        {
            return 0;
        }

        _game.Restore(save.ToProgress());
        _collapsed = save.Collapsed;
        _savedCloudRevision = save.CloudRevision;
        double elapsed = Math.Clamp(Time.GetUnixTimeFromSystem() - save.SavedAtUnixSeconds, 0, MaxOfflineSeconds);
        return _game.Advance(elapsed);
    }

    private void SaveGame()
    {
        long? cloudRevision = _cloud?.KnownRevision ?? _savedCloudRevision;
        _saveStore.Save(SaveData.From(_game.Capture(), Time.GetUnixTimeFromSystem(), _collapsed, cloudRevision));
    }

    private CloudSync? CreateCloudSync()
    {
        SupabaseSettings? settings = SupabaseSettings.FromProjectSettings();
        if (settings is null || !OperatingSystem.IsWindows())
        {
            return null;
        }

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
        CloudSync cloud = new(
            _game,
            new SupabaseAuth(_http, settings),
            new SupabaseSaves(_http, settings),
            new SessionStore(ProjectSettings.GlobalizePath(SessionPath)),
            System.Environment.MachineName,
            _savedCloudRevision,
            MaxOfflineSeconds);
        cloud.ProgressAdopted += OnProgressAdopted;
        return cloud;
    }

    private async void Quit()
    {
        if (_quitting)
        {
            return;
        }

        _quitting = true;
        SaveGame();
        if (_cloud is not null)
        {
            await Task.WhenAny(_cloud.FlushAsync(), Task.Delay(QuitFlushTimeoutMilliseconds));
            SaveGame();
        }

        _appBar?.Dispose();
        _tray.Visible = false;
        GetTree().Quit();
    }

    private void DockToScreen()
    {
        if (!OperatingSystem.IsWindows())
        {
            GD.PushWarning("La barre réservée n'est disponible que sous Windows.");
            return;
        }

        IntPtr window = new(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle));
        _appBar = new AppBar(window);
        _appBar.Docked += OnDocked;
        _appBar.FullscreenAppChanged += OnFullscreenAppChanged;
    }

    private void OnDocked()
    {
        GetWindow().ContentScaleFactor = _appBar!.Scale;
    }

    private void OnFullscreenAppChanged(bool fullscreen)
    {
        UpdateFrameRate();
    }

    private void ToggleCollapsed()
    {
        ApplyCollapsed(!_collapsed);
        SaveGame();
    }

    private void ApplyCollapsed(bool collapsed)
    {
        _collapsed = collapsed;
        _expandedView.Visible = !collapsed;
        _collapsedView.Visible = collapsed;
        _appBar?.Dock(collapsed ? CollapsedHeight : ExpandedHeight);
        _trayMenu.SetItemText(_trayMenu.GetItemIndex(TrayMenuToggleId), collapsed ? "Déplier" : "Replier");
        UpdateFrameRate();
    }

    private void UpdateFrameRate()
    {
        bool inBackground = _collapsed || _appBar?.FullscreenAppActive == true;
        Engine.MaxFps = inBackground ? BackgroundFramesPerSecond : ActiveFramesPerSecond;
    }

    private bool ClaimInteraction()
    {
        if (_cloud is not { Status: CloudStatus.Passive })
        {
            return true;
        }

        _cloud.RequestTakeOver();
        _lane.ShowBanner("Reprise de la partie sur ce PC…", TakeOverBannerSeconds);
        return false;
    }

    private void OnMined(Vector2 position)
    {
        if (!ClaimInteraction())
        {
            return;
        }

        double amount = _game.Mine();
        _lane.ShowGain(position, $"+{NumberFormat.Rate(amount)}");
    }

    private void OnUpgradePressed(UpgradeDefinition upgrade)
    {
        if (ClaimInteraction())
        {
            _game.TryBuy(upgrade);
        }
    }

    private void OnSyncSlotPressed()
    {
        switch (_cloud?.Status)
        {
            case CloudStatus.SignedOut:
                _loginWindow.Open(_appBar?.Scale ?? 1f, _cloud.Email);
                break;
            case CloudStatus.Passive:
            case CloudStatus.Offline:
                _cloud.RequestTakeOver();
                break;
        }
    }

    private async void OnLoginSubmitted(string email, string password)
    {
        if (_cloud is null)
        {
            return;
        }

        _loginWindow.SetBusy(true);
        try
        {
            await _cloud.SignInAsync(email, password);
            _loginWindow.Hide();
        }
        catch (CloudAuthException exception)
        {
            _loginWindow.ShowError(exception.Message);
        }
        catch (CloudRequestException exception)
        {
            _loginWindow.ShowError(exception.Message);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _loginWindow.ShowError("Supabase est injoignable. Vérifie ta connexion internet.");
        }
        finally
        {
            _loginWindow.SetBusy(false);
        }
    }

    private void OnProgressAdopted(double elapsedSeconds, double gain)
    {
        if (elapsedSeconds >= OfflineBannerMinSeconds && gain >= 1)
        {
            ShowOfflineGain(gain);
        }
    }

    private void ShowOfflineGain(double gain)
    {
        _lane.ShowBanner($"Pendant ton absence : +{NumberFormat.Amount(gain)} or", OfflineBannerSeconds);
    }

    private void RefreshInterface()
    {
        string gold = NumberFormat.Amount(_game.Gold);
        string rate = NumberFormat.Rate(_game.IncomePerSecond);
        _goldLabel.Text = $"{gold} or";
        _rateLabel.Text = $"+{rate}/s · clic +{NumberFormat.Rate(_game.ClickValue)}";
        _compactLabel.Text = $"{gold} or  ·  +{rate}/s{DescribeSyncForCompactView()}";
        _lane.SetCrew(_game.OwnedCount(UpgradeCatalog.MinerId), _game.OwnedCount(UpgradeCatalog.DrillId));
        _lane.SetNotice(_cloud is { Status: CloudStatus.Passive }
            ? $"Partie active sur {_cloud.ActiveDevice} · clique pour la reprendre ici"
            : string.Empty);
        foreach (UpgradeSlot slot in _upgradeSlots)
        {
            slot.Refresh(_game);
        }

        if (_cloud is not null)
        {
            _syncSlot?.Refresh(_cloud.Status, _cloud.ActiveDevice);
        }
    }

    private string DescribeSyncForCompactView() => _cloud?.Status switch
    {
        CloudStatus.Passive => $"  ·  active sur {_cloud.ActiveDevice}",
        CloudStatus.Offline => "  ·  hors ligne",
        _ => string.Empty,
    };

    private void RefreshTray()
    {
        string sync = _cloud?.Status switch
        {
            CloudStatus.Active => " · synchro active",
            CloudStatus.Passive => $" · active sur {_cloud.ActiveDevice}",
            CloudStatus.Offline => " · hors ligne",
            _ => string.Empty,
        };
        _tray.Tooltip = $"IdleBar : {NumberFormat.Amount(_game.Gold)} or (+{NumberFormat.Rate(_game.IncomePerSecond)}/s){sync}";

        int signOutIndex = _trayMenu.GetItemIndex(TrayMenuSignOutId);
        if (signOutIndex >= 0)
        {
            _trayMenu.SetItemDisabled(signOutIndex, _cloud?.Status is null or CloudStatus.SignedOut);
        }
    }

    private void BuildInterface()
    {
        Panel background = new() { MouseFilter = MouseFilterEnum.Ignore };
        background.AddThemeStyleboxOverride("panel", BarTheme.CreateBackground());
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _expandedView = BuildExpandedView();
        _collapsedView = BuildCollapsedView();
    }

    private MarginContainer BuildExpandedView()
    {
        MarginContainer view = AddFullRectMargin(10, 5);
        HBoxContainer row = CreateRow(10);
        view.AddChild(row);

        VBoxContainer stats = new() { CustomMinimumSize = new Vector2(150, 0), Alignment = BoxContainer.AlignmentMode.Center };
        stats.AddThemeConstantOverride("separation", 0);
        _goldLabel = CreateLabel(19, BarPalette.Gold);
        _rateLabel = CreateLabel(11, BarPalette.Muted);
        stats.AddChild(_goldLabel);
        stats.AddChild(_rateLabel);
        row.AddChild(stats);

        _lane = new MineLane { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _lane.Mined += OnMined;
        row.AddChild(_lane);

        foreach (UpgradeDefinition upgrade in _game.Upgrades)
        {
            UpgradeSlot slot = new(upgrade);
            ClickBinding.OnLeftPress(slot.Button, () => OnUpgradePressed(slot.Upgrade));
            _upgradeSlots.Add(slot);
            row.AddChild(slot.Button);
        }

        if (_cloud is not null)
        {
            _syncSlot = new SyncSlot();
            ClickBinding.OnLeftPress(_syncSlot.Button, OnSyncSlotPressed);
            row.AddChild(_syncSlot.Button);
        }

        VBoxContainer windowButtons = new() { Alignment = BoxContainer.AlignmentMode.Center };
        AddWindowButtons(windowButtons, "–");
        row.AddChild(windowButtons);
        return view;
    }

    private MarginContainer BuildCollapsedView()
    {
        MarginContainer view = AddFullRectMargin(10, 1);
        HBoxContainer row = CreateRow(6);
        view.AddChild(row);

        _compactLabel = CreateLabel(12, BarPalette.Gold);
        _compactLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(_compactLabel);
        AddWindowButtons(row, "+");
        return view;
    }

    private void AddWindowButtons(BoxContainer container, string toggleText)
    {
        container.AddThemeConstantOverride("separation", 2);
        container.AddChild(CreateWindowButton(toggleText, "Replier / déplier", ToggleCollapsed));
        container.AddChild(CreateWindowButton("×", "Quitter", Quit));
    }

    private void BuildTray()
    {
        _trayMenu = new PopupMenu { PreferNativeMenu = true };
        _trayMenu.AddItem("Replier", TrayMenuToggleId);
        if (_cloud is not null)
        {
            _trayMenu.AddItem("Se déconnecter de la synchro", TrayMenuSignOutId);
        }

        _trayMenu.AddSeparator();
        _trayMenu.AddItem("Quitter", TrayMenuQuitId);
        _trayMenu.IdPressed += OnTrayMenuItemPressed;
        AddChild(_trayMenu);

        _tray = new StatusIndicator { Icon = TrayIcon.Create(), Tooltip = "IdleBar" };
        _tray.Pressed += (mouseButton, position) => OnTrayPressed((MouseButton)mouseButton, position);
        AddChild(_tray);
    }

    private void BuildLoginWindow()
    {
        _loginWindow = new LoginWindow();
        _loginWindow.Submitted += OnLoginSubmitted;
        AddChild(_loginWindow);
    }

    private void OnTrayPressed(MouseButton button, Vector2I position)
    {
        if (button == MouseButton.Left)
        {
            ToggleCollapsed();
            return;
        }

        RefreshTray();
        _trayMenu.Popup(new Rect2I(position, Vector2I.Zero));
    }

    private void OnTrayMenuItemPressed(long id)
    {
        switch (id)
        {
            case TrayMenuToggleId:
                ToggleCollapsed();
                break;
            case TrayMenuSignOutId:
                _cloud?.SignOut();
                break;
            case TrayMenuQuitId:
                Quit();
                break;
        }
    }

    private MarginContainer AddFullRectMargin(int horizontal, int vertical)
    {
        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", horizontal);
        margin.AddThemeConstantOverride("margin_right", horizontal);
        margin.AddThemeConstantOverride("margin_top", vertical);
        margin.AddThemeConstantOverride("margin_bottom", vertical);
        AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return margin;
    }

    private static HBoxContainer CreateRow(int separation)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", separation);
        return row;
    }

    private static Label CreateLabel(int fontSize, Color color)
    {
        Label label = new() { VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Button CreateWindowButton(string text, string tooltip, Action onPressed)
    {
        Button button = new()
        {
            Text = text,
            TooltipText = tooltip,
            Flat = true,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(22, 18),
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        ClickBinding.OnLeftPress(button, onPressed);
        return button;
    }
}
