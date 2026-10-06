using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

[StructLayout(LayoutKind.Sequential)]
internal struct Win32Point
{
    public int X;
    public int Y;
}
