using System;
using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
internal delegate IntPtr WindowProcedure(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
