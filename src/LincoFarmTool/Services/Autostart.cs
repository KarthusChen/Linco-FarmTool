using System.IO;
using Microsoft.Win32;

namespace LincoFarmTool.Services;

/// <summary>
/// 开机自启（写用户级注册表 Run 项）和「彻底清理」。
/// 绿色 exe 没有安装/卸载，用这两个操作模拟。
/// </summary>
public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LincoFarmTool";

    public static bool IsEnabled()
    {
        using var k = Registry.CurrentUser.OpenSubKey(RunKey);
        return k?.GetValue(ValueName) != null;
    }

    public static void Set(bool on)
    {
        using var k = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                      ?? Registry.CurrentUser.CreateSubKey(RunKey);
        if (k == null) return;

        if (on)
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe)) k.SetValue(ValueName, $"\"{exe}\"");
        }
        else
        {
            k.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>清掉开机自启项 + 删除本地数据目录（%AppData%\LincoFarmTool）。</summary>
    public static void CleanUp()
    {
        Set(false);
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LincoFarmTool");
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // 删不掉就算了，不影响退出
        }
    }
}
