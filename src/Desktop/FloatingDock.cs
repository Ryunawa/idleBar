using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

namespace IdleBar.Desktop;

public sealed class FloatingDock : IBarDock
{
    private const ulong RecheckMilliseconds = 1000;
    private const string DevicePrefix = "screen-";

    private readonly Window _window;
    private int _logicalHeight;
    private string? _screenDevice;
    private float _sizeFactor = 1f;
    private Rect2I _bounds;
    private ulong _nextCheck;

    public FloatingDock(Window window)
    {
        _window = window;
    }

    public event Action? Docked;

    public event Action<bool>? FullscreenAppChanged
    {
        add { }
        remove { }
    }

    public float DpiScale { get; private set; } = 1f;

    public float Scale => DpiScale * _sizeFactor;

    public bool FullscreenAppActive => false;

    public IReadOnlyList<DisplayScreen> DetectScreens() =>
        Enumerable.Range(0, DisplayServer.GetScreenCount())
            .Select(index => Describe(index))
            .OrderBy(screen => screen.Left)
            .ThenBy(screen => screen.Top)
            .ToList();

    public void Dock(int logicalHeight)
    {
        _logicalHeight = logicalHeight;
        Reposition();
    }

    public void Place(string? screenDevice, float sizeFactor)
    {
        _screenDevice = screenDevice;
        _sizeFactor = sizeFactor;
        Reposition();
    }

    public void Update()
    {
        if (Time.GetTicksMsec() >= _nextCheck)
        {
            Reposition();
        }
    }

    public void Dispose()
    {
    }

    private static DisplayScreen Describe(int screen)
    {
        Vector2I position = DisplayServer.ScreenGetPosition(screen);
        Vector2I size = DisplayServer.ScreenGetSize(screen);
        return new DisplayScreen(DeviceOf(screen), position.X, position.Y, size.X, size.Y, screen == DisplayServer.GetPrimaryScreen());
    }

    private static string DeviceOf(int screen) => DevicePrefix + screen.ToString(CultureInfo.InvariantCulture);

    private static int ScreenOf(string? device) =>
        device is not null
        && device.StartsWith(DevicePrefix, StringComparison.Ordinal)
        && int.TryParse(device[DevicePrefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int screen)
        && screen < DisplayServer.GetScreenCount()
            ? screen
            : DisplayServer.GetPrimaryScreen();

    private void Reposition()
    {
        _nextCheck = Time.GetTicksMsec() + RecheckMilliseconds;
        if (_logicalHeight <= 0)
        {
            return;
        }

        int screen = ScreenOf(_screenDevice);
        DpiScale = DisplayServer.ScreenGetScale(screen);
        Rect2I usable = DisplayServer.ScreenGetUsableRect(screen);
        int height = (int)MathF.Round(_logicalHeight * Scale);
        Rect2I bounds = new(usable.Position.X, usable.End.Y - height, usable.Size.X, height);
        if (bounds == _bounds)
        {
            return;
        }

        _bounds = bounds;
        _window.CurrentScreen = screen;
        _window.Size = bounds.Size;
        _window.Position = bounds.Position;
        Docked?.Invoke();
    }
}