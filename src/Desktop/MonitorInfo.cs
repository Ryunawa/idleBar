using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MonitorInfo
{
    public uint Size;
    public Win32Rect Monitor;
    public Win32Rect WorkArea;
    public uint Flags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string Device;
}
