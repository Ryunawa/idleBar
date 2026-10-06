using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

[StructLayout(LayoutKind.Sequential)]
internal struct MonitorInfo
{
    public uint Size;
    public Win32Rect Monitor;
    public Win32Rect WorkArea;
    public uint Flags;
}
