using System.Runtime.InteropServices;

namespace LincoFarmTool.Native;

/// <summary>
/// 全局低级键盘钩子（WH_KEYBOARD_LL）。
/// 即使本窗口没有焦点，也能感知到任意按键 —— 这是 BongoCat 类桌宠的核心。
/// 用完务必 Dispose，否则钩子会一直挂在系统上。
/// </summary>
public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;

    // 按下 / 抬起事件，参数为虚拟键码
    public event Action<int>? KeyDown;
    public event Action<int>? KeyUp;

    // 委托必须用字段持有，否则会被 GC 回收导致钩子失效崩溃
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        // 对于 WH_KEYBOARD_LL，hMod 可以传 0、threadId 传 0 即为全局钩子
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            int vkCode = Marshal.ReadInt32(lParam);

            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                // 回到 UI 线程再抛事件，避免在钩子线程里碰 UI
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => KeyDown?.Invoke(vkCode));
            }
            else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => KeyUp?.Invoke(vkCode));
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
}
