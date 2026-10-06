using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

public static class DisplayScreens
{
    public static IReadOnlyList<DisplayScreen> Detect() =>
        EnumerateMonitors()
            .Select(Describe)
            .Select(info => new DisplayScreen(
                info.Device,
                info.Monitor.Left,
                info.Monitor.Top,
                info.Monitor.Width,
                info.Monitor.Height,
                info.Flags == Win32.MonitorInfoPrimary))
            .OrderBy(screen => screen.Left)
            .ThenBy(screen => screen.Top)
            .ToList();

    internal static IntPtr Resolve(string? device)
    {
        if (device is not null)
        {
            foreach (IntPtr monitor in EnumerateMonitors())
            {
                if (Describe(monitor).Device == device)
                {
                    return monitor;
                }
            }
        }

        return Win32.MonitorFromPoint(new Win32Point(), Win32.MonitorDefaultToPrimary);
    }

    internal static MonitorInfo Describe(IntPtr monitor)
    {
        MonitorInfo info = new() { Size = (uint)Marshal.SizeOf<MonitorInfo>(), Device = string.Empty };
        Win32.GetMonitorInfo(monitor, ref info);
        return info;
    }

    internal static float ReadDpiScale(IntPtr monitor)
    {
        int result = Win32.GetDpiForMonitor(monitor, Win32.MdtEffectiveDpi, out uint dpi, out uint _);
        return result == 0 && dpi > 0 ? dpi / Win32.DefaultDpi : 1f;
    }

    private static List<IntPtr> EnumerateMonitors()
    {
        List<IntPtr> monitors = [];
        MonitorEnumProcedure collect = (IntPtr monitor, IntPtr deviceContext, ref Win32Rect bounds, IntPtr data) =>
        {
            monitors.Add(monitor);
            return true;
        };
        Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, collect, IntPtr.Zero);
        GC.KeepAlive(collect);
        return monitors;
    }
}
