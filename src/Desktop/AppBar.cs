using System;
using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

public sealed class AppBar : IDisposable
{
    private const string CallbackMessageName = "IdleBar.AppBar.Callback";

    private readonly IntPtr _window;
    private readonly uint _callbackMessage;
    private readonly WindowProcedure _windowProcedure;
    private readonly IntPtr _originalWindowProcedure;
    private Win32Rect _bounds;
    private int _logicalHeight;
    private bool _registered;
    private bool _positionInvalidated;
    private bool? _pendingFullscreenState;

    public AppBar(IntPtr window)
    {
        _window = window;
        _callbackMessage = Win32.RegisterWindowMessage(CallbackMessageName);
        _windowProcedure = HandleWindowMessage;
        _originalWindowProcedure = Win32.SetWindowLongPtr(
            window,
            Win32.GwlpWndProc,
            Marshal.GetFunctionPointerForDelegate(_windowProcedure));
        HideFromTaskbar();

        AppBarData data = CreateData();
        Win32.SHAppBarMessage(Win32.AbmNew, ref data);
        _registered = true;
    }

    public event Action? Docked;

    public event Action<bool>? FullscreenAppChanged;

    public float Scale { get; private set; } = 1f;

    public bool FullscreenAppActive { get; private set; }

    public void Dock(int logicalHeight)
    {
        _logicalHeight = logicalHeight;
        _positionInvalidated = true;
        Update();
    }

    public void Update()
    {
        if (!_registered)
        {
            return;
        }

        if (_pendingFullscreenState is bool fullscreen)
        {
            _pendingFullscreenState = null;
            ApplyFullscreenState(fullscreen);
        }

        if (_positionInvalidated)
        {
            _positionInvalidated = false;
            Reposition();
        }
    }

    public void Dispose()
    {
        if (!_registered)
        {
            return;
        }

        _registered = false;
        AppBarData data = CreateData();
        Win32.SHAppBarMessage(Win32.AbmRemove, ref data);
        Win32.SetWindowLongPtr(_window, Win32.GwlpWndProc, _originalWindowProcedure);
    }

    private static float ReadScale(IntPtr monitor)
    {
        int result = Win32.GetDpiForMonitor(monitor, Win32.MdtEffectiveDpi, out uint dpi, out uint _);
        return result == 0 && dpi > 0 ? dpi / Win32.DefaultDpi : 1f;
    }

    private void Reposition()
    {
        IntPtr monitor = Win32.MonitorFromPoint(new Win32Point(), Win32.MonitorDefaultToPrimary);
        Scale = ReadScale(monitor);
        int height = (int)MathF.Round(_logicalHeight * Scale);

        MonitorInfo monitorInfo = new() { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        Win32.GetMonitorInfo(monitor, ref monitorInfo);

        AppBarData data = CreateData();
        data.Bounds = monitorInfo.Monitor;
        data.Bounds.Top = data.Bounds.Bottom - height;
        Win32.SHAppBarMessage(Win32.AbmQueryPos, ref data);
        data.Bounds.Top = data.Bounds.Bottom - height;

        if (!data.Bounds.SameAs(_bounds))
        {
            Win32.SHAppBarMessage(Win32.AbmSetPos, ref data);
            _bounds = data.Bounds;
        }

        IntPtr zOrder = FullscreenAppActive ? Win32.HwndBottom : Win32.HwndTopmost;
        Win32.SetWindowPos(_window, zOrder, _bounds.Left, _bounds.Top, _bounds.Width, _bounds.Height, Win32.SwpNoActivate);
        Docked?.Invoke();
    }

    private void ApplyFullscreenState(bool fullscreen)
    {
        if (fullscreen == FullscreenAppActive)
        {
            return;
        }

        FullscreenAppActive = fullscreen;
        IntPtr zOrder = fullscreen ? Win32.HwndBottom : Win32.HwndTopmost;
        Win32.SetWindowPos(_window, zOrder, 0, 0, 0, 0, Win32.SwpNoMoveNoSizeNoActivate);
        FullscreenAppChanged?.Invoke(fullscreen);
    }

    private void HideFromTaskbar()
    {
        long style = Win32.GetWindowLongPtr(_window, Win32.GwlExStyle).ToInt64();
        long toolWindowStyle = (style & ~Win32.WsExAppWindow) | Win32.WsExToolWindow;
        Win32.ShowWindow(_window, Win32.SwHide);
        Win32.SetWindowLongPtr(_window, Win32.GwlExStyle, new IntPtr(toolWindowStyle));
        Win32.ShowWindow(_window, Win32.SwShowNoActivate);
    }

    private IntPtr HandleWindowMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == _callbackMessage)
        {
            HandleShellNotification(wParam.ToInt32(), lParam);
            return IntPtr.Zero;
        }

        switch (message)
        {
            case Win32.WmActivate:
                NotifyShell(Win32.AbmActivate);
                break;
            case Win32.WmWindowPosChanged:
                NotifyShell(Win32.AbmWindowPosChanged);
                break;
            case Win32.WmDisplayChange:
            case Win32.WmDpiChanged:
                _positionInvalidated = true;
                break;
        }

        return Win32.CallWindowProc(_originalWindowProcedure, window, message, wParam, lParam);
    }

    private void HandleShellNotification(int notification, IntPtr lParam)
    {
        switch (notification)
        {
            case Win32.AbnPosChanged:
                _positionInvalidated = true;
                break;
            case Win32.AbnFullscreenApp:
                _pendingFullscreenState = lParam != IntPtr.Zero;
                break;
        }
    }

    private void NotifyShell(uint message)
    {
        if (!_registered)
        {
            return;
        }

        AppBarData data = CreateData();
        Win32.SHAppBarMessage(message, ref data);
    }

    private AppBarData CreateData() => new()
    {
        Size = (uint)Marshal.SizeOf<AppBarData>(),
        Window = _window,
        CallbackMessage = _callbackMessage,
        Edge = Win32.AbeBottom,
    };
}
