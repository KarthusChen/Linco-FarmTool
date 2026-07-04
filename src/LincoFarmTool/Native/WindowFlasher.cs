using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LincoFarmTool.Native;

/// <summary>
/// 让任务栏/窗口标题闪烁提醒（FlashWindowEx）。
/// 桌宠没有任务栏按钮，但仍会触发系统的“需要你注意”提示。
/// </summary>
public static class WindowFlasher
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    private const uint FLASHW_ALL = 3;      // 标题栏 + 任务栏
    private const uint FLASHW_TIMERNOFG = 12; // 持续闪到窗口被激活

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    public static void Flash(Window window, uint count = 5)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var info = new FLASHWINFO
        {
            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
            hwnd = hwnd,
            dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
            uCount = count,
            dwTimeout = 0
        };
        FlashWindowEx(ref info);
    }
}
