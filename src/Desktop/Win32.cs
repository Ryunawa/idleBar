using System;
using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

internal static class Win32
{
    public const int GwlExStyle = -20;
    public const int GwlpWndProc = -4;
    public const long WsExToolWindow = 0x00000080;
    public const long WsExAppWindow = 0x00040000;

    public const uint WmClose = 0x0010;
    public const uint WmActivate = 0x0006;
    public const uint WmWindowPosChanged = 0x0047;
    public const uint WmDisplayChange = 0x007E;
    public const uint WmDpiChanged = 0x02E0;

    public const int SwHide = 0;
    public const int SwShowNoActivate = 4;

    public const uint SwpNoActivate = 0x0010;
    public const uint SwpNoMoveNoSizeNoActivate = 0x0013;

    public const uint MonitorDefaultToPrimary = 0x00000001;
    public const int MdtEffectiveDpi = 0;
    public const float DefaultDpi = 96f;

    public const uint AbmNew = 0x00;
    public const uint AbmRemove = 0x01;
    public const uint AbmQueryPos = 0x02;
    public const uint AbmSetPos = 0x03;
    public const uint AbmActivate = 0x06;
    public const uint AbmWindowPosChanged = 0x09;
    public const uint AbeBottom = 3;
    public const int AbnPosChanged = 0x01;
    public const int AbnFullscreenApp = 0x02;

    public static readonly IntPtr HwndTopmost = new(-1);
    public static readonly IntPtr HwndBottom = new(1);

    [DllImport("shell32.dll")]
    public static extern UIntPtr SHAppBarMessage(uint message, ref AppBarData data);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    public static extern IntPtr CallWindowProc(IntPtr previousProcedure, IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)]
    public static extern uint RegisterWindowMessage(string name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(Win32Point point, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
}
