using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace LincoFarmTool.Services;

/// <summary>一条可用更新的信息。</summary>
public record UpdateInfo(Version Version, string DownloadUrl, string PageUrl);

/// <summary>
/// 应用内更新：查 GitHub Releases 最新版，比对版本，
/// 下载新 exe 并通过辅助脚本替换当前运行的 exe 后重启。
/// </summary>
public static class UpdateService
{
    // 发布仓库（需为 public，或 Release 资源可匿名下载）
    private const string Owner = "KarthusChen";
    private const string Repo = "Linco-FarmTool";
    private const string AssetName = "LincoFarmTool.exe";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("LincoFarmTool-Updater");
        return c;
    }

    public static Version CurrentVersion
    {
        get
        {
            var v = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
            return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
        }
    }

    /// <summary>查询是否有新版本。无更新 / 网络失败时返回 null。</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            string url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
            string json = await Http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var latest = ParseVersion(root.GetProperty("tag_name").GetString());
            if (latest == null || latest <= CurrentVersion) return null;

            string page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";

            // 找到名为 LincoFarmTool.exe 的发布资源
            string? asset = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var a in assets.EnumerateArray())
                {
                    if (string.Equals(a.GetProperty("name").GetString(), AssetName, StringComparison.OrdinalIgnoreCase))
                    {
                        asset = a.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }
            if (asset == null) return null;

            return new UpdateInfo(latest, asset, page);
        }
        catch
        {
            return null;
        }
    }

    private static Version? ParseVersion(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        tag = tag.TrimStart('v', 'V').Trim();
        return Version.TryParse(tag, out var v)
            ? new Version(v.Major, v.Minor, Math.Max(0, v.Build))
            : null;
    }

    /// <summary>下载新版并替换当前 exe，然后退出重启。运行于已发布的单文件 exe 时才有意义。</summary>
    public static async Task DownloadAndApplyAsync(UpdateInfo info)
    {
        string currentExe = Environment.ProcessPath
            ?? throw new InvalidOperationException("拿不到当前 exe 路径");
        string dir = Path.GetDirectoryName(currentExe)!;
        string newExe = Path.Combine(dir, "LincoFarmTool.new.exe");

        var bytes = await Http.GetByteArrayAsync(info.DownloadUrl);
        await File.WriteAllBytesAsync(newExe, bytes);

        // 生成替换脚本：等本进程退出 → 覆盖 → 重启
        int pid = Environment.ProcessId;
        string bat = Path.Combine(Path.GetTempPath(), "linco_update.bat");
        string script =
            "@echo off\r\n" +
            "chcp 65001 >nul\r\n" +
            ":waitloop\r\n" +
            $"tasklist /fi \"PID eq {pid}\" | find \"{pid}\" >nul\r\n" +
            "if not errorlevel 1 (\r\n" +
            "  ping -n 2 127.0.0.1 >nul\r\n" +
            "  goto waitloop\r\n" +
            ")\r\n" +
            $"move /y \"{newExe}\" \"{currentExe}\" >nul\r\n" +
            $"start \"\" \"{currentExe}\"\r\n" +
            "del \"%~f0\"\r\n";
        await File.WriteAllTextAsync(bat, script, new UTF8Encoding(false));

        Process.Start(new ProcessStartInfo
        {
            FileName = bat,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        System.Windows.Application.Current.Shutdown();
    }
}
